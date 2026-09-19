using Minio;
using Minio.DataModel.Args;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;

namespace ParkingApp.Infrastructure.Files;

/// <summary>
/// MinIO (S3-compatible) storage for facility images. Only the object key/URL
/// is persisted in the database; bytes live in the bucket.
/// </summary>
public class MinioFileStorage : IFileStorage
{
    private readonly MinioSettings _settings;
    private readonly IMinioClient _client;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public MinioFileStorage(MinioSettings settings)
    {
        _settings = settings;
        _client = new MinioClient()
            .WithEndpoint(settings.Endpoint)
            .WithCredentials(settings.AccessKey, settings.SecretKey)
            .WithSSL(settings.UseSsl)
            .Build();
    }

    public async Task<StoredFile> SaveAsync(Stream content, string fileName, string contentType, string folder, CancellationToken cancellationToken = default)
    {
        await EnsureBucketAsync(cancellationToken);

        var extension = Path.GetExtension(fileName);
        var prefix = folder.Trim().Trim('/');
        var key = $"{prefix}/{Guid.NewGuid():N}{extension}";

        long size;
        MemoryStream? buffer = null;

        try
        {
            Stream uploadStream;
            if (content.CanSeek)
            {
                if (content.Position != 0)
                    content.Position = 0;

                size = content.Length;
                uploadStream = content;
            }
            else
            {
                buffer = new MemoryStream();
                await content.CopyToAsync(buffer, cancellationToken);
                buffer.Position = 0;
                size = buffer.Length;
                uploadStream = buffer;
            }

            await _client.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(_settings.Bucket)
                    .WithObject(key)
                    .WithStreamData(uploadStream)
                    .WithObjectSize(size)
                    .WithContentType(contentType),
                cancellationToken);
        }
        finally
        {
            buffer?.Dispose();
        }

        var url = $"{_settings.PublicBaseUrl.TrimEnd('/')}/{_settings.Bucket}/{key}";

        return new StoredFile(url, Path.GetFileName(fileName), contentType, size);
    }

    public async Task DeleteAsync(string url, CancellationToken cancellationToken = default)
    {
        var marker = $"/{_settings.Bucket}/";
        var index = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return;

        var key = url[(index + marker.Length)..];
        if (string.IsNullOrWhiteSpace(key))
            return;

        await _client.RemoveObjectAsync(
            new RemoveObjectArgs()
                .WithBucket(_settings.Bucket)
                .WithObject(key),
            cancellationToken);
    }

    private async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
            return;

        await _initLock.WaitAsync(cancellationToken);

        try
        {
            if (_initialized)
                return;

            var exists = await _client.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_settings.Bucket),
                cancellationToken);

            if (!exists)
            {
                await _client.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(_settings.Bucket),
                    cancellationToken);

                var policy = $$"""
                    {
                      "Version": "2012-10-17",
                      "Statement": [
                        {
                          "Effect": "Allow",
                          "Principal": { "AWS": ["*"] },
                          "Action": ["s3:GetObject"],
                          "Resource": ["arn:aws:s3:::{{_settings.Bucket}}/*"]
                        }
                      ]
                    }
                    """;

                await _client.SetPolicyAsync(
                    new SetPolicyArgs()
                        .WithBucket(_settings.Bucket)
                        .WithPolicy(policy),
                    cancellationToken);
            }

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }
}
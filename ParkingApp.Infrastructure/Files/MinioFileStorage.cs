using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
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
    private readonly ILogger<MinioFileStorage> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public MinioFileStorage(MinioSettings settings, ILogger<MinioFileStorage> logger)
    {
        _settings = settings;
        _logger = logger;

        var builder = new MinioClient()
            .WithEndpoint(settings.Endpoint)
            .WithCredentials(settings.AccessKey, settings.SecretKey)
            .WithSSL(settings.UseSsl);

        // Some S3-compatible providers (e.g. Cloudflare R2) require an explicit
        // SigV4 region. MinIO itself discovers it, so only set it when provided.
        if (!string.IsNullOrWhiteSpace(settings.Region))
            builder = builder.WithRegion(settings.Region);

        _client = builder.Build();
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

            // Verify the bytes actually landed. Older Minio SDKs silently
            // reported success against an unreachable server, leaving a DB row
            // pointing at a non-existent object that later read back as a
            // 0-byte "corrupted" image. Fail loudly instead.
            var stored = await _client.StatObjectAsync(
                new StatObjectArgs()
                    .WithBucket(_settings.Bucket)
                    .WithObject(key),
                cancellationToken);

            if (stored.Size != size)
                throw new InvalidOperationException(
                    $"MinIO upload verification failed for '{key}': expected {size} bytes, storage reports {stored.Size}.");
        }
        finally
        {
            buffer?.Dispose();
        }

        var url = $"{_settings.PublicBaseUrl.TrimEnd('/')}/{_settings.Bucket}/{key}";

        return new StoredFile(url, Path.GetFileName(fileName), contentType, size);
    }

    public async Task<StoredFileContent?> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        key = key.TrimStart('/');

        try
        {
            var stat = await _client.StatObjectAsync(
                new StatObjectArgs()
                    .WithBucket(_settings.Bucket)
                    .WithObject(key),
                cancellationToken);

            var buffer = new MemoryStream();
            await _client.GetObjectAsync(
                new GetObjectArgs()
                    .WithBucket(_settings.Bucket)
                    .WithObject(key)
                    .WithCallbackStream(stream => stream.CopyTo(buffer)),
                cancellationToken);

            // A stored image is never legitimately empty. Getting zero bytes
            // back means the object (or the whole MinIO server) is missing.
            // Treat it as "not found" so the API answers 404 instead of
            // handing the caller a 0-byte file that reads as corrupted.
            if (buffer.Length == 0)
            {
                buffer.Dispose();
                _logger.LogWarning(
                    "MinIO object '{Bucket}/{Key}' read back empty (reported size {Size}); returning not-found.",
                    _settings.Bucket, key, stat.Size);
                return null;
            }

            buffer.Position = 0;

            return new StoredFileContent(
                buffer,
                string.IsNullOrWhiteSpace(stat.ContentType) ? "application/octet-stream" : stat.ContentType,
                buffer.Length);
        }
        catch (MinioException)
        {
            return null;
        }
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

                // Public-read policy is a MinIO convenience only; managed
                // S3 providers such as Cloudflare R2 reject bucket policies.
                // Images are served through the API (FilesApi), so this is
                // best-effort and never blocks uploads.
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

                try
                {
                    await _client.SetPolicyAsync(
                        new SetPolicyArgs()
                            .WithBucket(_settings.Bucket)
                            .WithPolicy(policy),
                        cancellationToken);
                }
                catch (MinioException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Could not set public-read policy on bucket '{Bucket}'; objects will still be served through the API.",
                        _settings.Bucket);
                }
            }

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }
}
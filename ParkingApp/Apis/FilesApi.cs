using ParkingApp.Api.Infrastructure;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;

namespace ParkingApp.Api.Apis;

/// <summary>
/// Serves MinIO objects through the API host so image URLs resolve in a browser
/// even when MinIO itself is only reachable inside the cluster (localhost:9000).
/// Mirrors the stored path: /{bucket}/{key}, e.g. /parkingapp/license-images/x.png.
/// </summary>
public class FilesApi : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var minio = app.ServiceProvider.GetRequiredService<MinioSettings>();
        var bucket = minio.Bucket.Trim('/');

        app.MapGet($"/{bucket}/{{**key}}", OpenAsync);
    }

    private static async Task<IResult> OpenAsync(string key, IFileStorage storage, CancellationToken cancellationToken)
    {
        var file = await storage.OpenReadAsync(key, cancellationToken);
        if (file is null)
            return Results.NotFound();

        return Results.Stream(file.Content, file.ContentType);
    }
}

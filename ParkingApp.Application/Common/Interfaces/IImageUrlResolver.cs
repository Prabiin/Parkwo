namespace ParkingApp.Application.Common.Interfaces;

/// <summary>
/// Rewrites a stored object URL (as persisted by <see cref="IFileStorage"/>) into
/// one the current caller can actually fetch. Storage writes the configured
/// <c>Minio:PublicBaseUrl</c> into the row; when that host is only reachable
/// inside the cluster (localhost), this swaps it for the request's public origin
/// so browsers and phones get a working URL.
/// </summary>
public interface IImageUrlResolver
{
    /// <summary>
    /// Resolves a single stored URL. Null/empty input is returned unchanged;
    /// URLs that do not look like storage objects are returned as-is.
    /// </summary>
    string? Resolve(string? storedUrl);
}

namespace ParkingApp.Application.Common.Interfaces;

public record StoredFile(string Url, string FileName, string ContentType, long SizeInBytes);

public record StoredFileContent(Stream Content, string ContentType, long SizeInBytes);

public interface IFileStorage
{
    /// <summary>
    /// Persists the given stream under the bucket's <paramref name="folder"/>
    /// prefix (e.g. "facility-images", "profile-images") and returns its public URL.
    /// </summary>
    Task<StoredFile> SaveAsync(Stream content, string fileName, string contentType, string folder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a stored object for reading by its key (e.g. "license-images/x.png").
    /// Returns null when the object does not exist. The caller owns the stream.
    /// </summary>
    Task<StoredFileContent?> OpenReadAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Best-effort delete of a previously stored URL. No-op when the URL is unknown.
    /// </summary>
    Task DeleteAsync(string url, CancellationToken cancellationToken = default);
}
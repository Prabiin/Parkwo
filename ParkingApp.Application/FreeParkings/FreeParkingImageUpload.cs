namespace ParkingApp.Application.FreeParkings;

/// <summary>
/// A photo attached to a multipart free-parking form (create or update). The API
/// layer maps each IFormFile to this record; the handler validates, uploads to
/// MinIO and disposes it — same shape as <c>LicenseFileUpload</c>.
/// </summary>
public sealed record FreeParkingImageUpload(
    string FileName,
    string ContentType,
    long Length,
    Stream Content);

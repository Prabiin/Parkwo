using ParkingApp.Application.Configuration;

namespace ParkingApp.Application.Common.Helpers;

/// <summary>
/// Single place for upload validation (extension + size), backed by
/// <see cref="UploadSettings"/> so limits change via config/env, not code.
/// </summary>
public static class UploadValidation
{
    public static string? ValidateImage(
        string fileName,
        long length,
        UploadSettings settings)
    {
        var extension = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();

        if (string.IsNullOrEmpty(extension)
            || !settings.AllowedImageTypes.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return $"'{fileName}' is not a supported image type.";

        var maxBytes = (long)settings.MaxImageSizeInMB * 1024 * 1024;

        if (length > maxBytes || length == 0)
            return $"'{fileName}' must be between 1 byte and {settings.MaxImageSizeInMB}MB.";

        return null;
    }
}

using Microsoft.AspNetCore.Http;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;

namespace ParkingApp.Infrastructure.Files;

/// <summary>
/// Resolves stored MinIO object URLs against the caller's public origin.
/// Objects live at {PublicBaseUrl}/{Bucket}/{key}; the host written at upload
/// time is replaced only when the configured public base is a loopback address
/// (i.e. only reachable inside the cluster), in which case the incoming request's
/// scheme + host is used instead.
/// </summary>
public class MinioImageUrlResolver(MinioSettings settings, IHttpContextAccessor httpContextAccessor) : IImageUrlResolver
{
    public string? Resolve(string? storedUrl)
    {
        if (string.IsNullOrWhiteSpace(storedUrl))
            return storedUrl;

        var bucket = settings.Bucket.Trim('/');
        var marker = $"/{bucket}/";
        var markerIndex = storedUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        var key = markerIndex >= 0
            ? storedUrl[(markerIndex + marker.Length)..]
            : storedUrl.TrimStart('/');

        var baseUrl = settings.PublicBaseUrl?.TrimEnd('/') ?? string.Empty;
        var requestBaseUrl = GetRequestBaseUrl();

        if (!string.IsNullOrWhiteSpace(requestBaseUrl) && IsLoopback(baseUrl))
            baseUrl = requestBaseUrl.TrimEnd('/');

        return string.IsNullOrEmpty(baseUrl)
            ? storedUrl
            : $"{baseUrl}/{bucket}/{key}";
    }

    private string? GetRequestBaseUrl()
    {
        var request = httpContextAccessor.HttpContext?.Request;
        return request is null ? null : $"{request.Scheme}://{request.Host}";
    }

    private static bool IsLoopback(string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
            return true;

        var host = uri.Host;
        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
               || host.Equals("127.0.0.1", StringComparison.Ordinal)
               || host.Equals("::1", StringComparison.Ordinal)
               || host.Equals("0.0.0.0", StringComparison.Ordinal);
    }
}

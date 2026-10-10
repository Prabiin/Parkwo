namespace ParkingApp.Application.Configuration;

public class MinioSettings
{
    public const string SectionName = "Minio";

    public string Endpoint { get; set; } = default!;
    public string AccessKey { get; set; } = default!;
    public string SecretKey { get; set; } = default!;
    public string Bucket { get; set; } = "parkingapp";
    public bool UseSsl { get; set; }
    public string PublicBaseUrl { get; set; } = default!;

    /// <summary>
    /// SigV4 region for S3-compatible backends. Required by some providers
    /// (e.g. Cloudflare R2 uses "auto"); left empty for MinIO, which discovers
    /// the bucket region itself.
    /// </summary>
    public string? Region { get; set; }
}
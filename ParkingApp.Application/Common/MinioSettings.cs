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
}
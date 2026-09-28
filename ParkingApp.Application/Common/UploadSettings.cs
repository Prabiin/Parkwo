namespace ParkingApp.Application.Configuration;

public class UploadSettings
{
    public const string SectionName = "Upload";

    public int MaxImageSizeInMB { get; set; } = 5;
    public string[] AllowedImageTypes { get; set; } = ["jpg", "jpeg", "png"];
}

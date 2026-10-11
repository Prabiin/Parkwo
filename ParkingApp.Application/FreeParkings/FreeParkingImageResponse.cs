namespace ParkingApp.Application.FreeParkings;

public record FreeParkingImageResponse(
    Guid Id,
    string Url,
    string FileName,
    string ContentType,
    long SizeInBytes,
    int SortOrder);

namespace ParkingApp.Application.Facilities;

public record ParkingFacilityImageResponse(
    Guid Id,
    string Url,
    string FileName,
    string ContentType,
    long SizeInBytes,
    int SortOrder);
namespace ParkingApp.Application.FreeParkings.Queries.GetPublicFreeParking;

public record GetPublicFreeParkingResponse(
    Guid Id,
    string Name,
    string? Description,
    string Address,
    double Latitude,
    double Longitude,
    int? TwoWheelerOccupancy,
    int? FourWheelerOccupancy,
    IReadOnlyList<FreeParkingImageResponse> Images);

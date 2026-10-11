namespace ParkingApp.Application.FreeParkings.Queries.GetFreeParkingById;

public record GetFreeParkingByIdResponse(
    Guid Id,
    string Name,
    string? Description,
    string Address,
    double Latitude,
    double Longitude,
    int? TwoWheelerOccupancy,
    int? FourWheelerOccupancy,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    IReadOnlyList<FreeParkingImageResponse> Images);

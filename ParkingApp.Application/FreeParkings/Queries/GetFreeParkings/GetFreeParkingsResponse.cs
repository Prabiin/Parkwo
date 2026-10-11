namespace ParkingApp.Application.FreeParkings.Queries.GetFreeParkings;

public record FreeParkingItemResponse(
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
    int ImageCount,
    string? FirstImageUrl);

public record GetFreeParkingsResponse(
    IReadOnlyList<FreeParkingItemResponse> FreeParkings);

using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Facilities.Queries.GetParkingFacilities;

public record ParkingFacilityItemResponse(
    Guid Id,
    Guid ProviderId,
    string Name,
    string? Description,
    string Address,
    double? Latitude,
    double? Longitude,
    ApprovalStatusEnum ApprovalStatus,
    string ApprovalStatusDescription,
    DateTimeOffset CreatedAtUtc,
    int TwoWheelerCount,
    int FourWheelerCount,
    int ImageCount,
    double? AverageRating,
    int RatingCount);

public record GetParkingFacilitiesResponse(
    IReadOnlyList<ParkingFacilityItemResponse> Facilities);
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Facilities.Queries.GetParkingFacilityById;

public record GetParkingFacilityByIdResponse(
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
    int TwoWheelerOccupancy,
    int FourWheelerOccupancy,
    decimal? LandAreaSqM,
    int? PendingTwoWheelerOccupancy,
    int? PendingFourWheelerOccupancy,
    decimal? PendingLandAreaSqM,
    IReadOnlyList<ParkingFacilityImageResponse> Images,
    double? AverageRating,
    int RatingCount,
    bool HasMarkedParkingLot,
    string? RejectionReason);
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Facilities.Queries.GetNearbyFacilities;

public record NearbyFacilityItemResponse(
    Guid Id,
    string Name,
    string Address,
    double? Latitude,
    double? Longitude,
    int DistanceMeters,
    ApprovalStatusEnum ApprovalStatus,
    string ApprovalStatusDescription,
    double? AverageRating,
    int RatingCount,
    int ImageCount,
    string? FirstImageUrl,
    bool HasMarkedParkingLot,
    int TwoWheelerAvailable,
    int TwoWheelerOccupancy,
    decimal TwoWheelerPricePerHourNpr,
    int FourWheelerAvailable,
    int FourWheelerOccupancy,
    decimal FourWheelerPricePerHourNpr);

public record GetNearbyFacilitiesResponse(
    IReadOnlyList<NearbyFacilityItemResponse> Facilities);

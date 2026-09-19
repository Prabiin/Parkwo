using ParkingApp.Application.Facilities;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.BackOffice.Queries.GetParkingFacilityDetail;

public record GetParkingFacilityDetailResponse(
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
    string? ProviderOwnerName,
    string? ProviderOwnerContactNumber,
    IReadOnlyList<ParkingSpotItemResponse> Spots,
    int TwoWheelerCount,
    int FourWheelerCount,
    IReadOnlyList<ParkingFacilityImageResponse> Images,
    double? AverageRating,
    int RatingCount);
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.BackOffice.Queries.GetParkingFacilities;

public record BackOfficeParkingFacilityItemResponse(
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
    int TwoWheelerCount,
    int FourWheelerCount,
    int ImageCount,
    double? AverageRating,
    int RatingCount);

public record GetParkingFacilitiesResponse(
    IReadOnlyList<BackOfficeParkingFacilityItemResponse> ParkingFacilities);
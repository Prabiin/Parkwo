using ParkingApp.Application.Facilities;

namespace ParkingApp.Application.Facilities.Queries.GetPublicFacilityDetail;

/// <summary>
/// One vehicle type a facility serves, with what a rider can book and what it
/// costs. <see cref="PricePerHourNpr"/> is null when the lot has no space of
/// that type, so a UI never renders "Rs 0/hr" for a category that isn't served.
/// </summary>
public record PublicFacilityTypeDetail(
    int Occupancy,
    int Available,
    decimal? PricePerHourNpr);

/// <summary>
/// The rider's "View details" screen for a paid facility — the pin popup
/// carries only name, address, rating and per-type availability, so this adds
/// the description, the whole image gallery and the per-type rate. Richer than
/// the owner read and never owner-shaped: no pending capacity, land area or
/// rejection reason. Reviews come from the separate
/// <c>GET /facilities/{id}/reviews</c> read.
/// </summary>
public record GetPublicFacilityDetailResponse(
    Guid Id,
    string Name,
    string? Description,
    string Address,
    double? Latitude,
    double? Longitude,
    bool HasMarkedParkingLot,
    double? AverageRating,
    int RatingCount,
    IReadOnlyList<ParkingFacilityImageResponse> Images,
    PublicFacilityTypeDetail TwoWheeler,
    PublicFacilityTypeDetail FourWheeler);

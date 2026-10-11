using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Facilities.Queries.GetNearbyFacilities;

/// <summary>
/// Two modes in one endpoint:
/// - no vehicleType: "parkings near me" — pure spatial query (map pins).
/// - with vehicleType: "available spaces near me" — spatial + only places with
///   at least one space of that type still unclaimed.
/// Paid facilities subtract every live booking — the stay in progress or one
/// still to come, plus unexpired checkout holds (the same set
/// CreateBookingCommand counts) — so the feed can never advertise a space the
/// booking gate would refuse with a 409. Parkwo's own public free parkings
/// (FreeParkingFacility) are merged into the same list as markers with
/// IsFree = true and a zero price; they are never bookable, so no availability
/// math applies to them.
/// </summary>
public sealed record GetNearbyFacilitiesQuery(
    double Latitude,
    double Longitude,
    double? RadiusKm,
    VehicleTypeEnum? VehicleType)
    : IRequestResult<GetNearbyFacilitiesQuery, GetNearbyFacilitiesResponse>;

public sealed class GetNearbyFacilitiesQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IImageUrlResolver imageUrls,
    IOptions<ParkwoPricingSettings> pricingSettings)
    : IRequestResultHandler<GetNearbyFacilitiesQuery, GetNearbyFacilitiesResponse>
{
    private const int MaxResults = 50;

    public async Task<Result<GetNearbyFacilitiesResponse>> Handle(
        GetNearbyFacilitiesQuery request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<GetNearbyFacilitiesResponse>.Failure("Authentication required.", 401);

        var radiusMeters = (request.RadiusKm ?? GetNearbyFacilitiesQueryValidator.DefaultRadiusKm) * 1000;
        var origin = new Point(request.Longitude, request.Latitude) { SRID = 4326 };

        // The price shown is Parkwo's own rate (providers never price their
        // spaces), projected as constants so it is not translated to SQL.
        var twoWheelerRate = pricingSettings.Value.TwoWheelerPricePerHourNpr;
        var fourWheelerRate = pricingSettings.Value.FourWheelerPricePerHourNpr;

        var query = context.ParkingFacilities
            .AsNoTracking()
            .Where(f => f.ApprovalStatus == ApprovalStatusEnum.Verified
                        && f.Location != null
                        && f.Location.IsWithinDistance(origin, radiusMeters));

        if (request.VehicleType.HasValue)
        {
            query = request.VehicleType.Value switch
            {
                VehicleTypeEnum.TwoWheeler => query.Where(f => f.TwoWheelerOccupancy > 0),
                VehicleTypeEnum.FourWheeler => query.Where(f => f.FourWheelerOccupancy > 0),
                _ => query
            };
        }

        var rows = await query
            .OrderBy(f => f.Location!.Distance(origin))
            .Take(MaxResults)
            .Select(f => new
            {
                f.Id,
                f.Name,
                f.Address,
                f.Latitude,
                f.Longitude,
                DistanceRaw = f.Location!.Distance(origin),
                f.ApprovalStatus,
                f.AverageRating,
                f.RatingCount,
                ImageCount = f.Images.Count,
                FirstImageUrl = f.Images
                    .OrderBy(i => i.SortOrder)
                    .Select(i => i.Url)
                    .FirstOrDefault(),
                f.HasMarkedParkingLot,
                f.TwoWheelerOccupancy,
                f.FourWheelerOccupancy
            })
            .ToListAsync(cancellationToken);

        // Parkwo's own free parkings: markers only, so the type filter is "does
        // it declare capacity for this type" — a null count means unknown, which
        // we keep (the place is free, so it is not turned away for that).
        var freeQuery = context.FreeParkingFacilities
            .AsNoTracking()
            .Where(f => f.IsActive
                        && !f.IsDeleted
                        && f.Location != null
                        && f.Location.IsWithinDistance(origin, radiusMeters));

        if (request.VehicleType == VehicleTypeEnum.TwoWheeler)
            freeQuery = freeQuery.Where(f => f.TwoWheelerOccupancy == null || f.TwoWheelerOccupancy > 0);
        else if (request.VehicleType == VehicleTypeEnum.FourWheeler)
            freeQuery = freeQuery.Where(f => f.FourWheelerOccupancy == null || f.FourWheelerOccupancy > 0);

        var freeRows = await freeQuery
            .OrderBy(f => f.Location!.Distance(origin))
            .Take(MaxResults)
            .Select(f => new
            {
                f.Id,
                f.Name,
                f.Address,
                f.Latitude,
                f.Longitude,
                DistanceRaw = f.Location!.Distance(origin),
                ImageCount = f.Images.Count,
                FirstImageUrl = f.Images
                    .OrderBy(i => i.SortOrder)
                    .Select(i => i.Url)
                    .FirstOrDefault(),
                f.TwoWheelerOccupancy,
                f.FourWheelerOccupancy
            })
            .ToListAsync(cancellationToken);

        // Every space already spoken for is subtracted from what this feed
        // advertises — a paid booking (stay in progress or still to come) and an
        // unexpired checkout hold, the same set CreateBookingCommand counts, so
        // the list and the booking gate agree.
        var now = DateTimeOffset.UtcNow;
        var takenSpaces = await FacilityAvailability.LoadTakenSpacesAsync(
            context,
            rows.Select(r => r.Id).ToList(),
            now,
            cancellationToken);

        var paidItems = rows
            .Select(f =>
            {
                var twoWheelerAvailable = FacilityAvailability.Available(
                    f.TwoWheelerOccupancy, takenSpaces, f.Id, VehicleTypeEnum.TwoWheeler);
                var fourWheelerAvailable = FacilityAvailability.Available(
                    f.FourWheelerOccupancy, takenSpaces, f.Id, VehicleTypeEnum.FourWheeler);

                return new NearbyFacilityItemResponse(
                    f.Id,
                    f.Name,
                    f.Address,
                    f.Latitude,
                    f.Longitude,
                    (int)Math.Round(f.DistanceRaw),
                    f.ApprovalStatus,
                    f.ApprovalStatus.ToDescription(),
                    f.AverageRating,
                    f.RatingCount,
                    f.ImageCount,
                    imageUrls.Resolve(f.FirstImageUrl),
                    f.HasMarkedParkingLot,
                    twoWheelerAvailable,
                    f.TwoWheelerOccupancy,
                    twoWheelerRate,
                    fourWheelerAvailable,
                    f.FourWheelerOccupancy,
                    fourWheelerRate,
                    false);
            })
            // "Available spaces near me": drop paid lots whose remaining count
            // for the requested type has hit zero. They still appear in the
            // plain "parkings near me" mode (no vehicleType) as map pins.
            .Where(f => request.VehicleType is null
                        || (request.VehicleType == VehicleTypeEnum.TwoWheeler
                            ? f.TwoWheelerAvailable > 0
                            : f.FourWheelerAvailable > 0));

        // Free parkings carry no provider, no approval and no bookings. The
        // approval/rating fields are placeholders the app should ignore when
        // IsFree is true; price is 0 and "available" mirrors the declared count.
        var freeItems = freeRows
            .Select(f => new NearbyFacilityItemResponse(
                f.Id,
                f.Name,
                f.Address,
                f.Latitude,
                f.Longitude,
                (int)Math.Round(f.DistanceRaw),
                ApprovalStatusEnum.Verified,
                ApprovalStatusEnum.Verified.ToDescription(),
                0d,
                0,
                f.ImageCount,
                imageUrls.Resolve(f.FirstImageUrl),
                false,
                f.TwoWheelerOccupancy ?? 0,
                f.TwoWheelerOccupancy ?? 0,
                0m,
                f.FourWheelerOccupancy ?? 0,
                f.FourWheelerOccupancy ?? 0,
                0m,
                true));

        var items = paidItems
            .Concat(freeItems)
            .OrderBy(f => f.DistanceMeters)
            .Take(MaxResults)
            .ToList();

        return Result<GetNearbyFacilitiesResponse>.Success(
            new GetNearbyFacilitiesResponse(items));
    }
}

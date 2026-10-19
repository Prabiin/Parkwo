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
/// Rider discovery, two modes in one endpoint:
/// - no query: "parkings near me" — Verified lots within
///   <see cref="NearbySettings.NearMeRadiusKm"/> of the supplied point (map
///   pins). The radius is fixed server-side: nobody walks kilometres to park,
///   and a rider who wants another area searches for it instead of widening
///   the circle.
/// - with query: name/address search — the point is used only to measure
///   distance and break ties, never to filter, so a rider in Boudha can find a
///   lot in Durbarmarg. Both sides normalise (lowercase, spaces removed) so
///   "Durbarmarg" finds "Durbar Marg" — see <see cref="FacilityTextSearch"/>.
/// Both modes keep the vehicleType filter, subtract every live booking — the
/// stay in progress or one still to come, plus unexpired checkout holds (the
/// same set CreateBookingCommand counts) — so the feed can never advertise a
/// space the booking gate would refuse with a 409. Parkwo's own public free
/// parkings (FreeParkingFacility) are merged into the same list as markers with
/// IsFree = true and a zero price; they are never bookable, so no availability
/// math applies to them.
/// </summary>
public sealed record GetNearbyFacilitiesQuery(
    double Latitude,
    double Longitude,
    VehicleTypeEnum? VehicleType,
    string? Query)
    : IRequestResult<GetNearbyFacilitiesQuery, GetNearbyFacilitiesResponse>;

public sealed class GetNearbyFacilitiesQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IImageUrlResolver imageUrls,
    IOptions<ParkwoPricingSettings> pricingSettings,
    IOptions<NearbySettings> nearbySettings)
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

        // Search mode is decided by the text, not by the map: when a term is
        // present the position drops to a distance calculator (and tie-breaker)
        // so the match is found wherever the facility is.
        string? searchTerm = null;
        if (FacilityTextSearch.HasQuery(request.Query))
            searchTerm = FacilityTextSearch.Normalize(request.Query!);

        var origin = new Point(request.Longitude, request.Latitude) { SRID = 4326 };

        // A zero or negative radius would silently empty the map, so a bad
        // config value falls back to the shipped default instead of hiding
        // every pin (the handler is the only place this setting is read).
        var configuredRadiusKm = nearbySettings.Value.NearMeRadiusKm;
        var radiusMeters = (configuredRadiusKm > 0 ? configuredRadiusKm : NearbySettings.DefaultNearMeRadiusKm) * 1000;

        // The price shown is Parkwo's own rate (providers never price their
        // spaces), projected as constants so it is not translated to SQL.
        var twoWheelerRate = pricingSettings.Value.TwoWheelerPricePerHourNpr;
        var fourWheelerRate = pricingSettings.Value.FourWheelerPricePerHourNpr;

        var query = context.ParkingFacilities
            .AsNoTracking()
            .Where(f => f.ApprovalStatus == ApprovalStatusEnum.Verified
                        && f.Location != null);

        if (searchTerm is not null)
        {
            // Normalised on both sides, so the SQL "lower/replace" must stay in
            // step with FacilityTextSearch.Normalize. EF renders Contains as a
            // LIKE pattern and escapes the term itself, so a rider's "%" or "_"
            // stays a character rather than becoming a wildcard.
            var term = searchTerm;
            query = query.Where(f => f.Name.ToLower().Replace(" ", "").Contains(term)
                                     || f.Address.ToLower().Replace(" ", "").Contains(term));
        }
        else
        {
            query = query.Where(f => f.Location!.IsWithinDistance(origin, radiusMeters));
        }

        if (request.VehicleType.HasValue)
        {
            query = request.VehicleType.Value switch
            {
                VehicleTypeEnum.TwoWheeler => query.Where(f => f.TwoWheelerOccupancy > 0),
                VehicleTypeEnum.FourWheeler => query.Where(f => f.FourWheelerOccupancy > 0),
                _ => query
            };
        }

        var paidProjection = query.Select(f => new
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
        });

        // Near me: the ring is small, so SQL can take the nearest 50. Search:
        // match quality must beat distance, and the term already bounds the
        // candidate set (facilities are hand-verified, so it is small) — order
        // in memory after the fetch.
        var rows = searchTerm is null
            ? await paidProjection
                .OrderBy(f => f.DistanceRaw)
                .Take(MaxResults)
                .ToListAsync(cancellationToken)
            : await paidProjection.ToListAsync(cancellationToken);

        // Parkwo's own free parkings: markers only, so the type filter is "does
        // it declare capacity for this type" — a null count means unknown, which
        // we keep (the place is free, so it is not turned away for that).
        var freeQuery = context.FreeParkingFacilities
            .AsNoTracking()
            .Where(f => f.IsActive
                        && !f.IsDeleted
                        && f.Location != null);

        if (searchTerm is not null)
        {
            var term = searchTerm;
            freeQuery = freeQuery.Where(f => f.Name.ToLower().Replace(" ", "").Contains(term)
                                             || f.Address.ToLower().Replace(" ", "").Contains(term));
        }
        else
        {
            freeQuery = freeQuery.Where(f => f.Location!.IsWithinDistance(origin, radiusMeters));
        }

        if (request.VehicleType == VehicleTypeEnum.TwoWheeler)
            freeQuery = freeQuery.Where(f => f.TwoWheelerOccupancy == null || f.TwoWheelerOccupancy > 0);
        else if (request.VehicleType == VehicleTypeEnum.FourWheeler)
            freeQuery = freeQuery.Where(f => f.FourWheelerOccupancy == null || f.FourWheelerOccupancy > 0);

        var freeProjection = freeQuery.Select(f => new
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
        });

        var freeRows = searchTerm is null
            ? await freeProjection
                .OrderBy(f => f.DistanceRaw)
                .Take(MaxResults)
                .ToListAsync(cancellationToken)
            : await freeProjection.ToListAsync(cancellationToken);

        // Every space already spoken for is subtracted from what this feed
        // advertises — a paid booking that overlaps the pin's window (the stay
        // in progress, or one starting within the hour) and an unexpired
        // checkout hold, the same set CreateBookingCommand counts, so the list
        // and the booking gate agree. Future-only bookings are ignored: a stay
        // at 8 PM must not hide a lot that is free at 3 PM.
        var now = DateTimeOffset.UtcNow;
        var window = AvailabilityWindows.StartingNow(now);
        var takenSpaces = await FacilityAvailability.LoadTakenSpacesAsync(
            context,
            rows.Select(r => r.Id).ToList(),
            window,
            now,
            cancellationToken);

        var paidItems = rows
            .Select(f =>
            {
                var twoWheelerAvailable = FacilityAvailability.Available(
                    f.TwoWheelerOccupancy, takenSpaces, f.Id, VehicleTypeEnum.TwoWheeler);
                var fourWheelerAvailable = FacilityAvailability.Available(
                    f.FourWheelerOccupancy, takenSpaces, f.Id, VehicleTypeEnum.FourWheeler);

                var item = new NearbyFacilityItemResponse(
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

                var rank = searchTerm is null ? 0 : FacilityTextSearch.Rank(f.Name, searchTerm);
                return (Item: item, Rank: rank);
            })
            // "Available spaces near me": drop paid lots whose remaining count
            // for the requested type has hit zero. They still appear in the
            // plain "parkings near me" mode (no vehicleType) as map pins.
            .Where(f => request.VehicleType is null
                        || (request.VehicleType == VehicleTypeEnum.TwoWheeler
                            ? f.Item.TwoWheelerAvailable > 0
                            : f.Item.FourWheelerAvailable > 0));

        // Free parkings carry no provider, no approval and no bookings. The
        // approval/rating fields are placeholders the app should ignore when
        // IsFree is true; price is 0 and "available" mirrors the declared count.
        var freeItems = freeRows
            .Select(f =>
            {
                var item = new NearbyFacilityItemResponse(
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
                    true);

                var rank = searchTerm is null ? 0 : FacilityTextSearch.Rank(f.Name, searchTerm);
                return (Item: item, Rank: rank);
            });

        // In "near me" every rank is 0, so this is the old nearest-first order.
        // In search the best name match leads, address-only matches trail, and
        // distance separates equals.
        var items = paidItems
            .Concat(freeItems)
            .OrderBy(f => f.Rank)
            .ThenBy(f => f.Item.DistanceMeters)
            .Take(MaxResults)
            .Select(f => f.Item)
            .ToList();

        return Result<GetNearbyFacilitiesResponse>.Success(
            new GetNearbyFacilitiesResponse(items));
    }
}

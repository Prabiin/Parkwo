using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Facilities.Queries.GetNearbyFacilities;

/// <summary>
/// Two modes in one endpoint:
/// - no vehicleType: "parkings near me" — pure spatial query (map pins).
/// - with vehicleType: "available spaces near me" — spatial + occupancy filter,
///   only facilities claiming at least one space of that type.
/// Available equals occupancy until bookings land; overlapping bookings will be
/// subtracted here once they exist (TODO(bookings)).
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

        var items = rows
            .Select(f => new NearbyFacilityItemResponse(
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
                f.FirstImageUrl,
                f.HasMarkedParkingLot,
                f.TwoWheelerOccupancy,
                f.TwoWheelerOccupancy,
                twoWheelerRate,
                f.FourWheelerOccupancy,
                f.FourWheelerOccupancy,
                fourWheelerRate))
            .ToList();

        return Result<GetNearbyFacilitiesResponse>.Success(
            new GetNearbyFacilitiesResponse(items));
    }
}

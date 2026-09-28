using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Facilities.Queries.GetNearbyFacilities;

/// <summary>
/// Two modes in one endpoint:
/// - no vehicleType: "parkings near me" — pure spatial query (map pins).
/// - with vehicleType: "available spaces near me" — spatial + capacity filter,
///   only facilities with at least one free spot of that type.
/// Free counts currently equal active spots; overlapping bookings will be
/// subtracted here once bookings exist (TODO(bookings)).
/// </summary>
public sealed record GetNearbyFacilitiesQuery(
    double Latitude,
    double Longitude,
    double? RadiusKm,
    VehicleTypeEnum? VehicleType)
    : IRequestResult<GetNearbyFacilitiesQuery, GetNearbyFacilitiesResponse>;

public sealed class GetNearbyFacilitiesQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
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

        var query = context.ParkingFacilities
            .AsNoTracking()
            .Where(f => f.ApprovalStatus == ApprovalStatusEnum.Verified
                        && f.Location != null
                        && f.Location.IsWithinDistance(origin, radiusMeters));

        if (request.VehicleType.HasValue)
        {
            var type = request.VehicleType.Value;
            query = query.Where(f => f.Spots.Any(s => s.VehicleType == type && s.IsActive));
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
                TwoWheelerTotal = f.Spots.Count(s =>
                    s.VehicleType == VehicleTypeEnum.TwoWheeler && s.IsActive),
                TwoWheelerFromPrice = f.Spots
                    .Where(s => s.VehicleType == VehicleTypeEnum.TwoWheeler && s.IsActive)
                    .Min(s => (decimal?)s.PricePerHourNpr),
                FourWheelerTotal = f.Spots.Count(s =>
                    s.VehicleType == VehicleTypeEnum.FourWheeler && s.IsActive),
                FourWheelerFromPrice = f.Spots
                    .Where(s => s.VehicleType == VehicleTypeEnum.FourWheeler && s.IsActive)
                    .Min(s => (decimal?)s.PricePerHourNpr)
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
                f.TwoWheelerTotal,
                f.TwoWheelerTotal,
                f.TwoWheelerFromPrice,
                f.FourWheelerTotal,
                f.FourWheelerTotal,
                f.FourWheelerFromPrice))
            .ToList();

        return Result<GetNearbyFacilitiesResponse>.Success(
            new GetNearbyFacilitiesResponse(items));
    }
}

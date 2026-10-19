using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Facilities.Queries.GetPublicFacilityDetail;

/// <summary>
/// Rider-facing facility detail for the pin's "View details" screen. Only
/// <c>Verified</c> lots are visible — anything else answers 404 as if it never
/// existed. When the rider's <see cref="VehicleType"/> is supplied, a lot that
/// serves no space of that type is also 404, so a car-only lot never opens for a
/// bike rider. Availability is the pin's window, <c>[now, now + 1h)</c>, so this
/// screen and the map agree.
/// </summary>
public sealed record GetPublicFacilityDetailQuery(Guid FacilityId, VehicleTypeEnum? VehicleType)
    : IRequestResult<GetPublicFacilityDetailQuery, GetPublicFacilityDetailResponse>;

public sealed class GetPublicFacilityDetailQueryHandler(
    IApplicationDbContext context,
    IImageUrlResolver imageUrls,
    IOptions<ParkwoPricingSettings> pricingSettings)
    : IRequestResultHandler<GetPublicFacilityDetailQuery, GetPublicFacilityDetailResponse>
{
    public async Task<Result<GetPublicFacilityDetailResponse>> Handle(
        GetPublicFacilityDetailQuery request,
        CancellationToken cancellationToken = default)
    {
        var facility = await context.ParkingFacilities
            .AsNoTracking()
            .Where(f => f.Id == request.FacilityId
                        && f.ApprovalStatus == ApprovalStatusEnum.Verified)
            .Select(f => new
            {
                f.Id,
                f.Name,
                f.Description,
                f.Address,
                f.Latitude,
                f.Longitude,
                f.HasMarkedParkingLot,
                f.AverageRating,
                f.RatingCount,
                f.TwoWheelerOccupancy,
                f.FourWheelerOccupancy
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (facility is null)
            return Result<GetPublicFacilityDetailResponse>.Failure("Parking facility not found.", 404);

        // Same 404, deliberately: a rider looking for a type this lot does not
        // serve should never learn that the lot exists.
        if (request.VehicleType is { } vehicleType
            && !FacilityAvailability.ServesType(
                facility.TwoWheelerOccupancy, facility.FourWheelerOccupancy, vehicleType))
            return Result<GetPublicFacilityDetailResponse>.Failure("Parking facility not found.", 404);

        var images = await context.ParkingFacilityImages
            .AsNoTracking()
            .Where(i => i.FacilityId == request.FacilityId)
            .OrderBy(i => i.SortOrder)
            .Select(i => new
            {
                i.Id,
                i.Url,
                i.FileName,
                i.ContentType,
                i.SizeInBytes,
                i.SortOrder
            })
            .ToListAsync(cancellationToken);

        var imageItems = images
            .Select(i => new ParkingFacilityImageResponse(
                i.Id,
                imageUrls.Resolve(i.Url)!,
                i.FileName,
                i.ContentType,
                i.SizeInBytes,
                i.SortOrder))
            .ToList();

        var now = DateTimeOffset.UtcNow;
        var takenSpaces = await FacilityAvailability.LoadTakenSpacesAsync(
            context,
            [facility.Id],
            AvailabilityWindows.StartingNow(now),
            now,
            cancellationToken);

        // PricePerHourFor returns null for a type the lot does not offer, so an
        // unsupported category reads as "not offered" rather than "Rs 0".
        var twoWheeler = new PublicFacilityTypeDetail(
            facility.TwoWheelerOccupancy,
            FacilityAvailability.Available(
                facility.TwoWheelerOccupancy, takenSpaces, facility.Id, VehicleTypeEnum.TwoWheeler),
            BookingPricing.PricePerHourFor(
                VehicleTypeEnum.TwoWheeler,
                facility.TwoWheelerOccupancy,
                pricingSettings.Value.TwoWheelerPricePerHourNpr));

        var fourWheeler = new PublicFacilityTypeDetail(
            facility.FourWheelerOccupancy,
            FacilityAvailability.Available(
                facility.FourWheelerOccupancy, takenSpaces, facility.Id, VehicleTypeEnum.FourWheeler),
            BookingPricing.PricePerHourFor(
                VehicleTypeEnum.FourWheeler,
                facility.FourWheelerOccupancy,
                pricingSettings.Value.FourWheelerPricePerHourNpr));

        return Result<GetPublicFacilityDetailResponse>.Success(
            new GetPublicFacilityDetailResponse(
                facility.Id,
                facility.Name,
                facility.Description,
                facility.Address,
                facility.Latitude,
                facility.Longitude,
                facility.HasMarkedParkingLot,
                facility.AverageRating,
                facility.RatingCount,
                imageItems,
                twoWheeler,
                fourWheeler));
    }
}

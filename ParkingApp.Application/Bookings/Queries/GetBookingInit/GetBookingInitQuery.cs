using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Vehicles.Queries.GetVehicles;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Bookings.Queries.GetBookingInit;

/// <summary>
/// Loads the "Book a spot" screen: the parking's name, the spaces it has per
/// vehicle type, and the rider's vehicles that this lot can actually take. The
/// facility comes from the pin that was tapped. A lot that is not Verified is
/// unknown here, same as anywhere else a rider looks.
/// </summary>
public sealed record GetBookingInitQuery(Guid FacilityId)
    : IRequestResult<GetBookingInitQuery, GetBookingInitResponse>;

public sealed class GetBookingInitQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestResultHandler<GetBookingInitQuery, GetBookingInitResponse>
{
    public async Task<Result<GetBookingInitResponse>> Handle(
        GetBookingInitQuery request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<GetBookingInitResponse>.Failure("Authentication required.", 401);

        var facility = await context.ParkingFacilities
            .AsNoTracking()
            .Where(f => f.Id == request.FacilityId
                        && f.ApprovalStatus == ApprovalStatusEnum.Verified)
            .Select(f => new { f.Name, f.TwoWheelerOccupancy, f.FourWheelerOccupancy })
            .FirstOrDefaultAsync(cancellationToken);

        if (facility is null)
            return Result<GetBookingInitResponse>.Failure("Parking facility not found.", 404);

        // The types this lot can take. A car-only lot drops out of a bike
        // rider's picker here, and the pin feed already kept it off the map, so
        // no request ever reaches a screen that would show a 409 or Rs 0/hr.
        var servedTypes = new List<VehicleTypeEnum>(2);
        if (facility.TwoWheelerOccupancy > 0)
            servedTypes.Add(VehicleTypeEnum.TwoWheeler);
        if (facility.FourWheelerOccupancy > 0)
            servedTypes.Add(VehicleTypeEnum.FourWheeler);

        var vehicles = await context.Vehicles
            .AsNoTracking()
            .Where(v => v.UserId == userId && servedTypes.Contains(v.VehicleType))
            .OrderByDescending(v => v.CreatedAtUtc)
            .Select(v => new { v.Id, v.VehicleType, v.VehicleCategory, v.Name, v.VehicleNumber, v.Brand, v.Model, v.Color })
            .ToListAsync(cancellationToken);

        var items = vehicles
            .Select(v => new VehicleItemResponse(
                v.Id,
                v.VehicleType,
                v.VehicleType.ToDescription(),
                v.VehicleCategory,
                v.VehicleCategory.ToDescription(),
                v.Name,
                v.VehicleNumber,
                v.Brand,
                v.Model,
                v.Color))
            .ToList();

        return Result<GetBookingInitResponse>.Success(
            new GetBookingInitResponse(
                request.FacilityId,
                facility.Name,
                facility.TwoWheelerOccupancy,
                facility.FourWheelerOccupancy,
                items,
                items.Count > 0));
    }
}

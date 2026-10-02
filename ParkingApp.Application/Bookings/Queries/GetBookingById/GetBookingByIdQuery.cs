using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Bookings.Queries.GetBookingById;

public sealed record GetBookingByIdQuery(Guid BookingId)
    : IRequestResult<GetBookingByIdQuery, GetBookingByIdResponse>;

public sealed class GetBookingByIdQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestResultHandler<GetBookingByIdQuery, GetBookingByIdResponse>
{
    public async Task<Result<GetBookingByIdResponse>> Handle(
        GetBookingByIdQuery request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<GetBookingByIdResponse>.Failure("Authentication required.", 401);

        var booking = await context.Bookings
            .AsNoTracking()
            .Where(b => b.Id == request.BookingId)
            .Select(b => new
            {
                b.Id,
                b.Status,
                b.StartsAtUtc,
                b.EndsAtUtc,
                b.VehicleType,
                b.PricePerHourNpr,
                b.BillableHours,
                b.TotalAmountNpr,
                b.PlatformFeeNpr,
                b.ProviderAmountNpr,
                b.HoldExpiresAtUtc,
                b.ConfirmedAtUtc,
                b.CancelledAtUtc,
                b.CancellationReason,
                b.FacilityId,
                FacilityName = b.Facility!.Name,
                b.VehicleId,
                VehicleNumber = b.Vehicle!.VehicleNumber,
                UserId = b.UserId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (booking is null)
            return Result<GetBookingByIdResponse>.Failure("Booking not found.", 404);

        if (booking.UserId != userId.Value)
            return Result<GetBookingByIdResponse>.Failure("You can only view your own booking.", 403);

        return Result<GetBookingByIdResponse>.Success(
            new GetBookingByIdResponse(
                booking.Id,
                booking.Status,
                booking.Status.ToDescription(),
                booking.FacilityId,
                booking.FacilityName,
                booking.VehicleId,
                booking.VehicleNumber,
                booking.VehicleType,
                booking.VehicleType.ToDescription(),
                booking.StartsAtUtc,
                booking.EndsAtUtc,
                booking.PricePerHourNpr,
                booking.BillableHours,
                booking.TotalAmountNpr,
                booking.PlatformFeeNpr,
                booking.ProviderAmountNpr,
                booking.HoldExpiresAtUtc,
                booking.ConfirmedAtUtc,
                booking.CancelledAtUtc,
                booking.CancellationReason));
    }
}
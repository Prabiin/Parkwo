using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Bookings.Queries.GetBookingSummary;

/// <summary>
/// "You are about to pay NPR X for this booking" — shown after booking, before
/// the prepaid payment starts. Read-only and safe to reopen; the payment
/// itself is started separately.
/// </summary>
public sealed record GetBookingSummaryQuery(Guid BookingId)
    : IRequestResult<GetBookingSummaryQuery, GetBookingSummaryResponse>;

public sealed class GetBookingSummaryQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestResultHandler<GetBookingSummaryQuery, GetBookingSummaryResponse>
{
    public async Task<Result<GetBookingSummaryResponse>> Handle(
        GetBookingSummaryQuery request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<GetBookingSummaryResponse>.Failure("Authentication required.", 401);

        var booking = await context.Bookings
            .AsNoTracking()
            .Where(b => b.Id == request.BookingId)
            .Select(b => new
            {
                b.Id,
                b.UserId,
                b.Status,
                b.StartsAtUtc,
                b.EndsAtUtc,
                b.HoldExpiresAtUtc,
                b.PricePerHourNpr,
                b.BillableHours,
                b.TotalAmountNpr,
                b.PlatformFeeNpr,
                b.ProviderAmountNpr,
                b.VehicleType,
                FacilityName = b.Facility != null ? b.Facility.Name : "",
                VehicleName = b.Vehicle != null ? b.Vehicle.Name : "",
                VehicleNumber = b.Vehicle != null ? b.Vehicle.VehicleNumber : ""
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (booking is null)
            return Result<GetBookingSummaryResponse>.Failure("Booking not found.", 404);

        if (booking.UserId != userId.Value)
            return Result<GetBookingSummaryResponse>.Failure(
                "You can only view your own booking.", 403);

        // This screen exists to confirm a payment, so it is only meaningful
        // while the booking is actually waiting to be paid.
        if (booking.Status != BookingStatusEnum.PendingPayment)
            return Result<GetBookingSummaryResponse>.Failure(
                "This booking is no longer waiting for payment.", 409);

        return Result<GetBookingSummaryResponse>.Success(
            new GetBookingSummaryResponse(
                booking.Id,
                booking.FacilityName,
                booking.VehicleName,
                booking.VehicleNumber,
                booking.VehicleType.ToDescription(),
                LocalTime.Format(booking.StartsAtUtc),
                LocalTime.Format(booking.EndsAtUtc),
                booking.PricePerHourNpr,
                booking.BillableHours,
                booking.TotalAmountNpr,
                booking.PlatformFeeNpr,
                booking.ProviderAmountNpr,
                "NPR",
                LocalTime.Format(booking.HoldExpiresAtUtc)));
    }
}
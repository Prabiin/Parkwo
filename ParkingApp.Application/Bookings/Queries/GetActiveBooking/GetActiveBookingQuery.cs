using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Bookings.Queries.Shared;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Bookings.Queries.GetActiveBooking;

/// <summary>
/// What the Home and Bookings cards need in one call: the booking the rider
/// should be looking at right now, and any overstay they still owe.
/// </summary>
public sealed record GetActiveBookingQuery
    : IRequestResult<GetActiveBookingQuery, GetActiveBookingResponse>;

/// <param name="Current">
/// The vehicle that is parked right now; else the soonest paid booking that
/// has not ended; else an unpaid hold that has not lapsed. Null when none.
/// </param>
/// <param name="UnsettledOverstay">
/// The oldest completed booking whose overstay charge is unpaid. While this
/// is set, creating a new booking is refused (409).
/// </param>
public sealed record GetActiveBookingResponse(
    RiderBookingItemResponse? Current,
    RiderBookingItemResponse? UnsettledOverstay);

public sealed class GetActiveBookingQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestResultHandler<GetActiveBookingQuery, GetActiveBookingResponse>
{
    public async Task<Result<GetActiveBookingResponse>> Handle(
        GetActiveBookingQuery request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<GetActiveBookingResponse>.Failure("Authentication required.", 401);

        var now = DateTimeOffset.UtcNow;

        var mine = context.Bookings
            .AsNoTracking()
            .Where(b => b.UserId == userId && !b.IsDeleted);

        // Parked beats paid-and-upcoming, which beats an unpaid hold.
        var current = await RiderBookings.Project(context, mine
                .Where(b => b.Status == BookingStatusEnum.Active
                            || (b.Status == BookingStatusEnum.Confirmed && b.EndsAtUtc > now)
                            || (b.Status == BookingStatusEnum.PendingPayment && b.HoldExpiresAtUtc > now))
                .OrderBy(b => b.Status == BookingStatusEnum.Active ? 0
                    : b.Status == BookingStatusEnum.Confirmed ? 1 : 2)
                .ThenBy(b => b.StartsAtUtc)
                .Take(1))
            .FirstOrDefaultAsync(cancellationToken);

        var unsettled = await RiderBookings.Project(context, mine
                .Where(b => b.OverstayAmountPaisa != null
                            && !context.OverstayPayments.Any(p => p.BookingId == b.Id
                                && (p.Status == PaymentStatusEnum.Completed
                                    || p.Status == PaymentStatusEnum.Refunded)))
                .OrderBy(b => b.StartsAtUtc)
                .Take(1))
            .FirstOrDefaultAsync(cancellationToken);

        return Result<GetActiveBookingResponse>.Success(new GetActiveBookingResponse(
            current is null ? null : RiderBookings.ToResponse(current, now),
            unsettled is null ? null : RiderBookings.ToResponse(unsettled, now)));
    }
}

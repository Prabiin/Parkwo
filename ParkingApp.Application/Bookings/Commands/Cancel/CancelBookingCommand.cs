using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Bookings.Commands.Cancel;

/// <summary>
/// Rider-initiated cancel. Releasing an unpaid hold or a paid-but-unused
/// booking frees the space. No money moves here: a refund needs the refund
/// policy (cutoff, partial rate, provider share), which is still undecided,
/// so a cancelled-paid booking is left for BackOffice to settle.
/// </summary>
public sealed record CancelBookingCommand(Guid BookingId)
    : IRequestResult<CancelBookingCommand, Unit>;

public sealed class CancelBookingCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestResultHandler<CancelBookingCommand, Unit>
{
    public async Task<Result<Unit>> Handle(
        CancelBookingCommand request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<Unit>.Failure("Authentication required.", 401);

        var booking = await context.Bookings
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking is null)
            return Result<Unit>.Failure("Booking not found.", 404);

        if (booking.UserId != userId.Value)
            return Result<Unit>.Failure("You can only cancel your own booking.", 403);

        if (!BookingTransitions.IsAllowed(booking.Status, BookingStatusEnum.Cancelled))
            return Result<Unit>.Failure($"A {booking.Status.ToDescription().ToLowerInvariant()} booking cannot be cancelled.", 409);

        booking.Cancel("Cancelled by rider");

        // Abandoned payments die with the hold; there is nothing to refund.
        var payments = await context.Payments
            .Where(p => p.BookingId == booking.Id
                        && p.Status == PaymentStatusEnum.Initiated)
            .ToListAsync(cancellationToken);

        foreach (var payment in payments)
            payment.MarkCancelled();

        await context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
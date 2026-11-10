using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Bookings.Commands.DeleteWalkIn;

/// <summary>
/// Removes a walk-in from the gate's board without destroying its history. A
/// walk-in may be deleted once it no longer occupies a space: either it was
/// opened by mistake and never admitted, or the vehicle has left (and any
/// overstay it owes has been settled). A vehicle still parked is refused —
/// the exit scan is what measures the stay and prices the overstay, so deleting
/// the row must never stand in for it.
/// </summary>
public sealed record DeleteWalkInCommand(
    Guid BookingId,
    Guid FacilityId)
    : IRequestResult<DeleteWalkInCommand, Unit>;

public sealed class DeleteWalkInCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestResultHandler<DeleteWalkInCommand, Unit>
{
    public async Task<Result<Unit>> Handle(
        DeleteWalkInCommand request,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.StaffId;
        if (staffId is null)
            return Result<Unit>.Failure("Authentication required.", 401);

        if (!await GateAccess.CanScanAsync(context, staffId.Value, request.FacilityId, cancellationToken))
            return Result<Unit>.Failure(
                "You are not authorised to delete walk-ins at this facility.", 403);

        var booking = await context.Bookings
            .FirstOrDefaultAsync(b => b.Id == request.BookingId
                                      && b.IsWalkInBooking
                                      && b.FacilityId == request.FacilityId
                                      && !b.IsDeleted,
                cancellationToken);

        if (booking is null)
            return Result<Unit>.Failure("Walk-in booking not found.", 404);

        // Still parked: the space is in use and the exit scan has not happened.
        // Refuse rather than let a delete erase a live stay.
        if (booking.Status == BookingStatusEnum.Active)
            return Result<Unit>.Failure(
                "This walk-in is still parked. Record the exit scan before deleting it.", 409);

        // Money already taken for an overstay cannot be orphaned by deleting the
        // stay it settled. Same "settled" test the booking flow uses, so the two
        // can never disagree.
        if (booking.OverstayAmountPaisa is not null)
        {
            var overstaySettled = await context.OverstayPayments
                .AnyAsync(p => p.BookingId == booking.Id
                               && (p.Status == PaymentStatusEnum.Completed
                                   || p.Status == PaymentStatusEnum.Refunded),
                    cancellationToken);

            if (!overstaySettled)
                return Result<Unit>.Failure(
                    "This walk-in still has an unsettled overstay. Settle it before deleting the booking.", 409);
        }

        // Abandoned attempts die with the deleted hold, exactly as they do when
        // a rider cancels. A settled payment stays on the row as financial
        // history, even though the stay is hidden.
        var pendingPayments = await context.Payments
            .Where(p => p.BookingId == booking.Id
                        && p.Status == PaymentStatusEnum.Initiated)
            .ToListAsync(cancellationToken);

        foreach (var payment in pendingPayments)
            payment.MarkCancelled();

        booking.MarkDeleted(DateTimeOffset.UtcNow);

        await context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}

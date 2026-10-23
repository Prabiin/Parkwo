using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Payments.Commands.VerifyWalkIn;

/// <summary>
/// Staff override for "the rider has paid but the gateway callback has not
/// landed yet" — the normal case on a laggy connection at a barrier. Marks the
/// walk-in's latest attempt settled on a staff member's visual confirmation,
/// then admits the vehicle: for a walk-in, acknowledging the money is the entry
/// (see <see cref="Domain.Booking.ActivateWalkIn"/>). Recorded with the staff
/// id, so the settlement stays auditable: a later callback reconciles as a
/// no-op rather than a second charge.
/// </summary>
public sealed record VerifyWalkInPaymentCommand(
    Guid BookingId,
    Guid FacilityId)
    : IRequestResult<VerifyWalkInPaymentCommand, VerifyWalkInPaymentResponse>;

public sealed record VerifyWalkInPaymentResponse(
    Guid BookingId,
    Guid PaymentId,
    PaymentStatusEnum PaymentStatus,
    string PaymentStatusDescription,
    bool VerifiedManually,
    BookingStatusEnum BookingStatus,
    string BookingStatusDescription,
    bool AlreadySettled,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    DateTimeOffset? EnteredAtUtc);

public sealed class VerifyWalkInPaymentCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestResultHandler<VerifyWalkInPaymentCommand, VerifyWalkInPaymentResponse>
{
    public async Task<Result<VerifyWalkInPaymentResponse>> Handle(
        VerifyWalkInPaymentCommand request,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.StaffId;
        if (staffId is null)
            return Result<VerifyWalkInPaymentResponse>.Failure("Authentication required.", 401);

        if (!await GateAccess.CanScanAsync(context, staffId.Value, request.FacilityId, cancellationToken))
            return Result<VerifyWalkInPaymentResponse>.Failure(
                "You are not authorised to verify payments at this facility.", 403);

        // The server's clock is the billing clock, here as at the gate.
        var now = DateTimeOffset.UtcNow;

        var booking = await context.Bookings
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking is null)
            return Result<VerifyWalkInPaymentResponse>.Failure("Booking not found.", 404);

        if (!booking.IsWalkInBooking || booking.FacilityId != request.FacilityId)
            return Result<VerifyWalkInPaymentResponse>.Failure("This is not a walk-in booking at this facility.", 404);

        var payments = await context.Payments
            .Where(p => p.BookingId == booking.Id)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var settled = payments.FirstOrDefault(p => p.IsSettled);

        // A real settlement (webhook or an earlier manual verify) always wins.
        // Re-confirm the booking if a callback settled the money but somehow did
        // not move the booking, then admit the vehicle if that has not happened
        // yet, and answer idempotently.
        if (settled is not null)
        {
            if (booking.Status == BookingStatusEnum.PendingPayment
                && BookingTransitions.IsAllowed(booking.Status, BookingStatusEnum.Confirmed))
            {
                booking.Confirm();
            }

            Admit(booking, now, staffId.Value);
            await context.SaveChangesAsync(cancellationToken);

            return Result<VerifyWalkInPaymentResponse>.Success(
                Build(booking, settled, verifiedManually: settled.VerifiedManuallyAtUtc is not null, alreadySettled: true));
        }

        // No settlement yet. The override only makes sense while the hold is
        // live and the booking is still waiting for payment.
        if (booking.Status != BookingStatusEnum.PendingPayment)
            return Result<VerifyWalkInPaymentResponse>.Failure(
                $"This booking is {booking.Status.ToDescription().ToLowerInvariant()} and cannot be verified.", 409);

        if (booking.HoldExpiresAtUtc <= now)
        {
            booking.Expire();
            await context.SaveChangesAsync(cancellationToken);

            return Result<VerifyWalkInPaymentResponse>.Failure(
                "This booking hold has expired. Open a new walk-in.", 409);
        }

        var target = payments.FirstOrDefault();
        if (target is null)
            return Result<VerifyWalkInPaymentResponse>.Failure(
                "No payment attempt exists for this booking yet. Start the payment first.", 409);

        if (!BookingTransitions.IsAllowed(booking.Status, BookingStatusEnum.Confirmed))
            return Result<VerifyWalkInPaymentResponse>.Failure(
                $"A {booking.Status.ToDescription().ToLowerInvariant()} booking cannot be confirmed.", 409);

        target.MarkManuallyVerified(staffId.Value);
        booking.Confirm();
        Admit(booking, now, staffId.Value);
        await context.SaveChangesAsync(cancellationToken);

        return Result<VerifyWalkInPaymentResponse>.Success(
            Build(booking, target, verifiedManually: true, alreadySettled: false));
    }

    /// <summary>
    /// The rider is at the barrier, so the acknowledgement of payment is the
    /// entry: the vehicle is admitted and the paid clock starts here. Safe to
    /// call on every retry — an already-parked or closed booking is left alone,
    /// which is what keeps a duplicate tap from re-anchoring a live stay.
    /// </summary>
    private static void Admit(Domain.Booking booking, DateTimeOffset now, Guid staffId)
    {
        if (booking.Status != BookingStatusEnum.Confirmed)
            return;

        booking.ActivateWalkIn(now, staffId);
    }

    private static VerifyWalkInPaymentResponse Build(
        Domain.Booking booking,
        Domain.Payment payment,
        bool verifiedManually,
        bool alreadySettled)
        => new(
            booking.Id,
            payment.Id,
            payment.Status,
            payment.Status.ToDescription(),
            verifiedManually,
            booking.Status,
            booking.Status.ToDescription(),
            alreadySettled,
            booking.StartsAtUtc,
            booking.EndsAtUtc,
            booking.EnteredAtUtc);
}

using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Payments.Commands.VerifyWalkInOverstay;

/// <summary>
/// Staff override for a walk-in overstay the rider has paid at the barrier but
/// whose gateway callback has not landed. Marks the latest attempt settled on a
/// staff member's word and records who confirmed it, so the money stays
/// attributable; a later callback reconciles as a no-op.
/// </summary>
public sealed record VerifyWalkInOverstayPaymentCommand(
    Guid BookingId,
    Guid FacilityId)
    : IRequestResult<VerifyWalkInOverstayPaymentCommand, VerifyWalkInOverstayPaymentResponse>;

public sealed record VerifyWalkInOverstayPaymentResponse(
    Guid BookingId,
    Guid OverstayPaymentId,
    PaymentStatusEnum PaymentStatus,
    string PaymentStatusDescription,
    bool VerifiedManually,
    bool AlreadySettled);

public sealed class VerifyWalkInOverstayPaymentCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestResultHandler<VerifyWalkInOverstayPaymentCommand, VerifyWalkInOverstayPaymentResponse>
{
    public async Task<Result<VerifyWalkInOverstayPaymentResponse>> Handle(
        VerifyWalkInOverstayPaymentCommand request,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.StaffId;
        if (staffId is null)
            return Result<VerifyWalkInOverstayPaymentResponse>.Failure("Authentication required.", 401);

        if (!await GateAccess.CanScanAsync(context, staffId.Value, request.FacilityId, cancellationToken))
            return Result<VerifyWalkInOverstayPaymentResponse>.Failure(
                "You are not authorised to verify payments at this facility.", 403);

        var booking = await context.Bookings
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking is null)
            return Result<VerifyWalkInOverstayPaymentResponse>.Failure("Booking not found.", 404);

        if (!booking.IsWalkInBooking || booking.FacilityId != request.FacilityId)
            return Result<VerifyWalkInOverstayPaymentResponse>.Failure(
                "This is not a walk-in booking at this facility.", 404);

        if (booking.OverstayAmountPaisa is null)
            return Result<VerifyWalkInOverstayPaymentResponse>.Failure(
                "This booking has no overstay charge.", 409);

        var payments = await context.OverstayPayments
            .Where(p => p.BookingId == booking.Id)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var settled = payments.FirstOrDefault(p => p.IsSettled);
        if (settled is not null)
            return Result<VerifyWalkInOverstayPaymentResponse>.Success(
                Build(settled, verifiedManually: settled.VerifiedManuallyAtUtc is not null, alreadySettled: true));

        var target = payments.FirstOrDefault();
        if (target is null)
            return Result<VerifyWalkInOverstayPaymentResponse>.Failure(
                "No overstay payment attempt exists for this booking yet. Start the payment first.", 409);

        target.MarkManuallyVerified(staffId.Value);
        await context.SaveChangesAsync(cancellationToken);

        return Result<VerifyWalkInOverstayPaymentResponse>.Success(
            Build(target, verifiedManually: true, alreadySettled: false));
    }

    private static VerifyWalkInOverstayPaymentResponse Build(
        Domain.OverstayPayment payment,
        bool verifiedManually,
        bool alreadySettled)
        => new(
            payment.BookingId,
            payment.Id,
            payment.Status,
            payment.Status.ToDescription(),
            verifiedManually,
            alreadySettled);
}

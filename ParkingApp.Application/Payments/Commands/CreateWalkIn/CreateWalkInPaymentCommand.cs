using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Payments.Commands.Create;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Payments.Commands.CreateWalkIn;

/// <summary>
/// Starts the prepaid charge for a walk-in on a staff device. This is the
/// dynamic-QR basis: the returned <c>Payload</c> (a gateway pay URL) is what the
/// rider scans with any phone. The amount is the booking's snapshot, never
/// anything the staff device supplies, and settlement still happens only
/// through a gateway callback or an explicit staff verification.
/// </summary>
public sealed record CreateWalkInPaymentCommand(
    Guid BookingId,
    Guid FacilityId,
    PaymentGatewayEnum Gateway)
    : IRequestResult<CreateWalkInPaymentCommand, CreatePaymentResponse>;

public sealed class CreateWalkInPaymentCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IEnumerable<IPaymentGateway> gateways)
    : IRequestResultHandler<CreateWalkInPaymentCommand, CreatePaymentResponse>
{
    public async Task<Result<CreatePaymentResponse>> Handle(
        CreateWalkInPaymentCommand request,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.StaffId;
        if (staffId is null)
            return Result<CreatePaymentResponse>.Failure("Authentication required.", 401);

        if (!await GateAccess.CanScanAsync(context, staffId.Value, request.FacilityId, cancellationToken))
            return Result<CreatePaymentResponse>.Failure(
                "You are not authorised to take payments at this facility.", 403);

        var booking = await context.Bookings
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking is null)
            return Result<CreatePaymentResponse>.Failure("Booking not found.", 404);

        if (!booking.IsWalkInBooking || booking.FacilityId != request.FacilityId)
            return Result<CreatePaymentResponse>.Failure("This is not a walk-in booking at this facility.", 404);

        if (booking.Status != BookingStatusEnum.PendingPayment)
            return Result<CreatePaymentResponse>.Failure(
                $"This booking is {booking.Status.ToDescription().ToLowerInvariant()} and cannot be paid for.", 409);

        if (booking.HoldExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            booking.Expire();
            await context.SaveChangesAsync(cancellationToken);

            return Result<CreatePaymentResponse>.Failure(
                "This booking hold has expired. Open a new walk-in.", 409);
        }

        // Reuse a live attempt rather than stacking parallel charges for one hold.
        var existing = await context.Payments
            .AsNoTracking()
            .Where(p => p.BookingId == booking.Id
                        && p.Gateway == request.Gateway
                        && p.Status == PaymentStatusEnum.Initiated)
            .OrderByDescending(p => p.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null && existing.GatewayPaymentId is not null)
            return Result<CreatePaymentResponse>.Failure(
                "A payment for this walk-in is already in progress.", 409);

        // No rider account: the charge is tied to the booking, and the walk-in
        // snapshots travel to the gateway as the customer details.
        var payment = Payment.Create(
            booking.Id,
            null,
            request.Gateway,
            BookingPricing.ToPaisa(booking.TotalAmountNpr));

        context.Payments.Add(payment);
        await context.SaveChangesAsync(cancellationToken);

        IPaymentGateway gateway;
        try
        {
            gateway = PaymentGateways.Find(gateways, request.Gateway);
        }
        catch (PaymentGatewayException ex)
        {
            payment.MarkFailed(ex.Message);
            await context.SaveChangesAsync(cancellationToken);

            return Result<CreatePaymentResponse>.Failure(ex.Message, 400);
        }

        PaymentHandoffRequest handoff;
        try
        {
            handoff = await gateway.InitiateAsync(
                payment.Id.ToString(),
                payment.AmountPaisa,
                $"Walk-in parking {payment.BookingId:N}",
                PaymentFlowEnum.Prepaid,
                booking.WalkInRiderName,
                null,
                booking.WalkInPhoneNumber,
                cancellationToken);
        }
        catch (PaymentGatewayException ex)
        {
            payment.MarkFailed(ex.Message);
            await context.SaveChangesAsync(cancellationToken);

            return Result<CreatePaymentResponse>.Failure(ex.Message, 502);
        }

        // Persist the gateway handle BEFORE the staff device can render the QR —
        // it is the only key that can reconcile a payment whose callback is lost.
        payment.AttachGatewayPaymentId(handoff.GatewayPaymentId);
        await context.SaveChangesAsync(cancellationToken);

        return Result<CreatePaymentResponse>.Success(
            new CreatePaymentResponse(
                payment.Id,
                payment.BookingId,
                handoff.Gateway,
                payment.Status,
                payment.Status.ToDescription(),
                handoff.Mode,
                handoff.Mode.ToDescription(),
                handoff.Payload,
                handoff.FormFields,
                payment.AmountPaisa,
                payment.Currency,
                booking.HoldExpiresAtUtc));
    }
}

using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Payments.Commands.CreateOverstay;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Payments.Commands.CreateWalkInOverstay;

/// <summary>
/// Starts the overstay charge for a walk-in on a staff device. The walk-in
/// exited already, so this is the same post-exit settlement an app rider runs
/// from their phone — restarted here because a walk-in has no phone app and no
/// account to own the charge. The amount is the figure frozen at the exit scan,
/// never anything the staff device supplies.
/// </summary>
public sealed record CreateWalkInOverstayPaymentCommand(
    Guid BookingId,
    Guid FacilityId,
    PaymentGatewayEnum Gateway)
    : IRequestResult<CreateWalkInOverstayPaymentCommand, CreateOverstayPaymentResponse>;

public sealed class CreateWalkInOverstayPaymentCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IEnumerable<IPaymentGateway> gateways)
    : IRequestResultHandler<CreateWalkInOverstayPaymentCommand, CreateOverstayPaymentResponse>
{
    public async Task<Result<CreateOverstayPaymentResponse>> Handle(
        CreateWalkInOverstayPaymentCommand request,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.StaffId;
        if (staffId is null)
            return Result<CreateOverstayPaymentResponse>.Failure("Authentication required.", 401);

        if (!await GateAccess.CanScanAsync(context, staffId.Value, request.FacilityId, cancellationToken))
            return Result<CreateOverstayPaymentResponse>.Failure(
                "You are not authorised to take payments at this facility.", 403);

        var booking = await context.Bookings
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking is null)
            return Result<CreateOverstayPaymentResponse>.Failure("Booking not found.", 404);

        if (!booking.IsWalkInBooking || booking.FacilityId != request.FacilityId)
            return Result<CreateOverstayPaymentResponse>.Failure(
                "This is not a walk-in booking at this facility.", 404);

        if (booking.Status != BookingStatusEnum.Completed)
            return Result<CreateOverstayPaymentResponse>.Failure(
                "The vehicle has not exited yet, so there is nothing to settle.", 409);

        if (booking.OverstayAmountPaisa is not { } amountPaisa)
            return Result<CreateOverstayPaymentResponse>.Failure(
                "This booking has no overstay charge.", 409);

        var attempts = await OverstayPaymentStatus.LoadAsync(
            context, gateways, booking.Id, cancellationToken);

        if (attempts.Any(p => p.Status is PaymentStatusEnum.Completed or PaymentStatusEnum.Refunded))
            return Result<CreateOverstayPaymentResponse>.Failure(
                "The overstay for this booking has already been settled.", 409);

        if (attempts.Any(p => p.Gateway == request.Gateway
                              && p.Status == PaymentStatusEnum.Initiated
                              && p.GatewayPaymentId is not null))
            return Result<CreateOverstayPaymentResponse>.Failure(
                "An overstay payment for this booking is already in progress.", 409);

        // No rider account: the charge rides on the booking, and the walk-in
        // snapshots travel to the gateway as the customer details.
        var payment = OverstayPayment.Create(
            booking.Id,
            null,
            request.Gateway,
            amountPaisa);

        context.OverstayPayments.Add(payment);
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

            return Result<CreateOverstayPaymentResponse>.Failure(ex.Message, 400);
        }

        PaymentHandoffRequest handoff;
        try
        {
            handoff = await gateway.InitiateAsync(
                payment.Id.ToString(),
                payment.AmountPaisa,
                $"Walk-in overstay for booking {payment.BookingId:N}",
                PaymentFlowEnum.Overstay,
                booking.WalkInRiderName,
                null,
                booking.WalkInPhoneNumber,
                cancellationToken);
        }
        catch (PaymentGatewayException ex)
        {
            payment.MarkFailed(ex.Message);
            await context.SaveChangesAsync(cancellationToken);

            return Result<CreateOverstayPaymentResponse>.Failure(ex.Message, 502);
        }

        payment.AttachGatewayPaymentId(handoff.GatewayPaymentId);
        await context.SaveChangesAsync(cancellationToken);

        return Result<CreateOverstayPaymentResponse>.Success(
            new CreateOverstayPaymentResponse(
                payment.Id,
                payment.BookingId,
                handoff.Gateway,
                payment.Status,
                payment.Status.ToDescription(),
                handoff.Mode,
                handoff.Mode.ToDescription(),
                handoff.Payload,
                handoff.FormFields,
                BookingPricing.ToNpr(payment.AmountPaisa),
                payment.Currency,
                booking.OverstayBillableHours ?? 1));
    }
}

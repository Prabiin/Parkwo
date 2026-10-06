using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Payments.Commands.CreateOverstay;

/// <summary>
/// Starts payment for a booking's overstay charge.
///
/// This is separate from CreatePaymentCommand because the two deal with
/// different booking states: the prepaid one only touches a booking awaiting
/// its first payment, this one only a booking that has already exited. The
/// amount comes from the charge frozen at the exit scan, never from the client.
/// </summary>
public sealed record CreateOverstayPaymentCommand(
    Guid BookingId,
    PaymentGatewayEnum Gateway)
    : IRequestResult<CreateOverstayPaymentCommand, CreateOverstayPaymentResponse>;

public sealed class CreateOverstayPaymentCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IEnumerable<IPaymentGateway> gateways)
    : IRequestResultHandler<CreateOverstayPaymentCommand, CreateOverstayPaymentResponse>
{
    public async Task<Result<CreateOverstayPaymentResponse>> Handle(
        CreateOverstayPaymentCommand request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<CreateOverstayPaymentResponse>.Failure("Authentication required.", 401);

        var booking = await context.Bookings
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking is null)
            return Result<CreateOverstayPaymentResponse>.Failure("Booking not found.", 404);

        if (booking.UserId != userId.Value)
            return Result<CreateOverstayPaymentResponse>.Failure("You can only pay for your own booking.", 403);

        if (booking.Status != BookingStatusEnum.Completed)
            return Result<CreateOverstayPaymentResponse>.Failure(
                "The vehicle has not exited yet, so there is nothing to settle.", 409);

        if (booking.OverstayAmountPaisa is not { } amountPaisa)
            return Result<CreateOverstayPaymentResponse>.Failure(
                "This booking has no overstay charge.", 409);

        // Check with the gateway first so an old abandoned attempt does not
        // keep saying "already in progress".
        var attempts = await OverstayPaymentStatus.LoadAsync(
            context, gateways, booking.Id, cancellationToken);

        if (attempts.Any(p => p.Status is PaymentStatusEnum.Completed or PaymentStatusEnum.Refunded))
            return Result<CreateOverstayPaymentResponse>.Failure(
                "The overstay for this booking has already been settled.", 409);

        // Reuse a live attempt instead of stacking two charges. Abandoned ones
        // (failed, expired, cancelled) fall through so the rider can try again.
        if (attempts.Any(p => p.Gateway == request.Gateway
                              && p.Status == PaymentStatusEnum.Initiated
                              && p.GatewayPaymentId is not null))
            return Result<CreateOverstayPaymentResponse>.Failure(
                "An overstay payment for this booking is already in progress.", 409);

        var payment = OverstayPayment.Create(
            booking.Id,
            userId.Value,
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

        var rider = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId.Value)
            .Select(u => new { u.FullName, u.Email, u.PhoneNumber })
            .FirstOrDefaultAsync(cancellationToken);

        PaymentHandoffRequest handoff;
        try
        {
            handoff = await gateway.InitiateAsync(
                payment.Id.ToString(),
                payment.AmountPaisa,
                $"Parking overstay for booking {payment.BookingId:N}",
                PaymentFlowEnum.Overstay,
                rider?.FullName,
                rider?.Email,
                rider?.PhoneNumber,
                cancellationToken);
        }
        catch (PaymentGatewayException ex)
        {
            payment.MarkFailed(ex.Message);
            await context.SaveChangesAsync(cancellationToken);

            return Result<CreateOverstayPaymentResponse>.Failure(ex.Message, 502);
        }

        // Save the gateway's payment id BEFORE the client can open the URL, so
        // a rider who closes the page immediately still has a payment we can look up.
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
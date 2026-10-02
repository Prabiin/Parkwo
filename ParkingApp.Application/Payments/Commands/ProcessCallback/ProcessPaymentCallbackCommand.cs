using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Bookings.Queries.GetBookingById;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Payments.Commands.ProcessCallback;

/// <summary>
/// Khalti return-url handler. The payer is redirected here with query params
/// after checkout, and Flutter also calls it to force a reconciliation pass.
///
/// The query params are treated as a HINT, never as proof: the handler looks the
/// payment up by its stored pidx and asks Khalti server-to-server. Amount is
/// compared against what we charged, because a matching status on a mismatched
/// amount is a forged or misrouted callback.
/// </summary>
public sealed record ProcessPaymentCallbackCommand(
    PaymentGatewayEnum Gateway,
    string? Pidx,
    string? StatusHint,
    long? AmountPaisaHint)
    : IRequestResult<ProcessPaymentCallbackCommand, ProcessPaymentCallbackResponse>;

public sealed class ProcessPaymentCallbackCommandHandler(
    IApplicationDbContext context,
    IEnumerable<IPaymentGateway> gateways)
    : IRequestResultHandler<ProcessPaymentCallbackCommand, ProcessPaymentCallbackResponse>
{
    public async Task<Result<ProcessPaymentCallbackResponse>> Handle(
        ProcessPaymentCallbackCommand request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Pidx))
            return Result<ProcessPaymentCallbackResponse>.Failure("Missing payment reference.", 400);

        var pidx = request.Pidx.Trim();

        var payment = await context.Payments
            .Include(p => p.Booking)
            .FirstOrDefaultAsync(p => p.GatewayPaymentId == pidx, cancellationToken);

        if (payment is null)
            return Result<ProcessPaymentCallbackResponse>.Failure("No payment matches that reference.", 404);

        if (payment.Gateway != request.Gateway)
            return Result<ProcessPaymentCallbackResponse>.Failure("Payment reference does not belong to that gateway.", 400);

        // Already settled: answer idempotently so a replayed redirect does not
        // fail the rider on a payment that is genuinely done.
        if (payment.IsSettled)
            return Result<ProcessPaymentCallbackResponse>.Success(
                Build(payment, payment.Booking!, alreadySettled: true));

        var booking = payment.Booking ?? throw new InvalidOperationException($"Payment {payment.Id} has no booking row.");

        IPaymentGateway gateway;
        try
        {
            gateway = PaymentGateways.Resolve(gateways, request.Gateway);
        }
        catch (PaymentGatewayException ex)
        {
            return Result<ProcessPaymentCallbackResponse>.Failure(ex.Message, 400);
        }

        GatewayPaymentStatus status;
        try
        {
            status = await gateway.LookupAsync(pidx, cancellationToken);
        }
        catch (PaymentGatewayException ex)
        {
            // Leave the payment Initiated: a gateway outage must not be
            // recorded as a rider-side failure. The next poll retries.
            return Result<ProcessPaymentCallbackResponse>.Failure(ex.Message, 502);
        }

        if (status.Status == PaymentStatusEnum.Completed
            && status.AmountPaisa is not null
            && status.AmountPaisa.Value != payment.AmountPaisa)
            return Result<ProcessPaymentCallbackResponse>.Failure("Settled amount does not match the amount charged.", 409);

        var now = DateTimeOffset.UtcNow;

        switch (status.Status)
        {
            case PaymentStatusEnum.Completed:
                PaymentCompletion.TryConfirm(booking, payment, status.TransactionId);
                break;

            case PaymentStatusEnum.Cancelled:
                payment.MarkCancelled();
                booking.Cancel("Payment cancelled at the gateway.");
                break;

            case PaymentStatusEnum.Expired:
                payment.MarkExpired();
                booking.Expire();
                break;

            case PaymentStatusEnum.Failed:
                payment.MarkFailed("The gateway reported the payment as failed.");
                break;

            // Initiated: still in flight at the gateway. Keep the hold alive and
            // let the client poll again rather than guessing an outcome.
            case PaymentStatusEnum.Initiated:
                if (booking.HoldExpiresAtUtc <= now)
                {
                    payment.MarkExpired();
                    booking.Expire();
                }
                break;
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result<ProcessPaymentCallbackResponse>.Success(
            Build(payment, booking, alreadySettled: false));
    }

    private static ProcessPaymentCallbackResponse Build(
        Payment payment,
        Booking booking,
        bool alreadySettled)
        => new(
            payment.Id,
            payment.BookingId,
            payment.Gateway,
            payment.Status,
            payment.Status.ToDescription(),
            payment.GatewayTransactionId,
            booking.Status,
            booking.Status.ToDescription(),
            alreadySettled);
}
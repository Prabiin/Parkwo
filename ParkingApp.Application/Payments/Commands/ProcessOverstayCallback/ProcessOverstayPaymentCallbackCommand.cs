using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Payments.Commands.ProcessOverstayCallback;

/// <summary>
/// Overstay return-url handler. Separate from ProcessPaymentCallbackCommand
/// because the prepaid one settles by advancing the booking's state, and this
/// booking is already finished — here only the payment row changes.
///
/// Query parameters are a hint, never proof: the payment is looked up by its
/// stored gateway id and the gateway is asked server-to-server.
/// </summary>
public sealed record ProcessOverstayPaymentCallbackCommand(
    PaymentGatewayEnum Gateway,
    string? Reference,
    string? StatusHint,
    long? AmountPaisaHint)
    : IRequestResult<ProcessOverstayPaymentCallbackCommand, ProcessOverstayPaymentCallbackResponse>;

public sealed class ProcessOverstayPaymentCallbackCommandHandler(
    IApplicationDbContext context,
    IEnumerable<IPaymentGateway> gateways)
    : IRequestResultHandler<ProcessOverstayPaymentCallbackCommand, ProcessOverstayPaymentCallbackResponse>
{
    public async Task<Result<ProcessOverstayPaymentCallbackResponse>> Handle(
        ProcessOverstayPaymentCallbackCommand request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reference))
            return Result<ProcessOverstayPaymentCallbackResponse>.Failure("Missing payment reference.", 400);

        var reference = request.Reference.Trim();

        var payment = await context.OverstayPayments
            .Include(p => p.Booking)
            .FirstOrDefaultAsync(p => p.GatewayPaymentId == reference, cancellationToken);

        if (payment is null)
            return Result<ProcessOverstayPaymentCallbackResponse>.Failure("No payment matches that reference.", 404);

        if (payment.Gateway != request.Gateway)
            return Result<ProcessOverstayPaymentCallbackResponse>.Failure(
                "Payment reference does not belong to that gateway.", 400);

        // Already settled: answer the same thing again so a replayed redirect
        // does not fail the rider on a payment that really is done.
        if (payment.IsSettled)
            return Result<ProcessOverstayPaymentCallbackResponse>.Success(
                Build(payment, alreadySettled: true));

        IPaymentGateway gateway;
        try
        {
            gateway = PaymentGateways.Find(gateways, request.Gateway);
        }
        catch (PaymentGatewayException ex)
        {
            return Result<ProcessOverstayPaymentCallbackResponse>.Failure(ex.Message, 400);
        }

        GatewayPaymentStatus status;
        try
        {
            status = await gateway.LookupAsync(reference, cancellationToken);
        }
        catch (PaymentGatewayException ex)
        {
            // Ask again later; a gateway outage is not a rider-side failure.
            return Result<ProcessOverstayPaymentCallbackResponse>.Failure(ex.Message, 502);
        }

        if (status.Status == PaymentStatusEnum.Completed
            && status.AmountPaisa is not null
            && status.AmountPaisa.Value != payment.AmountPaisa)
            return Result<ProcessOverstayPaymentCallbackResponse>.Failure(
                "Settled amount does not match the amount charged.", 409);

        OverstayPaymentStatus.UpdateRow(payment, status);

        await context.SaveChangesAsync(cancellationToken);

        return Result<ProcessOverstayPaymentCallbackResponse>.Success(
            Build(payment, alreadySettled: false));
    }

    private static ProcessOverstayPaymentCallbackResponse Build(
        OverstayPayment payment,
        bool alreadySettled)
        => new(
            payment.Id,
            payment.BookingId,
            payment.Gateway,
            payment.Status,
            payment.Status.ToDescription(),
            payment.GatewayTransactionId,
            alreadySettled);
}
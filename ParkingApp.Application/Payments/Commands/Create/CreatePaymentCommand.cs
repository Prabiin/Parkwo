using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Bookings.Queries.GetBookingById;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Payments.Commands.Create;

/// <summary>
/// Starts a payment for an existing booking: charges the snapshot price (never
/// a client-supplied amount) at the gateway and returns where to send the user.
///
/// The response is always "Initiated", never "Completed". Settlement happens in
/// ProcessPaymentCallback after a server-side lookup, so a rider who closes the
/// webview early is not double-charged and a forged redirect confirms nothing.
/// </summary>
public sealed record CreatePaymentCommand(
    Guid BookingId,
    PaymentGatewayEnum Gateway)
    : IRequestResult<CreatePaymentCommand, CreatePaymentResponse>;

public sealed class CreatePaymentCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IEnumerable<IPaymentGateway> gateways)
    : IRequestResultHandler<CreatePaymentCommand, CreatePaymentResponse>
{
    public async Task<Result<CreatePaymentResponse>> Handle(
        CreatePaymentCommand request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<CreatePaymentResponse>.Failure("Authentication required.", 401);

        var booking = await context.Bookings
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking is null)
            return Result<CreatePaymentResponse>.Failure("Booking not found.", 404);

        if (booking.UserId != userId.Value)
            return Result<CreatePaymentResponse>.Failure("You can only pay for your own booking.", 403);

        if (booking.Status != BookingStatusEnum.PendingPayment)
            return Result<CreatePaymentResponse>.Failure(
                $"This booking is {booking.Status.ToDescription().ToLowerInvariant()} and cannot be paid for.", 409);

        if (booking.HoldExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            booking.Expire();
            await context.SaveChangesAsync(cancellationToken);

            return Result<CreatePaymentResponse>.Failure("This booking hold has expired. Create a new booking.", 409);
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
            return Result<CreatePaymentResponse>.Failure("A payment for this booking is already in progress.", 409);

        var payment = Payment.Create(
            booking.Id,
            userId.Value,
            request.Gateway,
            BookingPricing.ToPaisa(booking.TotalAmountNpr));

        context.Payments.Add(payment);
        await context.SaveChangesAsync(cancellationToken);

        IPaymentGateway gateway;
        try
        {
            gateway = PaymentGateways.Resolve(gateways, request.Gateway);
        }
        catch (PaymentGatewayException ex)
        {
            payment.MarkFailed(ex.Message);
            await context.SaveChangesAsync(cancellationToken);

            return Result<CreatePaymentResponse>.Failure(ex.Message, 400);
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
                $"Parking booking {payment.BookingId:N}",
                rider?.FullName,
                rider?.Email,
                rider?.PhoneNumber,
                cancellationToken);
        }
        catch (PaymentGatewayException ex)
        {
            payment.MarkFailed(ex.Message);
            await context.SaveChangesAsync(cancellationToken);

            return Result<CreatePaymentResponse>.Failure(ex.Message, 502);
        }

        // Persist the gateway handle BEFORE the client can open the URL — this is
        // the only key that can reconcile a payment whose callback never arrives.
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
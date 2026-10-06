using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Payments.Commands.ProcessOverstayCallback;

/// <summary>
/// No booking status here on purpose: settling an overstay never moves the
/// booking, so reporting it would only suggest otherwise.
/// </summary>
public record ProcessOverstayPaymentCallbackResponse(
    Guid OverstayPaymentId,
    Guid BookingId,
    PaymentGatewayEnum Gateway,
    PaymentStatusEnum PaymentStatus,
    string PaymentStatusDescription,
    string? GatewayTransactionId,
    bool AlreadySettled);

using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Payments.Commands.ProcessCallback;

public record ProcessPaymentCallbackResponse(
    Guid PaymentId,
    Guid BookingId,
    PaymentGatewayEnum Gateway,
    PaymentStatusEnum PaymentStatus,
    string PaymentStatusDescription,
    string? GatewayTransactionId,
    BookingStatusEnum BookingStatus,
    string BookingStatusDescription,
    /// <summary>True when the payment was already settled before this call.</summary>
    bool AlreadySettled);
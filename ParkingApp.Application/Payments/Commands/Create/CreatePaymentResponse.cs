using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Payments.Commands.Create;

public record CreatePaymentResponse(
    Guid PaymentId,
    Guid BookingId,
    PaymentGatewayEnum Gateway,
    PaymentStatusEnum Status,
    string StatusDescription,
    PaymentHandoffModeEnum Mode,
    string ModeDescription,
    /// <summary>
    /// The URL (Redirect) or deeplink (DeepLink) the client opens. Never proves
    /// payment — only the callback lookup does.
    /// </summary>
    string Payload,
    IReadOnlyDictionary<string, string>? FormFields,
    long AmountPaisa,
    string Currency,
    DateTimeOffset HoldExpiresAtUtc);
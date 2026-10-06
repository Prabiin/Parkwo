using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Payments.Queries.GetPaymentGateways;

/// <summary>
/// One selectable method on the checkout screen. Name and description are
/// ready to display.
/// </summary>
public record PaymentGatewayOption(
    PaymentGatewayEnum Gateway,
    string Name,
    string Description);

/// <summary>An empty Gateways list means no payment credentials are configured.</summary>
public record GetPaymentGatewaysResponse(
    IReadOnlyList<PaymentGatewayOption> Gateways);

using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Common.Helpers;

/// <summary>
/// Resolves the gateway implementation for a requested gateway. Keeps handlers
/// gateway-agnostic: they ask for "Khalti" and get an IPaymentGateway without
/// knowing which concrete adapter is registered.
/// </summary>
public static class PaymentGateways
{
    /// <summary>Returns the implementation for a gateway, or throws if it is not configured.</summary>
    public static IPaymentGateway Find(
        IEnumerable<IPaymentGateway> gateways,
        PaymentGatewayEnum gateway)
    {
        var match = gateways.FirstOrDefault(g => g.Gateway == gateway);

        if (match is null)
            throw new PaymentGatewayException($"No payment gateway is configured for {gateway.ToDescription()}.");

        return match;
    }
}
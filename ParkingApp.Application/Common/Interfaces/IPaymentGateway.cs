using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Common.Interfaces;

/// <summary>
/// How the client is expected to hand the user over to the gateway. Lets one
/// payment endpoint serve gateways with very different hand-off mechanics
/// (Khalti's GET redirect, eSewa's HTML form POST) without changing its shape.
/// </summary>
public enum PaymentHandoffModeEnum
{
    /// <summary>Open <see cref="PaymentHandoffRequest.Payload"/> as a URL in a webview.</summary>
    Redirect = 1,

    /// <summary>Open <see cref="PaymentHandoffRequest.Payload"/> as an OS deeplink (eSewa app).</summary>
    DeepLink = 2,

    /// <summary>POST the fields in <see cref="PaymentHandoffRequest.FormFields"/> to <see cref="PaymentHandoffRequest.Payload"/>.</summary>
    FormPost = 3
}

public sealed record PaymentHandoffRequest(
    PaymentGatewayEnum Gateway,
    PaymentHandoffModeEnum Mode,
    string Payload,
    /// <summary>
    /// The gateway's own handle for this charge (Khalti `pidx`). The caller
    /// MUST persist this before returning to the client — it is the only key
    /// that can reconcile a payment whose callback never arrived.
    /// </summary>
    string GatewayPaymentId,
    IReadOnlyDictionary<string, string>? FormFields = null);

/// <summary>Server-to-server confirmation of what a gateway thinks happened.</summary>
public sealed record GatewayPaymentStatus(
    PaymentStatusEnum Status,
    string? TransactionId = null,
    long? AmountPaisa = null);

/// <summary>
/// Transport to a payment gateway. Initiation and verification are always
/// server-to-server with the secret key — a client redirect alone is never
/// treated as proof of payment.
/// </summary>
public interface IPaymentGateway
{
    PaymentGatewayEnum Gateway { get; }

    /// <summary>
    /// Creates the charge at the gateway and returns where to send the user.
    /// Throws <see cref="PaymentGatewayException"/> on a gateway-side failure.
    /// </summary>
    Task<PaymentHandoffRequest> InitiateAsync(
        string reference,
        long amountPaisa,
        string description,
        string? customerName,
        string? customerEmail,
        string? customerPhone,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks the gateway for the authoritative state of a payment previously
    /// initiated with <paramref name="gatewayPaymentId"/>.
    /// </summary>
    Task<GatewayPaymentStatus> LookupAsync(
        string gatewayPaymentId,
        CancellationToken cancellationToken = default);
}

public sealed class PaymentGatewayException(string message, Exception? inner = null)
    : Exception(message, inner);
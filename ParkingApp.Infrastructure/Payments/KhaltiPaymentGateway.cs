using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Infrastructure.Payments;

/// <summary>
/// Khalti ePayment v2 (KPG-2) web checkout.
///
/// Initiate is a server-side POST with the secret key that returns a `pidx`
/// plus a hosted `payment_url` the client opens in a webview. Redirect params
/// on the way back are NOT proof of payment — per Khalti's docs the lookup API
/// is the authoritative check, so LookupAsync is what actually settles a booking.
/// </summary>
public sealed class KhaltiPaymentGateway(
    HttpClient httpClient,
    KhaltiSettings settings,
    ILogger<KhaltiPaymentGateway> logger) : IPaymentGateway
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public PaymentGatewayEnum Gateway => PaymentGatewayEnum.Khalti;

    public async Task<PaymentHandoffRequest> InitiateAsync(
        string reference,
        long amountPaisa,
        string description,
        string? customerName,
        string? customerEmail,
        string? customerPhone,
        CancellationToken cancellationToken = default)
    {
        // Khalti's minimum charge is NPR 10 (1000 paisa). Rejecting locally
        // avoids burning a gateway round-trip on a request that cannot succeed.
        if (amountPaisa < 1000)
            throw new PaymentGatewayException("Amount is below the Khalti minimum of NPR 10.");

        var payload = new Dictionary<string, object?>
        {
            ["return_url"] = settings.ReturnUrl,
            ["website_url"] = settings.WebsiteUrl,
            // Paisa, as an integer string — Khalti rejects decimals here.
            ["amount"] = amountPaisa.ToString(CultureInfo.InvariantCulture),
            ["purchase_order_id"] = reference,
            ["purchase_order_name"] = description
        };

        if (!string.IsNullOrWhiteSpace(customerName)
            || !string.IsNullOrWhiteSpace(customerPhone))
        {
            payload["customer_info"] = new Dictionary<string, object?>
            {
                ["name"] = customerName,
                ["phone"] = customerPhone,
                ["email"] = customerEmail
            };
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "epayment/initiate/")
        {
            Content = JsonContent.Create(payload, options: Json)
        };
        request.Headers.TryAddWithoutValidation("Authorization", $"Key {settings.SecretKey}");

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            logger.LogError("Khalti initiate failed for {Reference}: {Status} {Body}", reference, (int)response.StatusCode, body);

            throw new PaymentGatewayException($"Khalti rejected the payment request ({(int)response.StatusCode}).");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json, cancellationToken);

        if (!json.TryGetProperty("pidx", out var pidxElement) || string.IsNullOrWhiteSpace(pidxElement.GetString()))
            throw new PaymentGatewayException("Khalti did not return a payment reference.");

        if (!json.TryGetProperty("payment_url", out var urlElement) || string.IsNullOrWhiteSpace(urlElement.GetString()))
            throw new PaymentGatewayException("Khalti did not return a payment URL.");

        // The pidx travels back to the caller so it can be persisted before the
        // client opens the URL — without it we cannot reconcile a lost callback.
        return new PaymentHandoffRequest(
            PaymentGatewayEnum.Khalti,
            PaymentHandoffModeEnum.Redirect,
            urlElement.GetString()!,
            pidxElement.GetString()!);
    }

    public async Task<GatewayPaymentStatus> LookupAsync(
        string gatewayPaymentId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "epayment/lookup/")
        {
            Content = JsonContent.Create(new { pidx = gatewayPaymentId }, options: Json)
        };
        request.Headers.TryAddWithoutValidation("Authorization", $"Key {settings.SecretKey}");

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            logger.LogError("Khalti lookup failed for {Pidx}: {Status} {Body}", gatewayPaymentId, (int)response.StatusCode, body);

            throw new PaymentGatewayException($"Khalti lookup failed ({(int)response.StatusCode}).");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json, cancellationToken);

        var status = json.TryGetProperty("status", out var statusElement)
            ? statusElement.GetString()
            : null;

        var transactionId = json.TryGetProperty("transaction_id", out var txnElement)
            ? txnElement.GetString()
            : null;

        // Khalti reports the settled figure as total_amount, in paisa.
        long? amountPaisa = json.TryGetProperty("total_amount", out var amountElement)
                            && amountElement.TryGetInt64(out var parsedAmount)
            ? parsedAmount
            : null;

        logger.LogInformation(
            "Khalti lookup {Pidx}: status={Status} txn={TransactionId} amountPaisa={Amount}",
            gatewayPaymentId, status, transactionId, amountPaisa);

        return new GatewayPaymentStatus(MapStatus(status), transactionId, amountPaisa);
    }

    /// <summary>
    /// Only "Completed" is success. Khalti is explicit that Cancelled, Expired
    /// and Failed are terminal failures; anything unrecognised is held as
    /// Initiated rather than assumed paid or assumed dead.
    /// </summary>
    private static PaymentStatusEnum MapStatus(string? status) => status switch
    {
        "Completed" => PaymentStatusEnum.Completed,
        "User canceled" => PaymentStatusEnum.Cancelled,
        "Expired" => PaymentStatusEnum.Expired,
        "Failed" => PaymentStatusEnum.Failed,
        "Refunded" => PaymentStatusEnum.Refunded,
        "Partially refunded" => PaymentStatusEnum.Refunded,
        _ => PaymentStatusEnum.Initiated
    };
}
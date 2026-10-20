using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;

namespace ParkingApp.Infrastructure.Auth;

/// <summary>
/// Real delivery over Resend's HTTP API, registered when
/// <c>Email:Provider</c> is <c>Resend</c>.
/// <para>
/// This exists because a hosted platform may refuse outbound SMTP. Render
/// drops traffic to ports 25, 465 and 587 on every plan, and the failure is a
/// silent packet drop rather than a rejection: the send sits in the TCP
/// handshake until the timeout fires, so it looks like a hung relay or a bad
/// credential and no setting can fix it. HTTPS on 443 is not blocked, which is
/// the whole reason to prefer an API over a relay on that host.
/// </para>
/// <para>
/// Plain text only, one message per call, and only the recipient and subject
/// are logged — matching <see cref="SmtpEmailSender"/>, since the messages
/// carry a one-time password that has no business in the log stream. The
/// console sender is the one place the secret is written out, and only in the
/// configuration where nothing could have delivered it.
/// </para>
/// </summary>
public class ResendEmailSender : IEmailSender
{
    private const string EmailsEndpoint = "https://api.resend.com/emails";

    private readonly EmailSettings _settings;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(EmailSettings settings, ILogger<ResendEmailSender> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task SendAsync(
        string toEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds),
        };

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        var payload = new ResendEmail(
            // "Name <address>", which is the one shape Resend accepts for a
            // display name. Built here rather than in configuration so the
            // address stays a single value an operator can check against the
            // domain in the dashboard.
            From: string.IsNullOrWhiteSpace(_settings.FromName)
                ? _settings.From
                : $"{_settings.FromName} <{_settings.From}>",
            To: [toEmail],
            Subject: subject,
            Text: body,
            ReplyTo: string.IsNullOrWhiteSpace(_settings.ReplyTo) ? null : _settings.ReplyTo);

        using var response = await client.PostAsJsonAsync(
            EmailsEndpoint,
            payload,
            ResendJsonContext.Default.ResendEmail,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // The body carries Resend's own explanation ("domain is not
            // verified", "API key is invalid"), which is the difference between
            // a one-line fix and another round of guessing. Read even though it
            // is only logged: the caller gets "Failed" and the reason lands
            // beside the recipient in the log stream.
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new HttpRequestException(
                $"Resend rejected the message ({(int)response.StatusCode} {response.StatusCode}): {detail}");
        }

        _logger.LogInformation("Email delivered to {ToEmail} | {Subject}", toEmail, subject);
    }
}

/// <summary>
/// Resend's <c>send</c> payload. Only the fields Parkwo uses; the rest of the
/// API's surface is left until it is needed.
/// </summary>
internal sealed record ResendEmail(
    [property: JsonPropertyName("from")] string From,
    [property: JsonPropertyName("to")] List<string> To,
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("reply_to")] string? ReplyTo);

/// <summary>
/// Source-generated serialisation, so the payload shape is trimmed with the
/// application rather than resolved by reflection at runtime.
/// </summary>
[JsonSerializable(typeof(ResendEmail))]
internal sealed partial class ResendJsonContext : JsonSerializerContext;

namespace ParkingApp.Application.Configuration;

/// <summary>
/// Outbound email for the handful of plain-text messages Parkwo sends (today:
/// the generated password a staff member gets on create, resend and reset).
/// <para>
/// Two transports behind one setting, because the right one depends on where
/// the API is hosted rather than on preference. <c>Email:Provider</c> picks
/// between them:
/// </para>
/// <list type="bullet">
/// <item><c>Resend</c> — an HTTP API over 443. This is what a platform that
/// blocks outbound SMTP needs: Render drops traffic to 25/465/587 on every
/// plan, so a relay configured there never connects. The send fails at the TCP
/// handshake, before TLS or authentication, which reads as a mystery timeout
/// rather than a policy refusal.</item>
/// <item><c>Smtp</c> — a relay on 587. Still correct on a host that permits
/// outbound submission, and left in place for exactly that.</item>
/// </list>
/// <para>
/// The section stays optional: with no provider the console sender is
/// registered, which only logs the message. Local development then needs no
/// mail account, while a deployment that never configured email is visible in
/// its own logs rather than reporting a message it never sent.
/// </para>
/// </summary>
public class EmailSettings
{
    public const string SectionName = "Email";

    /// <summary>
    /// Which transport sends. Blank means "not configured" and falls back to
    /// the console sender. Recognised values are <c>Resend</c> and <c>Smtp</c>
    /// (case-insensitive).
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Resend API key (<c>re_…</c>). Required when <see cref="Provider"/> is
    /// <c>Resend</c>. Treat it as a secret: it authorises sending as the
    /// verified domain, so it belongs in the host's environment, never in the
    /// repository.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>SMTP relay, e.g. <c>smtp.gmail.com</c>.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>587 (STARTTLS) is the usual submission port; 465 is implicit TLS.</summary>
    public int Port { get; set; } = 587;

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Mailbox the message is sent from. With Resend this must be on a domain
    /// verified in the account — Resend refuses a sender it cannot prove the
    /// customer controls, and it cannot be a public domain such as gmail.com.
    /// With SMTP it is usually the address the credentials own.
    /// </summary>
    public string From { get; set; } = string.Empty;

    public string FromName { get; set; } = "Parkwo";

    /// <summary>
    /// Where replies go, when that should differ from <see cref="From"/>. A
    /// verified-domain sender is usually a <c>noreply@</c> address nobody
    /// reads, so pointing replies at a real inbox keeps a staff member's
    /// answer from disappearing.
    /// </summary>
    public string ReplyTo { get; set; } = string.Empty;

    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// Bound on the round-trip, carried through the cancellation token rather
    /// than the client's own timeout: a hung relay or API must fail the admin's
    /// request instead of pinning it open indefinitely. Missing this, an
    /// unreachable SMTP host blocks the request until the platform kills it.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 15;
}

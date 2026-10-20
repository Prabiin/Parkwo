namespace ParkingApp.Application.Configuration;

/// <summary>
/// Outbound SMTP for the handful of plain-text emails Parkwo sends (today: the
/// generated password a staff member gets on create and on reset).
/// <para>
/// The section is optional on purpose, and the optionality is the point: with
/// no <c>Email:Host</c> the console sender is registered instead, which only
/// logs the message. Local development then needs no mail server, while a
/// deployment that never configured SMTP is visible in its own logs rather than
/// reporting an email it never sent — the failure this setting exists to make
/// impossible to miss.
/// </para>
/// </summary>
public class EmailSettings
{
    public const string SectionName = "Email";

    /// <summary>SMTP relay, e.g. <c>smtp.gmail.com</c>. Blank means "not configured".</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>587 (STARTTLS) is the usual submission port; 465 is implicit TLS.</summary>
    public int Port { get; set; } = 587;

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Mailbox the message is sent from. Most relays only allow an address the
    /// configured credentials own, so this has to be one they can send as.
    /// </summary>
    public string From { get; set; } = string.Empty;

    public string FromName { get; set; } = "Parkwo";

    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// Bound on the SMTP round-trip. <see cref="System.Net.Mail.SmtpClient.Timeout"/>
    /// only applies to the synchronous send, so this is enforced through the
    /// cancellation token: a hung relay must fail the admin's request rather
    /// than pin it open indefinitely.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 15;
}

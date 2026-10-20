using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;

namespace ParkingApp.Infrastructure.Auth;

/// <summary>
/// Real delivery over SMTP, registered only when <see cref="EmailSettings"/>
/// is configured — the counterpart to <see cref="ConsoleEmailSender"/>, which
/// is what an unconfigured deployment falls back to.
/// <para>
/// Plain text only (the messages carry a one-time password and there is no
/// template engine behind <see cref="IEmailSender"/>), one message per call,
/// and only the recipient and subject are logged — the secret itself is
/// written to the log stream exactly once, by the console sender, and only in
/// the configuration where nothing could have delivered it anyway.
/// </para>
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(EmailSettings settings, ILogger<SmtpEmailSender> logger)
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
        using var message = new MailMessage
        {
            From = new MailAddress(_settings.From, _settings.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false,
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            DeliveryMethod = SmtpDeliveryMethod.Network,
            EnableSsl = _settings.EnableSsl,
            Credentials = string.IsNullOrWhiteSpace(_settings.UserName)
                ? null
                : new NetworkCredential(_settings.UserName, _settings.Password),
        };

        // SmtpClient.Timeout only bounds the synchronous Send, so the timeout
        // that actually applies here is carried by the token.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

        await client.SendMailAsync(message, timeout.Token);

        _logger.LogInformation("Email delivered to {ToEmail} | {Subject}", toEmail, subject);
    }
}

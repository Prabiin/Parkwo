using Microsoft.Extensions.Logging;
using ParkingApp.Application.Common.Interfaces;

namespace ParkingApp.Infrastructure.Auth;

/// <summary>
/// Development email sender. Logs the message instead of delivering it so a
/// locally-created staff member's generated password is recoverable from the
/// console. Replace with a real provider (SMTP, SES, etc.) in production.
/// </summary>
public class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(
        string toEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Email to {ToEmail} | {Subject}\n{Body}",
            toEmail,
            subject,
            body);

        return Task.CompletedTask;
    }
}

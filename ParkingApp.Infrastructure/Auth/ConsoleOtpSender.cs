using Microsoft.Extensions.Logging;
using ParkingApp.Application.Auth.Interfaces;

namespace ParkingApp.Infrastructure.Auth;

/// <summary>
/// Console-based OTP sender for development.
/// Replace with an actual SMS provider (Twilio, AWS SNS, etc.) in production.
/// </summary>
public class ConsoleOtpSender : IOtpSender
{
    private readonly ILogger<ConsoleOtpSender> _logger;

    public ConsoleOtpSender(ILogger<ConsoleOtpSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("OTP for {PhoneNumber}: {Code}", phoneNumber, code);
        return Task.CompletedTask;
    }
}

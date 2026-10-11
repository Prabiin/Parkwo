namespace ParkingApp.Application.Common.Interfaces;

/// <summary>
/// Delivers a plain-text email. Parkwo only ever sends a handful of messages
/// (today: a staff member's generated password), so this is deliberately a
/// minimal seam rather than a template engine. The development implementation
/// logs; a real provider replaces it in production.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(
        string toEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}

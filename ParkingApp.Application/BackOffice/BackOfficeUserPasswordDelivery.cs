using Microsoft.Extensions.Logging;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain;

namespace ParkingApp.Application.BackOffice;

/// <summary>
/// Delivery of the generated BackOffice user password, which is the one secret
/// Parkwo emails and cannot re-read afterwards: only the hash is stored, so a
/// password that fails to send is gone and the answer is to issue a new one.
/// <para>
/// Both flows that create a secret (BackOffice user create, resend) go through
/// here, so a delivery failure is handled the same way in each. It deliberately
/// does NOT rethrow and does NOT fail the request: the account is already
/// persisted by the time this runs, and the client is told the outcome instead
/// of being handed an error that suggests the write failed. The resend action
/// is the recovery path.
/// </para>
/// </summary>
public static class BackOfficeUserPasswordDelivery
{
    private const string Failed = "Failed";
    private const string Sent = "Sent";

    public static async Task<string> SendAsync(
        IEmailSender emailSender,
        ILogger logger,
        BackOfficeUser user,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await emailSender.SendAsync(user.Email, subject, body, cancellationToken);
            return Sent;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Could not send the password email to {Email} for BackOffice user {UserId}.",
                user.Email,
                user.Id);

            return Failed;
        }
    }

    /// <summary>
    /// True while the account still holds a Parkwo-generated password, which is
    /// also the window in which resending an invite is legal.
    /// </summary>
    public static bool IsPasswordOutstanding(BackOfficeUser user) => user.MustChangePassword;

    public static bool IsFailed(string deliveryStatus)
        => string.Equals(deliveryStatus, Failed, StringComparison.Ordinal);
}

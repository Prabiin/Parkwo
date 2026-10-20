using Microsoft.Extensions.Logging;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain;

namespace ParkingApp.Application.Staffs;

/// <summary>
/// Delivery of the generated staff password, which is the one secret Parkwo
/// emails and the one it cannot re-read afterwards: only the hash is stored, so
/// a password that fails to send is gone and the answer is to issue a new one.
/// <para>
/// Both flows that create a secret (BackOffice create, BackOffice resend) go
/// through here, so a delivery failure is handled the same way in each and
/// neither can accidentally start caring about it differently. It deliberately
/// does NOT rethrow and does NOT fail the request: the account is already
/// persisted by the time this runs, and the client is told the outcome in the
/// response instead of being handed an error that suggests the write failed.
/// That leaves the BackOffice "resend" button as the recovery path, which is
/// exactly the case it exists for.
/// </para>
/// </summary>
public static class StaffPasswordDelivery
{
    private const string Failed = "Failed";
    private const string Sent = "Sent";

    /// <summary>
    /// Attempts delivery and reports what happened, never throwing. A relay that
    /// is down, misconfigured or slow must not turn a successful account
    /// creation into an error the admin would read as "nothing was saved".
    /// </summary>
    public static async Task<string> SendAsync(
        IEmailSender emailSender,
        ILogger logger,
        Staff staff,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await emailSender.SendAsync(staff.Email, subject, body, cancellationToken);
            return Sent;
        }
        catch (Exception ex)
        {
            // Logged rather than surfaced as a 502: the row exists and the
            // password is unusable, so there is nothing for the caller to undo.
            // The message stays out of the response because the staff record is
            // already created and exposing SMTP internals to an admin who can
            // do nothing with them is noise, not information.
            logger.LogError(
                ex,
                "Could not send the password email to {Email} for staff {StaffId}.",
                staff.Email,
                staff.Id);

            return Failed;
        }
    }

    /// <summary>
    /// True when a generated password is still outstanding, i.e. the staff
    /// member has never set one of their own. This is the whole visibility rule
    /// for the resend button: it is shown while the credential is Parkwo's
    /// generated secret and hidden the moment the staff member replaces it.
    /// No separate "invite status" is tracked, because the flag that already
    /// forces the change also marks the invite as outstanding.
    /// </summary>
    public static bool IsPasswordOutstanding(Staff staff) => staff.MustChangePassword;

    public static bool IsFailed(string deliveryStatus)
        => string.Equals(deliveryStatus, Failed, StringComparison.Ordinal);
}

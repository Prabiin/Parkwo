using ParkingApp.Domain.Common.Base;
using ParkingApp.Domain.BackOffice.Role;

namespace ParkingApp.Domain;

public class BackOfficeUser : AuditableEntity
{
    public string UserName { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;

    /// <summary>
    /// True from creation (and after a BackOffice resend) until the user sets
    /// their own password. While true the API only allows login and
    /// change-password, so the emailed temporary secret never becomes a
    /// long-lived shared credential.
    /// </summary>
    public bool MustChangePassword { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginAtUtc { get; set; }

    /// <summary>
    /// Single role assigned to this user (1:1).
    /// </summary>
    public BackOfficeUserRole? UserRole { get; set; }

    /// <summary>
    /// Replaces the credential. Used by both a BackOffice resend (which also
    /// re-forces a change and re-emails the secret) and the user's own
    /// change-password, so the two paths cannot diverge.
    /// </summary>
    public void SetPassword(string passwordHash, bool mustChangePassword)
    {
        PasswordHash = passwordHash;
        MustChangePassword = mustChangePassword;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
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

    /// <summary>
    /// True once the account has been deleted. A BackOffice admin is soft
    /// deleted rather than removed, because the rows that reference it — the
    /// audit trail, the permissions they granted — are the reason the account
    /// existed. A deleted admin cannot log in and no longer appears in the
    /// user list, but the history stays.
    /// </summary>
    public bool IsDeleted { get; set; }

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

    /// <summary>
    /// Deletes the account without removing the row. Deactivating as well as
    /// flagging means a deleted admin cannot log in even if a token from
    /// before the deletion is still live.
    /// </summary>
    public void SoftDelete()
    {
        IsDeleted = true;
        IsActive = false;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
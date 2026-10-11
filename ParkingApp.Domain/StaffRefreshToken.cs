using ParkingApp.Domain.Common.Base;

namespace ParkingApp.Domain;

/// <summary>
/// A staff member's refresh token. Deliberately a separate table from
/// <see cref="RefreshToken"/> rather than a nullable owner column on it: the
/// rider token hard-references <see cref="User"/>, and letting a staff token
/// borrow that column would mean a row whose FK points at the wrong identity
/// table. Two identities, two token tables; the rotation and revocation rules
/// are identical.
///
/// <para>
/// This row is the server's only session state. The staff device stores the
/// token and unlocks it locally with a PIN or fingerprint, so those never reach
/// the server. Because the token expires, the device must fall back to a
/// password login once it lapses — that is what bounds a day session.
/// </para>
/// </summary>
public class StaffRefreshToken : AuditableEntity
{
    public Guid StaffId { get; private set; }
    public string Token { get; private set; } = default!;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? ReplacedByToken { get; private set; }
    public string? RevokedReason { get; private set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAtUtc;
    public bool IsRevoked => RevokedAtUtc is not null;
    public bool IsActive => !IsRevoked && !IsExpired;

    public Staff Staff { get; private set; } = default!;

    private StaffRefreshToken() { }

    public static StaffRefreshToken Create(Guid staffId, string token, int expiresInDays = 1)
    {
        return new StaffRefreshToken
        {
            StaffId = staffId,
            Token = token,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(expiresInDays),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void Revoke(string? reason = null, string? replacedByToken = null)
    {
        RevokedAtUtc = DateTimeOffset.UtcNow;
        RevokedReason = reason;
        ReplacedByToken = replacedByToken;
    }
}

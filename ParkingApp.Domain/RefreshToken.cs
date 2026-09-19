using ParkingApp.Domain.Common.Base;

namespace ParkingApp.Domain;

public class RefreshToken : AuditableEntity
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = default!;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? ReplacedByToken { get; private set; }
    public string? RevokedReason { get; private set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAtUtc;
    public bool IsRevoked => RevokedAtUtc is not null;
    public bool IsActive => !IsRevoked && !IsExpired;

    // Navigation property
    public User User { get; private set; } = default!;

    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, string token, int expiresInDays = 30)
    {
        return new RefreshToken
        {
            UserId = userId,
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

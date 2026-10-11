using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;
using ParkingApp.Domain;

namespace ParkingApp.Application.StaffAuth.Commands.RefreshToken;

/// <summary>
/// Rotates a staff session. The device sends the stored refresh token (the one
/// its local PIN/fingerprint unlocked); the server returns a new access token
/// and a new refresh token, and revokes the presented one. Reuse of a revoked
/// token is treated as theft and kills every live token for that staff member.
/// </summary>
public sealed record StaffRefreshTokenCommand(string RefreshToken)
    : IRequestResult<StaffRefreshTokenCommand, StaffRefreshTokenResponse>;

public sealed record StaffRefreshTokenResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    Guid StaffId,
    bool MustChangePassword);

public sealed class StaffRefreshTokenCommandHandler(
    IApplicationDbContext context,
    IStaffTokenService tokenService,
    JwtSettings jwtSettings)
    : IRequestResultHandler<StaffRefreshTokenCommand, StaffRefreshTokenResponse>
{
    public async Task<Result<StaffRefreshTokenResponse>> Handle(
        StaffRefreshTokenCommand request,
        CancellationToken cancellationToken = default)
    {
        var existing = await context.StaffRefreshTokens
            .Include(rt => rt.Staff)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (existing is null)
            return Result<StaffRefreshTokenResponse>.Failure("Invalid refresh token.", 401);

        if (!existing.IsActive)
        {
            if (existing.IsRevoked)
            {
                await RevokeAllAsync(context, existing.StaffId, "Potential token theft detected", cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
            }

            return Result<StaffRefreshTokenResponse>.Failure(
                "Refresh token is no longer valid. Please login again.", 401);
        }

        var staff = existing.Staff;

        if (staff.IsDeleted || !staff.IsActive)
            return Result<StaffRefreshTokenResponse>.Failure("Your account is no longer active.", 403);

        var newTokenString = tokenService.GenerateRefreshToken();
        existing.Revoke(reason: "Rotated", replacedByToken: newTokenString);

        var newToken = StaffRefreshToken.Create(
            existing.StaffId,
            newTokenString,
            jwtSettings.StaffRefreshTokenExpirationDays);

        context.StaffRefreshTokens.Add(newToken);

        var accessToken = tokenService.GenerateAccessToken(staff);

        await context.SaveChangesAsync(cancellationToken);

        return Result<StaffRefreshTokenResponse>.Success(
            new StaffRefreshTokenResponse(
                accessToken,
                DateTimeOffset.UtcNow.AddMinutes(jwtSettings.AccessTokenExpirationMinutes),
                newToken.Token,
                newToken.ExpiresAtUtc,
                staff.Id,
                staff.MustChangePassword));
    }

    private static async Task RevokeAllAsync(
        IApplicationDbContext context,
        Guid staffId,
        string reason,
        CancellationToken cancellationToken)
    {
        var active = await context.StaffRefreshTokens
            .Where(rt => rt.StaffId == staffId
                         && rt.RevokedAtUtc == null
                         && rt.ExpiresAtUtc > DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in active)
            token.Revoke(reason);
    }
}

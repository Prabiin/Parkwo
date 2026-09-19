using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Auth.Interfaces;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;
using ParkingApp.Application.Features.Shared;
using RefreshTokenEntity = ParkingApp.Domain.RefreshToken;

namespace ParkingApp.Application.Features.RefreshToken.Command;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequestResult<RefreshTokenCommand, RefreshTokenResponse>;

public sealed class RefreshTokenCommandHandler(IApplicationDbContext context, ITokenService tokenService, JwtSettings jwtSettings)
    : IRequestResultHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken = default)
    {
        var existingToken = await context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (existingToken is null)
            return Result<RefreshTokenResponse>.Failure("Invalid refresh token.", 401);

        if (!existingToken.IsActive)
        {
            if (existingToken.IsRevoked)
            {
                await AuthDbHelper.RevokeAllUserTokensAsync(
                    context, existingToken.UserId, "Potential token theft detected", cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
            }

            return Result<RefreshTokenResponse>.Failure("Refresh token is no longer valid. Please login again.", 401);
        }

        var newRefreshTokenString = tokenService.GenerateRefreshToken();
        existingToken.Revoke(reason: "Rotated", replacedByToken: newRefreshTokenString);

        var newRefreshToken = RefreshTokenEntity.Create(
            existingToken.UserId,
            newRefreshTokenString,
            jwtSettings.RefreshTokenExpirationDays);

        context.RefreshTokens.Add(newRefreshToken);

        var accessToken = tokenService.GenerateAccessToken(existingToken.User);

        await context.SaveChangesAsync(cancellationToken);

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(jwtSettings.AccessTokenExpirationMinutes);

        var isProfileComplete = AuthDbHelper.IsProfileComplete(existingToken.User);

        return Result<RefreshTokenResponse>.Success(
            new RefreshTokenResponse(existingToken.UserId, accessToken, newRefreshTokenString, expiresAt, isProfileComplete));
    }
}
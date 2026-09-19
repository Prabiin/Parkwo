using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Auth.Interfaces;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;
using ParkingApp.Application.Features.Shared;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;
using RefreshTokenEntity = ParkingApp.Domain.RefreshToken;

namespace ParkingApp.Application.Features.VerifyOtp.Command;

public record VerifyOtpCommand(string PhoneNumber, string Code)
    : IRequestResult<VerifyOtpCommand, VerifyOtpResponse>;

public sealed class VerifyOtpCommandHandler(IApplicationDbContext context, ITokenService tokenService, JwtSettings jwtSettings)
    : IRequestResultHandler<VerifyOtpCommand, VerifyOtpResponse>
{
    public async Task<Result<VerifyOtpResponse>> Handle(VerifyOtpCommand request, CancellationToken cancellationToken = default)
    {
        var otp = await context.Otps
            .Where(o => o.PhoneNumber == request.PhoneNumber
                        && o.Status == OtpStatusEnum.Pending)
            .OrderByDescending(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null)
            return Result<VerifyOtpResponse>.Failure("No pending OTP found. Please request a new one.");

        if (otp.HasExceededMaxAttempts())
            return Result<VerifyOtpResponse>.Failure("Too many failed attempts. Please request a new OTP.");

        if (!otp.Verify(request.Code))
        {
            await context.SaveChangesAsync(cancellationToken);

            if (otp.IsExpired)
                return Result<VerifyOtpResponse>.Failure("OTP has expired. Please request a new one.");

            return Result<VerifyOtpResponse>.Failure("Invalid OTP code.");
        }

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber, cancellationToken);

        var isNewUser = false;

        if (user is null)
        {
            user = new User
            {
                PhoneNumber = request.PhoneNumber,
                IsPhoneVerified = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            context.Users.Add(user);
            isNewUser = true;
        }
        else if (!user.IsPhoneVerified)
        {
            user.IsPhoneVerified = true;
            user.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        var isProfileComplete = AuthDbHelper.IsProfileComplete(user);

        var accessToken = tokenService.GenerateAccessToken(user);
        var refreshTokenString = tokenService.GenerateRefreshToken();
        var refreshToken = RefreshTokenEntity.Create(
            user.Id,
            refreshTokenString,
            jwtSettings.RefreshTokenExpirationDays);

        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync(cancellationToken);

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(jwtSettings.AccessTokenExpirationMinutes);

        return Result<VerifyOtpResponse>.Success(
            new VerifyOtpResponse(user.Id, accessToken, refreshTokenString, expiresAt, isNewUser, isProfileComplete));
    }
}
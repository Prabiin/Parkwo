using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Features.Shared;

internal static class AuthDbHelper
{
    public static string GenerateOtpCode()
        => RandomNumberGenerator.GetInt32(100000, 999999).ToString();

    public static bool IsProfileComplete(User user)
        // Profile picture is optional: a profile counts as complete without one.
        // Gender + DOB are mandatory onboarding fields (enforced by UpdateProfileCommand).
        => !string.IsNullOrWhiteSpace(user.FullName)
           && !string.IsNullOrWhiteSpace(user.Email)
           && user.Gender.HasValue
           && user.DateOfBirth.HasValue;

    public static async Task CancelPendingOtpsAsync(
        IApplicationDbContext dbContext,
        string phoneNumber,
        OtpTypeEnum purpose,
        CancellationToken cancellationToken)
    {
        var pendingOtps = await dbContext.Otps
            .Where(o => o.PhoneNumber == phoneNumber
                        && o.Purpose == purpose
                        && o.Status == OtpStatusEnum.Pending)
            .ToListAsync(cancellationToken);

        foreach (var otp in pendingOtps)
            otp.Cancel();
    }

    public static async Task RevokeAllUserTokensAsync(
        IApplicationDbContext dbContext,
        Guid userId,
        string reason,
        CancellationToken cancellationToken)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAtUtc == null && rt.ExpiresAtUtc > DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
            token.Revoke(reason);
    }
}
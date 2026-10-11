using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;
using ParkingApp.Domain;

namespace ParkingApp.Application.StaffAuth.Commands.Login;

/// <summary>
/// Staff app sign-in: email + password. This is the credential that starts the
/// day — the device keeps the refresh token and unlocks it locally with a PIN or
/// fingerprint, but those never reach the server, so once the token lapses the
/// staff member is back here for the password.
/// </summary>
public sealed record StaffLoginCommand(string Email, string Password)
    : IRequestResult<StaffLoginCommand, StaffLoginResponse>;

public sealed record StaffLoginResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    Guid StaffId,
    string FullName,
    string Email,
    Guid FacilityId,
    string FacilityName,
    bool MustChangePassword);

public sealed class StaffLoginCommandHandler(
    IApplicationDbContext context,
    IStaffTokenService tokenService,
    IPasswordHasher passwordHasher,
    JwtSettings jwtSettings)
    : IRequestResultHandler<StaffLoginCommand, StaffLoginResponse>
{
    public async Task<Result<StaffLoginResponse>> Handle(
        StaffLoginCommand request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var staff = await context.Staffs
            .Include(s => s.Facility)
            .FirstOrDefaultAsync(s => s.Email == email && !s.IsDeleted, cancellationToken);

        if (staff is null)
            return Result<StaffLoginResponse>.Failure("Invalid credentials.", 401);

        if (!staff.IsActive)
            return Result<StaffLoginResponse>.Failure("Your account is deactivated.", 403);

        if (!passwordHasher.Verify(request.Password, staff.PasswordHash))
            return Result<StaffLoginResponse>.Failure("Invalid credentials.", 401);

        staff.RecordLogin();
        await context.SaveChangesAsync(cancellationToken);

        var refreshToken = StaffRefreshToken.Create(
            staff.Id,
            tokenService.GenerateRefreshToken(),
            jwtSettings.StaffRefreshTokenExpirationDays);

        context.StaffRefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync(cancellationToken);

        var accessToken = tokenService.GenerateAccessToken(staff);

        return Result<StaffLoginResponse>.Success(
            new StaffLoginResponse(
                accessToken,
                DateTimeOffset.UtcNow.AddMinutes(jwtSettings.AccessTokenExpirationMinutes),
                refreshToken.Token,
                refreshToken.ExpiresAtUtc,
                staff.Id,
                staff.FullName,
                staff.Email,
                staff.FacilityId,
                staff.Facility?.Name ?? string.Empty,
                staff.MustChangePassword));
    }
}

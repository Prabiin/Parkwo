using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Auth.Interfaces;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;

namespace ParkingApp.Application.BackOffice.Commands.Login;

public sealed record BackOfficeLoginCommand(
    string UserNameOrEmail,
    string Password)
    : IRequestResult<BackOfficeLoginCommand, BackOfficeLoginResponse>;

public sealed class BackOfficeLoginCommandHandler(
    IApplicationDbContext context,
    IBackOfficeTokenService tokenService,
    IPasswordHasher passwordHasher,
    JwtSettings jwtSettings)
    : IRequestResultHandler<BackOfficeLoginCommand, BackOfficeLoginResponse>
{
    public async Task<Result<BackOfficeLoginResponse>> Handle(
        BackOfficeLoginCommand request,
        CancellationToken cancellationToken = default)
    {
        var normalized = request.UserNameOrEmail.Trim();

        var user = await context.BackOfficeUsers
            .FirstOrDefaultAsync(
                u => u.UserName == normalized || u.Email == normalized,
                cancellationToken);

        if (user is null)
            return Result<BackOfficeLoginResponse>.Failure("Invalid credentials.", 401);

        if (!user.IsActive)
            return Result<BackOfficeLoginResponse>.Failure("Your account is deactivated.", 403);

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result<BackOfficeLoginResponse>.Failure("Invalid credentials.", 401);

        user.LastLoginAtUtc = DateTimeOffset.UtcNow;
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        var accessToken = tokenService.GenerateAccessToken(user);
        var expiresAtUtc = DateTimeOffset.UtcNow
            .AddMinutes(jwtSettings.AccessTokenExpirationMinutes);

        return Result<BackOfficeLoginResponse>.Success(
            new BackOfficeLoginResponse(
                accessToken,
                expiresAtUtc,
                user.FullName,
                user.UserName,
                user.Email));
    }
}
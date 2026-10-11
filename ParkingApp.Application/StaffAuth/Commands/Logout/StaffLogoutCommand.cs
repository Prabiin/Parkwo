using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;

namespace ParkingApp.Application.StaffAuth.Commands.Logout;

/// <summary>Revokes the presented staff refresh token, ending the device's session.</summary>
public sealed record StaffLogoutCommand(string RefreshToken)
    : IRequestResult<StaffLogoutCommand, Unit>;

public sealed class StaffLogoutCommandHandler(IApplicationDbContext context)
    : IRequestResultHandler<StaffLogoutCommand, Unit>
{
    public async Task<Result<Unit>> Handle(
        StaffLogoutCommand request,
        CancellationToken cancellationToken = default)
    {
        var token = await context.StaffRefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (token is not null && token.IsActive)
        {
            token.Revoke("Logged out");
            await context.SaveChangesAsync(cancellationToken);
        }

        return Result<Unit>.Success(Unit.Value);
    }
}

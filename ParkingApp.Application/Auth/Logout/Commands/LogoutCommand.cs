using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using RefreshTokenEntity = ParkingApp.Domain.RefreshToken;

namespace ParkingApp.Application.Features.Logout.Command;

public sealed record LogoutCommand(string RefreshToken) : IRequestResult<LogoutCommand, LogoutResponse>;

public sealed class LogoutCommandHandler(IApplicationDbContext context)
    : IRequestResultHandler<LogoutCommand, LogoutResponse>
{
    public async Task<Result<LogoutResponse>> Handle(LogoutCommand request, CancellationToken cancellationToken = default)
    {
        var token = await context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (token is not null && token.IsActive)
        {
            token.Revoke("Logged out");
            await context.SaveChangesAsync(cancellationToken);
        }

        return Result<LogoutResponse>.Success(new LogoutResponse("Signed out successfully."));
    }
}
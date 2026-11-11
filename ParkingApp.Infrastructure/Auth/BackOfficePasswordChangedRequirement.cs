using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common.Interfaces;

namespace ParkingApp.Infrastructure.Auth;

/// <summary>
/// Gates the admin console behind a password that is no longer the
/// BackOffice-generated one. A fresh account can log in and change its
/// password, but cannot reach any other BackOffice route until it has.
///
/// <para>
/// The check reads the user row on every request rather than trusting a claim,
/// for the same reason the staff check does: a claim minted at login would stay
/// stale for the access token's whole lifetime, and a BackOffice resend has to
/// take effect immediately.
/// </para>
/// </summary>
public sealed class BackOfficePasswordChangedRequirement : IAuthorizationRequirement
{
}

public sealed class BackOfficePasswordChangedAuthorizationHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : AuthorizationHandler<BackOfficePasswordChangedRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext authorizationContext,
        BackOfficePasswordChangedRequirement requirement)
    {
        var userId = currentUserService.UserId;

        if (userId is null)
            return;

        var user = await dbContext.BackOfficeUsers
            .FirstOrDefaultAsync(u => !u.IsDeleted && u.Id == userId.Value);

        if (user is { MustChangePassword: false, IsActive: true })
            authorizationContext.Succeed(requirement);
    }
}

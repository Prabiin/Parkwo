using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common.Interfaces;

namespace ParkingApp.Infrastructure.Auth;

/// <summary>
/// Gates the operational staff routes (gate scan, walk-in desk, profile) behind
/// a password that is no longer the BackOffice-generated one. A fresh account
/// can log in and change its password, but cannot open the gate until it has.
///
/// <para>
/// The check reads the staff row on every request rather than trusting a claim,
/// for the same reason the rider profile check does: a claim minted at login
/// would stay stale for the access token's whole lifetime, and a BackOffice
/// password reset has to take effect immediately.
/// </para>
/// </summary>
public sealed class StaffPasswordChangedRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "StaffOperational";
}

public sealed class StaffPasswordChangedAuthorizationHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : AuthorizationHandler<StaffPasswordChangedRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext authorizationContext,
        StaffPasswordChangedRequirement requirement)
    {
        var staffId = currentUserService.StaffId;

        if (staffId is null)
            return;

        var staff = await dbContext.Staffs
            .FirstOrDefaultAsync(s => s.Id == staffId.Value);

        if (staff is { MustChangePassword: false, IsActive: true, IsDeleted: false })
            authorizationContext.Succeed(requirement);
    }
}

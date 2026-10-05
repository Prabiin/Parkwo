using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Features.Shared;

namespace ParkingApp.Infrastructure.Auth;

/// <summary>
/// Gates rider endpoints behind a completed onboarding profile. The check reads
/// the user from the database on every request rather than trusting a JWT claim,
/// because a claim minted at verify-otp time would stay stale for the whole
/// access-token lifetime and would also be re-minted by refresh-token.
/// </summary>
public sealed class ProfileCompleteRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "ProfileComplete";
}

public sealed class ProfileCompleteAuthorizationHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : AuthorizationHandler<ProfileCompleteRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext authorizationContext,
        ProfileCompleteRequirement requirement)
    {
        var userId = currentUserService.UserId;

        if (userId is null)
            return;

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId.Value);

        if (user is not null && AuthDbHelper.IsProfileComplete(user))
            authorizationContext.Succeed(requirement);
    }
}
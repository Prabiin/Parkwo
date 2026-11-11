using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common.Constants;
using ParkingApp.Application.Common.Interfaces;

namespace ParkingApp.Infrastructure.Auth;

/// <summary>
/// Requires the caller to hold a BackOffice menu action, e.g. ("User", "Edit").
/// The pair is exactly what the role form stores in
/// <c>BackOfficeRoleMenuActions</c>, so what an administrator grants in the
/// console and what the API enforces cannot drift apart.
///
/// <para>
/// The check reads the role live rather than trusting the permission claims
/// minted into the access token at login. A claim would stay frozen for the
/// token's whole lifetime (30 minutes here), so granting a permission would
/// not work until the user logged in again. Reading the role costs one
/// indexed lookup per request, which is nothing for admin-console traffic.
/// </para>
/// </summary>
public sealed class BackOfficePermissionRequirement : IAuthorizationRequirement
{
    public BackOfficePermissionRequirement(string menuKey, string[] actions, bool requireAll = false)
    {
        MenuKey = menuKey;
        Actions = actions;
        RequireAll = requireAll;
    }

    /// <summary>Menu key, e.g. "User", "Role", "StaffManagement".</summary>
    public string MenuKey { get; }

    /// <summary>Action keys, e.g. "List", "View", "Add", "Edit", "Delete".</summary>
    public string[] Actions { get; }

    /// <summary>
    /// True when every action must be held; false (the default) when any one
    /// of them grants access.
    /// </summary>
    public bool RequireAll { get; }
}

/// <summary>
/// The queries the permission check runs, kept public so the SQL they produce
/// can be asserted in a test. A query EF Core cannot translate only shows up
/// at runtime — as a 500 on every protected endpoint — so the shape is pinned
/// here instead of being discovered in production.
/// </summary>
public static class BackOfficePermissionQueries
{
    /// <summary>The name of the caller's role, or nothing when they have none.</summary>
    public static IQueryable<string> RoleNames(IApplicationDbContext dbContext, Guid userId)
        => dbContext.BackOfficeRoles
            .AsNoTracking()
            .Where(r => r.IsActive
                        && !r.IsDeleted
                        && r.UserRoles.Any(ur => ur.UserId == userId
                                                && ur.User != null
                                                && !ur.User.IsDeleted))
            .Select(r => r.Name);

    /// <summary>
    /// The action keys the caller's role holds for one menu. The role is
    /// matched through the user's role link, the menu through the action's own
    /// menu key, so this is one indexed join rather than a scan.
    /// </summary>
    public static IQueryable<string> GrantedActions(
        IApplicationDbContext dbContext, Guid userId, string menuKey)
        => dbContext.BackOfficeRoleMenuActions
            .AsNoTracking()
            .Where(r => r.Role.IsActive
                        && !r.Role.IsDeleted
                        && r.Role.UserRoles.Any(ur => ur.UserId == userId
                                                  && ur.User != null
                                                  && !ur.User.IsDeleted)
                        && r.MenuAction != null
                        && r.MenuAction.IsActive
                        && r.MenuAction.MenuKey == menuKey)
            .Select(r => r.MenuAction!.ActionKey)
            .Distinct();
}

public sealed class BackOfficePermissionAuthorizationHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : AuthorizationHandler<BackOfficePermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext authorizationContext,
        BackOfficePermissionRequirement requirement)
    {
        var userId = currentUserService.UserId;

        if (userId is null)
            return;

        var roleName = await BackOfficePermissionQueries
            .RoleNames(dbContext, userId.Value)
            .FirstOrDefaultAsync();

        // No role means no permissions at all.
        if (roleName is null)
            return;

        // SuperAdmin is the "everything" role. Its menu actions are linked by
        // the startup sync, but the bypass keeps the owner working even if
        // that sync has not run yet (fresh database, failed migration).
        if (roleName == BackOfficeConstants.SuperAdminRoleName)
        {
            authorizationContext.Succeed(requirement);
            return;
        }

        var granted = await BackOfficePermissionQueries
            .GrantedActions(dbContext, userId.Value, requirement.MenuKey)
            .ToListAsync();

        var allowed = requirement.RequireAll
            ? requirement.Actions.All(a => granted.Contains(a))
            : requirement.Actions.Any(a => granted.Contains(a));

        if (allowed)
            authorizationContext.Succeed(requirement);
    }
}

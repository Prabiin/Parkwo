using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ParkingApp.Infrastructure.Auth;

namespace ParkingApp.Api.Infrastructure;

public static class EndpointGroupExtensions
{
    public static WebApplication MapEndpoints(this WebApplication app)
    {
        var groupTypes = typeof(EndpointGroupBase).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(EndpointGroupBase)) && !t.IsAbstract);

        foreach (var groupType in groupTypes)
        {
            var group = Activator.CreateInstance(groupType) as EndpointGroupBase
                        ?? throw new InvalidOperationException($"Could not create endpoint group type '{groupType.Name}'.");
            group.Map(app);
        }

        return app;
    }

    // NOTE: endpoint groups call the framework's app.MapGroup(prefix) directly.
    // A previous helper overload took the group itself and re-entered group.Map(),
    // causing infinite recursion (StackOverflow) at startup. Do not reintroduce it.

    // These return the RouteGroupBuilder so a group can be built fluently
    // (map -> configure -> map -> configure). NOTE: a chained
    // .RequireAuthorization(...) therefore applies to the WHOLE group, not just
    // the route that was mapped. For a per-endpoint policy use the framework's
    // pattern-first overload (group.MapGet("path", Handler)) and chain on the
    // RouteHandlerBuilder it returns — as BackOfficeAuthApi does for
    // auth/change-password.

    public static RouteGroupBuilder MapGet(this RouteGroupBuilder builder, Delegate handler, string pattern = "", string groupName = "")
    {
        var endpoint = builder.MapGet(pattern, handler);
        ApplyGroupName(endpoint, groupName);
        return builder;
    }

    public static RouteGroupBuilder MapPost(this RouteGroupBuilder builder, Delegate handler, string pattern = "", string groupName = "", bool disableAntiforgery = false)
    {
        var endpoint = builder.MapPost(pattern, handler);
        // File-upload endpoints bind forms, which .NET auto-protects with cookie-based
        // antiforgery. Our clients use Bearer tokens (no ambient cookies), so CSRF does
        // not apply — opt out explicitly, otherwise mobile + browser uploads fail.
        if (disableAntiforgery)
            endpoint.DisableAntiforgery();
        ApplyGroupName(endpoint, groupName);
        return builder;
    }

    public static RouteGroupBuilder MapPut(this RouteGroupBuilder builder, Delegate handler, string pattern = "", string groupName = "", bool disableAntiforgery = false)
    {
        var endpoint = builder.MapPut(pattern, handler);
        // File-upload endpoints bind forms, which .NET auto-protects with cookie-based
        // antiforgery. Our clients use Bearer tokens (no ambient cookies), so CSRF does
        // not apply — opt out explicitly, otherwise mobile + browser uploads fail.
        if (disableAntiforgery)
            endpoint.DisableAntiforgery();
        ApplyGroupName(endpoint, groupName);
        return builder;
    }

    public static RouteGroupBuilder MapDelete(this RouteGroupBuilder builder, Delegate handler, string pattern = "", string groupName = "")
    {
        var endpoint = builder.MapDelete(pattern, handler);
        ApplyGroupName(endpoint, groupName);
        return builder;
    }

    /// <summary>
    /// Requires a BackOffice menu action, e.g. ("User", "Edit"). The menu key and
    /// action key are the pair the role form stores, so the console and the API
    /// enforce the same thing. The role is read from the database on every
    /// request, so a permission change takes effect immediately instead of
    /// waiting for the user to log in again.
    /// </summary>
    public static RouteHandlerBuilder RequireBackOfficePermission(
        this RouteHandlerBuilder endpoint, string menuKey, params string[] actions)
        => endpoint.RequireAuthorization(policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new BackOfficePermissionRequirement(menuKey, actions));
        });

    private static void ApplyGroupName(RouteHandlerBuilder endpoint, string groupName)
    {
        if (!string.IsNullOrWhiteSpace(groupName))
            endpoint.WithGroupName(groupName);
    }
}

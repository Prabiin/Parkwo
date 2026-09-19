using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

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

    public static RouteGroupBuilder MapGet(this RouteGroupBuilder builder, Delegate handler, string pattern = "", string groupName = "")
    {
        var endpoint = builder.MapGet(pattern, handler);
        ApplyGroupName(endpoint, groupName);
        return builder;
    }

    public static RouteGroupBuilder MapPost(this RouteGroupBuilder builder, Delegate handler, string pattern = "", string groupName = "")
    {
        var endpoint = builder.MapPost(pattern, handler);
        ApplyGroupName(endpoint, groupName);
        return builder;
    }

    public static RouteGroupBuilder MapPut(this RouteGroupBuilder builder, Delegate handler, string pattern = "", string groupName = "")
    {
        var endpoint = builder.MapPut(pattern, handler);
        ApplyGroupName(endpoint, groupName);
        return builder;
    }

    public static RouteGroupBuilder MapDelete(this RouteGroupBuilder builder, Delegate handler, string pattern = "", string groupName = "")
    {
        var endpoint = builder.MapDelete(pattern, handler);
        ApplyGroupName(endpoint, groupName);
        return builder;
    }

    private static void ApplyGroupName(RouteHandlerBuilder endpoint, string groupName)
    {
        if (!string.IsNullOrWhiteSpace(groupName))
            endpoint.WithGroupName(groupName);
    }
}
using ParkingApp.Api.Infrastructure;
using ParkingApp.Application.BackOffice.Commands.CreateRole;
using ParkingApp.Application.BackOffice.Commands.DeleteRole;
using ParkingApp.Application.BackOffice.Commands.SetActive;
using ParkingApp.Application.BackOffice.Commands.UpdateRole;
using ParkingApp.Application.BackOffice.Queries.GetRoleById;
using ParkingApp.Application.BackOffice.Queries.GetRoleInit;
using ParkingApp.Application.BackOffice.Queries.GetRoles;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;

namespace ParkingApp.Api.Apis.BackOffice.Roles;

public class BackOfficeRolesApi : BackOfficeGroup
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("backoffice/roles").RequireAuthorization(BackOfficePolicy);

        group.MapGet("", GetRoles)
            .RequireBackOfficePermission("Role", "List");
        // The init endpoint returns the permission catalog the role form is
        // built from, so it rides along with the list permission.
        group.MapGet("init", GetRoleInit)
            .RequireBackOfficePermission("Role", "List");
        group.MapGet("{roleId:guid}", GetRoleById)
            .RequireBackOfficePermission("Role", "View");
        group.MapPost("", CreateRole)
            .RequireBackOfficePermission("Role", "Add");
        group.MapPut("{roleId:guid}", UpdateRole)
            .RequireBackOfficePermission("Role", "Edit");
        // Pausing and resuming a role is a reversible switch; deleting it is
        // the final one. Both need the edit permission.
        group.MapPut("{roleId:guid}/active", SetActive)
            .RequireBackOfficePermission("Role", "Edit");
        group.MapDelete("{roleId:guid}", DeleteRole)
            .RequireBackOfficePermission("Role", "Delete");
    }

    private static async Task<IResult> GetRoles(
        ISender sender,
        IServiceProvider serviceProvider,
        string? searchTerm,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
        => await ExecuteQuery<GetRolesQuery, GetRolesResponse>(sender,
            new GetRolesQuery(searchTerm, page, pageSize), serviceProvider, cancellationToken);

    private static async Task<IResult> GetRoleInit(
        ISender sender,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
        => await ExecuteQuery<GetRoleInitQuery, GetRoleInitResponse>(sender,
            new GetRoleInitQuery(), serviceProvider, cancellationToken);

    private static async Task<IResult> GetRoleById(
        ISender sender,
        IServiceProvider serviceProvider,
        Guid roleId,
        CancellationToken cancellationToken = default)
        => await ExecuteQuery<GetRoleByIdQuery, GetRoleByIdResponse>(sender,
            new GetRoleByIdQuery(roleId), serviceProvider, cancellationToken);

    private static async Task<IResult> CreateRole(
        ISender sender,
        IServiceProvider serviceProvider,
        CreateRoleCommand request,
        CancellationToken cancellationToken = default)
        => await ExecuteCommand<CreateRoleCommand, CreateRoleResponse>(sender,
            request, serviceProvider, cancellationToken);

    private static async Task<IResult> UpdateRole(
        ISender sender,
        IServiceProvider serviceProvider,
        Guid roleId,
        UpdateRoleCommand request,
        CancellationToken cancellationToken = default)
        => await ExecuteCommand<UpdateRoleCommand, Unit>(sender,
            request with { RoleId = roleId }, serviceProvider, cancellationToken);

    private static async Task<IResult> SetActive(
        ISender sender,
        IServiceProvider serviceProvider,
        Guid roleId,
        SetRoleActiveRequest request,
        CancellationToken cancellationToken)
        => await ExecuteCommand<SetRoleActiveCommand, Unit>(sender,
            new SetRoleActiveCommand(roleId, request.IsActive), serviceProvider, cancellationToken);

    private static async Task<IResult> DeleteRole(
        ISender sender,
        IServiceProvider serviceProvider,
        Guid roleId,
        CancellationToken cancellationToken)
        => await ExecuteCommand<DeleteRoleCommand, Unit>(sender,
            new DeleteRoleCommand(roleId), serviceProvider, cancellationToken);
}

/// <summary>Wire shape of the pause/resume switch on a role.</summary>
public sealed class SetRoleActiveRequest
{
    public bool IsActive { get; set; } = true;
}
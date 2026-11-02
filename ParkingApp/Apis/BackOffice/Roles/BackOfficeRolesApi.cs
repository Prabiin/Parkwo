using ParkingApp.Application.BackOffice.Commands.CreateRole;
using ParkingApp.Application.BackOffice.Commands.DeleteRole;
using ParkingApp.Application.BackOffice.Commands.UpdateRole;
using ParkingApp.Application.BackOffice.Queries.GetRoleById;
using ParkingApp.Application.BackOffice.Queries.GetRoles;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;

namespace ParkingApp.Api.Apis.BackOffice.Roles;

public class BackOfficeRolesApi : BackOfficeGroup
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("backoffice/roles").RequireAuthorization(BackOfficePolicy);

        group.MapGet("", GetRoles);
        group.MapGet("{roleId:guid}", GetRoleById);
        group.MapPost("", CreateRole);
        group.MapPut("{roleId:guid}", UpdateRole);
        group.MapDelete("{roleId:guid}", DeleteRole);
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

    private static async Task<IResult> DeleteRole(
        ISender sender,
        IServiceProvider serviceProvider,
        Guid roleId,
        CancellationToken cancellationToken = default)
        => await ExecuteCommand<DeleteRoleCommand, Unit>(sender,
            new DeleteRoleCommand(roleId), serviceProvider, cancellationToken);
}
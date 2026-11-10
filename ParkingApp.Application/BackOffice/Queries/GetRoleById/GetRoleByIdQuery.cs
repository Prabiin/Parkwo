using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.BackOffice.Menu;
using ParkingApp.Domain.BackOffice.Role;

namespace ParkingApp.Application.BackOffice.Queries.GetRoleById;

public sealed record GetRoleByIdQuery(Guid RoleId)
    : IRequestResult<GetRoleByIdQuery, GetRoleByIdResponse>;

public sealed class GetRoleByIdQueryHandler(IApplicationDbContext context)
    : IRequestResultHandler<GetRoleByIdQuery, GetRoleByIdResponse>
{
    public async Task<Result<GetRoleByIdResponse>> Handle(
        GetRoleByIdQuery request,
        CancellationToken cancellationToken = default)
    {
        var role = await context.BackOfficeRoles
            .AsNoTracking()
            .Include(r => r.RoleMenuActions)
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);

        if (role is null)
            return Result<GetRoleByIdResponse>.Failure("Role not found.", 404);

        var assignedActionIds = role.RoleMenuActions
            .Select(rma => rma.MenuActionId)
            .ToHashSet();

        // Return the full permission catalog so the client can render every
        // action as a checkbox, with the ones already on this role pre-checked.
        var actions = await context.BackOfficeMenuActions
            .AsNoTracking()
            .Where(a => a.IsActive)
            .OrderBy(a => a.MenuKey)
            .ThenBy(a => a.ActionKey)
            .Select(a => new
            {
                a.Id,
                a.MenuKey,
                a.ActionKey,
                a.DisplayName
            })
            .ToListAsync(cancellationToken);

        var permissions = actions
            .Select(a => new RolePermissionResponse(
                a.Id,
                a.MenuKey,
                a.ActionKey,
                a.DisplayName,
                assignedActionIds.Contains(a.Id)))
            .ToList();

        var userCount = role.UserRoles.Count;

        return Result<GetRoleByIdResponse>.Success(
            new GetRoleByIdResponse(
                role.Id,
                role.Name,
                role.Description,
                role.IsSystem,
                role.CreatedAtUtc,
                role.UpdatedAtUtc,
                permissions,
                userCount));
    }
}
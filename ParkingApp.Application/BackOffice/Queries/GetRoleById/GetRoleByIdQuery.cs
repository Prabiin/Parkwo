using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Constants;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;

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

        // Return the same menu/action catalog as roles/init so the edit form
        // can reuse the add form's rendering, with assigned actions checked.
        var actions = await context.BackOfficeMenuActions
            .AsNoTracking()
            .Where(a => a.IsActive)
            .Select(a => new
            {
                a.Id,
                a.MenuKey,
                a.ActionKey,
                a.DisplayName,
                MenuTitle = a.Menu!.Title,
                MenuSortOrder = a.Menu!.SortOrder
            })
            .ToListAsync(cancellationToken);

        var menus = actions
            .GroupBy(a => new { a.MenuKey, a.MenuTitle, a.MenuSortOrder })
            .OrderBy(g => g.Key.MenuSortOrder)
            .ThenBy(g => g.Key.MenuTitle)
            .Select(g => new RoleMenuOption(
                g.Key.MenuKey,
                g.Key.MenuTitle,
                g.OrderBy(a => a.DisplayName)
                    .Select(a => new RoleActionOption(
                        a.Id,
                        a.ActionKey,
                        a.DisplayName,
                        assignedActionIds.Contains(a.Id)
                        || a.MenuKey == BackOfficeConstants.DefaultPermissionMenuKey))
                    .ToList()))
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
                menus,
                userCount));
    }
}

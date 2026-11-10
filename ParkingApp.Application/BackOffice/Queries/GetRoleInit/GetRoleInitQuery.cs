using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;

namespace ParkingApp.Application.BackOffice.Queries.GetRoleInit;

/// <summary>
/// Permission catalog for the role form: every active menu with its active
/// actions. The client renders each menu as an expandable group and each action
/// as a checkbox whose value is the action id posted back as PermissionIds.
/// </summary>
public sealed record GetRoleInitQuery
    : IRequestResult<GetRoleInitQuery, GetRoleInitResponse>;

public sealed class GetRoleInitQueryHandler(IApplicationDbContext context)
    : IRequestResultHandler<GetRoleInitQuery, GetRoleInitResponse>
{
    public async Task<Result<GetRoleInitResponse>> Handle(
        GetRoleInitQuery request,
        CancellationToken cancellationToken = default)
    {
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
                    .Select(a => new RoleActionOption(a.Id, a.ActionKey, a.DisplayName))
                    .ToList()))
            .ToList();

        return Result<GetRoleInitResponse>.Success(new GetRoleInitResponse(menus));
    }
}

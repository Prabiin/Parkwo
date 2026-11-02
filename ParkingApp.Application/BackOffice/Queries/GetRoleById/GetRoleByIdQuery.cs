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
                .ThenInclude(rma => rma.MenuAction)
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);

        if (role is null)
            return Result<GetRoleByIdResponse>.Failure("Role not found.", 404);

        var permissions = role.RoleMenuActions
            .Where(rma => rma.MenuAction != null)
            .Select(rma => new RolePermissionResponse(
                rma.MenuAction!.MenuKey,
                rma.MenuAction.ActionKey,
                rma.MenuAction.DisplayName))
            .OrderBy(p => p.MenuKey)
            .ThenBy(p => p.ActionKey)
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
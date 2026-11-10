using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Auth.Interfaces;
using ParkingApp.Application.BackOffice.Commands.Login;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;
using ParkingApp.Application.Common.Constants;
using ParkingApp.Domain;
using ParkingApp.Domain.BackOffice.Menu;
using ParkingApp.Domain.BackOffice.Role;

namespace ParkingApp.Application.BackOffice.Commands.Login;

public sealed record BackOfficeLoginCommand(
    string UserNameOrEmail,
    string Password)
    : IRequestResult<BackOfficeLoginCommand, BackOfficeLoginResponse>;

public sealed class BackOfficeLoginCommandHandler(
    IApplicationDbContext context,
    IBackOfficeTokenService tokenService,
    IPasswordHasher passwordHasher,
    JwtSettings jwtSettings)
    : IRequestResultHandler<BackOfficeLoginCommand, BackOfficeLoginResponse>
{
    public async Task<Result<BackOfficeLoginResponse>> Handle(
        BackOfficeLoginCommand request,
        CancellationToken cancellationToken = default)
    {
        var normalized = request.UserNameOrEmail.Trim();

        var user = await context.BackOfficeUsers
            .Include(u => u.UserRole)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RoleMenuActions)
                        .ThenInclude(rma => rma.MenuAction)
            .FirstOrDefaultAsync(
                u => u.UserName == normalized || u.Email == normalized,
                cancellationToken);

        if (user is null)
            return Result<BackOfficeLoginResponse>.Failure("Invalid credentials.", 401);

        if (!user.IsActive)
            return Result<BackOfficeLoginResponse>.Failure("Your account is deactivated.", 403);

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result<BackOfficeLoginResponse>.Failure("Invalid credentials.", 401);

        user.LastLoginAtUtc = DateTimeOffset.UtcNow;
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        // Build menu tree and permissions from user's role
        var (menus, permissions, isSuperAdmin) = await BuildMenuTreeAndPermissionsAsync(user, cancellationToken);

        var accessToken = tokenService.GenerateAccessToken(user, permissions, isSuperAdmin);
        var expiresAtUtc = DateTimeOffset.UtcNow
            .AddMinutes(jwtSettings.AccessTokenExpirationMinutes);

        return Result<BackOfficeLoginResponse>.Success(
            new BackOfficeLoginResponse(
                accessToken,
                expiresAtUtc,
                user.FullName,
                user.UserName,
                user.Email,
                user.MustChangePassword,
                menus,
                permissions));
    }

    private async Task<(IReadOnlyList<BackOfficeMenuDto> menus, IReadOnlyList<string> permissions, bool isSuperAdmin)>
        BuildMenuTreeAndPermissionsAsync(BackOfficeUser user, CancellationToken ct)
    {
        // Get role menu actions from user's single role (1:1)
        var role = user.UserRole?.Role;
        var roleMenuActions = role?.RoleMenuActions?
            .Where(rma => rma.MenuAction?.IsActive == true)
            .ToList() ?? new List<BackOfficeRoleMenuAction>();

        // Check if user has SuperAdmin role
        var isSuperAdmin = role?.Name == BackOfficeConstants.SuperAdminRoleName;

        // Build flat permissions list
        var permissions = roleMenuActions
            .Select(rma => $"{rma.MenuAction!.MenuKey}:{rma.MenuAction.ActionKey}")
            .Distinct()
            .OrderBy(p => p)
            .ToList();

        // Get all menu keys user has actions for
        var permittedMenuKeys = roleMenuActions
            .Where(rma => rma.MenuAction != null)
            .Select(rma => rma.MenuAction!.MenuKey)
            .Distinct()
            .ToHashSet();

        if (!permittedMenuKeys.Any())
            return ([], permissions, isSuperAdmin);

        // Query all menus (including parents) that the user has access to
        // User has access to a menu if they have actions on it OR on any of its descendants
        var allMenus = await context.BackOfficeMenus
            .OrderBy(m => m.SortOrder)
            .ToListAsync(ct);

        var menuLookup = allMenus.ToDictionary(m => m.Key);

        // Find all menus in the hierarchy that should be visible
        // A menu is visible if:
        // 1. User has direct actions on it (permittedMenuKeys contains its Key)
        // 2. OR it's an ancestor of a menu the user has actions on
        var visibleMenuKeys = new HashSet<string>(permittedMenuKeys);

        // Add all ancestors of permitted menus
        foreach (var key in permittedMenuKeys)
        {
            if (menuLookup.TryGetValue(key, out var menu))
            {
                var current = menu;
                while (current.ParentMenuId.HasValue)
                {
                    var parent = allMenus.FirstOrDefault(m => m.Id == current.ParentMenuId.Value);
                    if (parent == null) break;
                    visibleMenuKeys.Add(parent.Key);
                    current = parent;
                }
            }
        }

        // Filter to only visible menus
        var visibleMenus = allMenus
            .Where(m => visibleMenuKeys.Contains(m.Key))
            .ToList();

        // Build children lookup
        var childrenLookup = visibleMenus
            .Where(m => m.ParentMenuId.HasValue && visibleMenuKeys.Contains(
                allMenus.FirstOrDefault(p => p.Id == m.ParentMenuId.Value)?.Key ?? ""))
            .GroupBy(m => m.ParentMenuId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.SortOrder).ToList());

        // Build actions lookup
        var actionsLookup = roleMenuActions
            .Where(rma => rma.MenuAction != null && visibleMenuKeys.Contains(rma.MenuAction!.MenuKey))
            .GroupBy(rma => rma.MenuAction!.MenuKey)
            .ToDictionary(
                g => g.Key,
                g => g.Select(rma => rma.MenuAction!.ActionKey).Distinct().OrderBy(a => a).ToList());

        // Build tree recursively - roots are menus with no parent or parent not visible
        var rootMenus = visibleMenus
            .Where(m => !m.ParentMenuId.HasValue ||
                       !visibleMenuKeys.Contains(
                           allMenus.FirstOrDefault(p => p.Id == m.ParentMenuId.Value)?.Key ?? ""))
            .OrderBy(m => m.SortOrder)
            .ToList();

        var menuDtos = rootMenus.Select(MapToDto).ToList();

        return (menuDtos, permissions, isSuperAdmin);

        BackOfficeMenuDto MapToDto(BackOfficeMenu m)
        {
            var children = childrenLookup.GetValueOrDefault(m.Id, [])
                .Select(MapToDto)
                .ToList();

            return new BackOfficeMenuDto(
                Key: m.Key,
                Title: m.Title,
                Route: m.Route,
                Icon: m.Icon,
                Children: children,
                Actions: actionsLookup.GetValueOrDefault(m.Key, []));
        }
    }
}
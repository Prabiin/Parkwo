using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Constants;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.BackOffice.Menu;
using ParkingApp.Domain.BackOffice.Role;

namespace ParkingApp.Application.BackOffice.Commands.UpdateRole;

public sealed record UpdateRoleCommand(
    Guid RoleId,
    string? Name,
    string? Description,
    IReadOnlyList<Guid>? PermissionIds)
    : IRequestResult<UpdateRoleCommand, Unit>;

public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Name));

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.PermissionIds)
            .NotNull()
            .When(x => x.PermissionIds != null);
    }
}

public sealed class UpdateRoleCommandHandler(IApplicationDbContext context)
    : IRequestResultHandler<UpdateRoleCommand, Unit>
{
    public async Task<Result<Unit>> Handle(
        UpdateRoleCommand request,
        CancellationToken cancellationToken = default)
    {
        var role = await context.BackOfficeRoles
            .Include(r => r.RoleMenuActions)
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);

        if (role is null)
            return Result<Unit>.Failure("Role not found.", 404);

        if (role.IsSystem)
            return Result<Unit>.Failure("System roles cannot be modified.", 400);

        // Update name if provided
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var normalizedName = request.Name.Trim();
            var existingRole = await context.BackOfficeRoles
                .FirstOrDefaultAsync(r => r.Name == normalizedName && r.Id != request.RoleId, cancellationToken);

            if (existingRole is not null)
                return Result<Unit>.Failure("Role with this name already exists.", 400);

            role.Name = normalizedName;
        }

        // Update description if provided
        if (request.Description != null)
        {
            role.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        }

        // Update permissions if provided
        if (request.PermissionIds != null)
        {
            // Validate permission IDs exist
            if (request.PermissionIds.Count > 0)
            {
                var validPermissionIds = await context.BackOfficeMenuActions
                    .Where(a => request.PermissionIds.Contains(a.Id))
                    .Select(a => a.Id)
                    .ToListAsync(cancellationToken);

                var invalidIds = request.PermissionIds.Except(validPermissionIds).ToList();
                if (invalidIds.Count > 0)
                    return Result<Unit>.Failure($"Invalid permission IDs: {string.Join(", ", invalidIds)}", 400);
            }

            // Dashboard is mandatory (the landing page), so keep it even if the
            // client submits a set that omits it.
            var defaultActionIds = await context.BackOfficeMenuActions
                .Where(a => a.IsActive && a.MenuKey == BackOfficeConstants.DefaultPermissionMenuKey)
                .Select(a => a.Id)
                .ToListAsync(cancellationToken);

            var permissionIds = request.PermissionIds
                .Union(defaultActionIds)
                .ToList();

            // Replace permissions
            role.RoleMenuActions.Clear();
            foreach (var permissionId in permissionIds)
            {
                role.RoleMenuActions.Add(new BackOfficeRoleMenuAction
                {
                    MenuActionId = permissionId,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
            }
        }

        role.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
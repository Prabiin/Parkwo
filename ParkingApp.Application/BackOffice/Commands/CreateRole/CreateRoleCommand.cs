using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.BackOffice.Audit;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Constants;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.BackOffice.Role;

namespace ParkingApp.Application.BackOffice.Commands.CreateRole;

public sealed record CreateRoleCommand(
    string Name,
    string? Description,
    IReadOnlyList<Guid> PermissionIds)
    : IRequestResult<CreateRoleCommand, CreateRoleResponse>;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.PermissionIds)
            .NotNull();
    }
}

public sealed class CreateRoleCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IBackOfficeAuditWriter auditWriter)
    : IRequestResultHandler<CreateRoleCommand, CreateRoleResponse>
{
    public async Task<Result<CreateRoleResponse>> Handle(
        CreateRoleCommand request,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = request.Name.Trim();

        // A name is only free if no live role holds it. A deleted role has
        // released its name, so the role can be recreated; an inactive role
        // still holds it, because it can be switched back on.
        var existingRole = await context.BackOfficeRoles
            .FirstOrDefaultAsync(r => !r.IsDeleted && r.Name == normalizedName, cancellationToken);

        if (existingRole is not null)
            return Result<CreateRoleResponse>.Failure("Role name already exist.", 400);

        // Validate permission IDs exist
        if (request.PermissionIds.Count > 0)
        {
            var validPermissionIds = await context.BackOfficeMenuActions
                .Where(a => request.PermissionIds.Contains(a.Id))
                .Select(a => a.Id)
                .ToListAsync(cancellationToken);

            var invalidIds = request.PermissionIds.Except(validPermissionIds).ToList();
            if (invalidIds.Count > 0)
                return Result<CreateRoleResponse>.Failure($"Invalid permission IDs: {string.Join(", ", invalidIds)}", 400);
        }

        // Dashboard is mandatory (the landing page) even when the client sends
        // no permissions, so always include its active actions.
        var defaultActionIds = await context.BackOfficeMenuActions
            .Where(a => a.IsActive && a.MenuKey == BackOfficeConstants.DefaultPermissionMenuKey)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        var permissionIds = request.PermissionIds
            .Union(defaultActionIds)
            .ToList();

        var role = new BackOfficeRole
        {
            Name = normalizedName,
            Description = request.Description?.Trim(),
            IsSystem = false,
            IsActive = true,
            IsDeleted = false,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = currentUser.UserId
        };

        role.RoleMenuActions = permissionIds
            .Select(id => new BackOfficeRoleMenuAction
            {
                MenuActionId = id,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                CreatedBy = currentUser.UserId
            })
            .ToList();

        context.BackOfficeRoles.Add(role);

        await auditWriter.RecordAsync(
            "Create",
            "BackOfficeRole",
            role.Id,
            $"Created role '{role.Name}' with {permissionIds.Count} permission(s).",
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return Result<CreateRoleResponse>.Success(
            new CreateRoleResponse(role.Id, role.Name));
    }
}
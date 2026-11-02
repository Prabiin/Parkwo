using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
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

public sealed class CreateRoleCommandHandler(IApplicationDbContext context)
    : IRequestResultHandler<CreateRoleCommand, CreateRoleResponse>
{
    public async Task<Result<CreateRoleResponse>> Handle(
        CreateRoleCommand request,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = request.Name.Trim();

        // Check if role name already exists
        var existingRole = await context.BackOfficeRoles
            .FirstOrDefaultAsync(r => r.Name == normalizedName, cancellationToken);

        if (existingRole is not null)
            return Result<CreateRoleResponse>.Failure("Role with this name already exists.", 400);

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

        var role = new BackOfficeRole
        {
            Name = normalizedName,
            Description = request.Description?.Trim(),
            IsSystem = false,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        if (request.PermissionIds.Count > 0)
        {
            role.RoleMenuActions = request.PermissionIds
                .Select(id => new BackOfficeRoleMenuAction
                {
                    MenuActionId = id,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                })
                .ToList();
        }

        context.BackOfficeRoles.Add(role);
        await context.SaveChangesAsync(cancellationToken);

        return Result<CreateRoleResponse>.Success(
            new CreateRoleResponse(role.Id, role.Name));
    }
}
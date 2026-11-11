using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.BackOffice.Audit;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;

namespace ParkingApp.Application.BackOffice.Commands.SetActive;

/// <summary>
/// Pauses or resumes a role. This is the reversible switch: an inactive role
/// still exists, still carries its permissions, and can be switched back on at
/// any time. Deleting a role is the other, final switch.
/// </summary>
public sealed record SetRoleActiveCommand(Guid RoleId, bool IsActive)
    : IRequestResult<SetRoleActiveCommand, Unit>;

public sealed class SetRoleActiveCommandValidator : AbstractValidator<SetRoleActiveCommand>
{
    public SetRoleActiveCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty();
    }
}

public sealed class SetRoleActiveCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IBackOfficeAuditWriter auditWriter)
    : IRequestResultHandler<SetRoleActiveCommand, Unit>
{
    public async Task<Result<Unit>> Handle(
        SetRoleActiveCommand request,
        CancellationToken cancellationToken = default)
    {
        var role = await context.BackOfficeRoles
            .FirstOrDefaultAsync(r => !r.IsDeleted && r.Id == request.RoleId, cancellationToken);

        if (role is null)
            return Result<Unit>.Failure("Role not found.", 404);

        // Pausing a system role would lock every administrator out of the
        // console, because the permission check requires an active role.
        if (role.IsSystem && !request.IsActive)
            return Result<Unit>.Failure("System roles cannot be deactivated.", 400);

        // A deleted role cannot be brought back through the console.
        if (role.IsDeleted)
            return Result<Unit>.Failure("This role has been deleted and cannot be reactivated.", 400);

        if (role.IsActive == request.IsActive)
            return Result<Unit>.Success(Unit.Value);

        // The permissions are untouched: pausing a role is about the role, not
        // about what it can do, so reactivating it must not require re-granting
        // access.
        role.SetActive(request.IsActive);
        role.UpdatedBy = currentUser.UserId;

        await auditWriter.RecordAsync(
            request.IsActive ? "Activate" : "Deactivate",
            "BackOfficeRole",
            role.Id,
            request.IsActive
                ? $"Reactivated role '{role.Name}'."
                : $"Deactivated role '{role.Name}'. Its permissions were kept.",
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}

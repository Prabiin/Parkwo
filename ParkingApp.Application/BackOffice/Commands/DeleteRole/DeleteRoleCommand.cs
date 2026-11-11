using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.BackOffice.Audit;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.BackOffice.Role;

namespace ParkingApp.Application.BackOffice.Commands.DeleteRole;

public sealed record DeleteRoleCommand(Guid RoleId)
    : IRequestResult<DeleteRoleCommand, Unit>;

public sealed class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty();
    }
}

public sealed class DeleteRoleCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IBackOfficeAuditWriter auditWriter)
    : IRequestResultHandler<DeleteRoleCommand, Unit>
{
    public async Task<Result<Unit>> Handle(
        DeleteRoleCommand request,
        CancellationToken cancellationToken = default)
    {
        var role = await context.BackOfficeRoles
            .Include(r => r.UserRoles)
                .ThenInclude(ur => ur.User)
            .FirstOrDefaultAsync(r => !r.IsDeleted && r.Id == request.RoleId, cancellationToken);

        if (role is null)
            return Result<Unit>.Failure("Role not found.", 404);

        if (role.IsSystem)
            return Result<Unit>.Failure("System roles cannot be deleted.", 400);

        // A role still held by a live account is not deleted out from under
        // them. Deleted accounts do not count: they are gone from the console
        // and their link is only a leftover.
        var assignedUsers = role.UserRoles.Count(ur => ur.User is { IsDeleted: false });

        if (assignedUsers > 0)
            return Result<Unit>.Failure("Cannot delete role with assigned users. Unassign users first.", 400);

        // Soft delete. The row and the role's menu actions are kept, so the
        // history of who had what survives and a mistake can be undone by
        // patching the row. The role disappears from the console and is
        // inactive, so nobody holding it can sign in.
        role.SoftDelete();
        role.UpdatedBy = currentUser.UserId;

        await auditWriter.RecordAsync(
            "Delete",
            "BackOfficeRole",
            role.Id,
            $"Deleted role '{role.Name}'. Its permissions were kept on the record.",
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}

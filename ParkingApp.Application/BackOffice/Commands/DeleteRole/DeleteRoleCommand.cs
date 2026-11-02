using FluentValidation;
using Microsoft.EntityFrameworkCore;
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

public sealed class DeleteRoleCommandHandler(IApplicationDbContext context)
    : IRequestResultHandler<DeleteRoleCommand, Unit>
{
    public async Task<Result<Unit>> Handle(
        DeleteRoleCommand request,
        CancellationToken cancellationToken = default)
    {
        var role = await context.BackOfficeRoles
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);

        if (role is null)
            return Result<Unit>.Failure("Role not found.", 404);

        if (role.IsSystem)
            return Result<Unit>.Failure("System roles cannot be deleted.", 400);

        if (role.UserRoles.Count > 0)
            return Result<Unit>.Failure("Cannot delete role with assigned users. Unassign users first.", 400);

        context.BackOfficeRoles.Remove(role);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
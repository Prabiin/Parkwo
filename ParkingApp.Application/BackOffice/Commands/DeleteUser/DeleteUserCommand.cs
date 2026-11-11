using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.BackOffice.Audit;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain;

namespace ParkingApp.Application.BackOffice.Commands.DeleteUser;

public sealed record DeleteUserCommand(Guid UserId)
    : IRequestResult<DeleteUserCommand, Unit>;

public sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}

public sealed class DeleteUserCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IBackOfficeAuditWriter auditWriter)
    : IRequestResultHandler<DeleteUserCommand, Unit>
{
    public async Task<Result<Unit>> Handle(
        DeleteUserCommand request,
        CancellationToken cancellationToken = default)
    {
        var user = await context.BackOfficeUsers
            .FirstOrDefaultAsync(u => !u.IsDeleted && u.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result<Unit>.Failure("User not found.", 404);

        // Check if this is the SuperAdmin user
        var superAdminUserId = new Guid("3f2504e0-4f89-11d3-9a0c-0305e82c3301");
        if (user.Id == superAdminUserId)
            return Result<Unit>.Failure("Cannot delete the SuperAdmin user.", 400);

        // Soft delete, not remove. The row is what the audit trail points at
        // and what the permissions they granted were attached to; dropping it
        // would erase the history of who did what. The account also leaves the
        // active roster and can no longer sign in.
        user.SoftDelete();
        user.UpdatedBy = currentUser.UserId;

        await auditWriter.RecordAsync(
            "Delete",
            "BackOfficeUser",
            user.Id,
            $"Deleted BackOffice user '{user.UserName}'.",
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
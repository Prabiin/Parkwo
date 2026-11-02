using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
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

public sealed class DeleteUserCommandHandler(IApplicationDbContext context)
    : IRequestResultHandler<DeleteUserCommand, Unit>
{
    public async Task<Result<Unit>> Handle(
        DeleteUserCommand request,
        CancellationToken cancellationToken = default)
    {
        var user = await context.BackOfficeUsers
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result<Unit>.Failure("User not found.", 404);

        // Check if this is the SuperAdmin user
        var superAdminUserId = new Guid("3f2504e0-4f89-11d3-9a0c-0305e82c3301");
        if (user.Id == superAdminUserId)
            return Result<Unit>.Failure("Cannot delete the SuperAdmin user.", 400);

        context.BackOfficeUsers.Remove(user);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
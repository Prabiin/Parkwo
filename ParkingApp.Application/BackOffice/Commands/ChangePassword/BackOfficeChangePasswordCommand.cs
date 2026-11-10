using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;

namespace ParkingApp.Application.BackOffice.Commands.ChangePassword;

/// <summary>
/// The admin replaces the generated password with their own. The current
/// password is required even on the forced first change, so a device that merely
/// holds a live session cannot silently reset the credential. Clearing
/// <see cref="Domain.BackOfficeUser.MustChangePassword"/> is what opens the rest
/// of the admin console; the client should sign in again afterwards.
/// </summary>
public sealed record BackOfficeChangePasswordCommand(
    string CurrentPassword,
    string NewPassword)
    : IRequestResult<BackOfficeChangePasswordCommand, Unit>;

public sealed class BackOfficeChangePasswordCommandValidator : AbstractValidator<BackOfficeChangePasswordCommand>
{
    public BackOfficeChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8);
    }
}

public sealed class BackOfficeChangePasswordCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IPasswordHasher passwordHasher)
    : IRequestResultHandler<BackOfficeChangePasswordCommand, Unit>
{
    public async Task<Result<Unit>> Handle(
        BackOfficeChangePasswordCommand request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<Unit>.Failure("Authentication required.", 401);

        var user = await context.BackOfficeUsers
            .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

        if (user is null)
            return Result<Unit>.Failure("User not found.", 404);

        if (!user.IsActive)
            return Result<Unit>.Failure("Your account is deactivated.", 403);

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return Result<Unit>.Failure("Your current password is incorrect.", 400);

        if (passwordHasher.Verify(request.NewPassword, user.PasswordHash))
            return Result<Unit>.Failure("Choose a password you have not used before.", 400);

        user.SetPassword(passwordHasher.Hash(request.NewPassword), mustChangePassword: false);

        await context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}

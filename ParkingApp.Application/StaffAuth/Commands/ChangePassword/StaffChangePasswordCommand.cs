using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;

namespace ParkingApp.Application.StaffAuth.Commands.ChangePassword;

/// <summary>
/// The staff member replaces the generated password with their own. The current
/// password is required even on the forced first change, so a device that merely
/// holds a live session cannot silently reset the credential. Clearing
/// <see cref="Domain.Staff.MustChangePassword"/> is what opens the gate routes.
/// </summary>
public sealed record StaffChangePasswordCommand(
    string CurrentPassword,
    string NewPassword)
    : IRequestResult<StaffChangePasswordCommand, Unit>;

public sealed class StaffChangePasswordCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IPasswordHasher passwordHasher)
    : IRequestResultHandler<StaffChangePasswordCommand, Unit>
{
    public async Task<Result<Unit>> Handle(
        StaffChangePasswordCommand request,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.StaffId;
        if (staffId is null)
            return Result<Unit>.Failure("Authentication required.", 401);

        var staff = await context.Staffs
            .FirstOrDefaultAsync(s => s.Id == staffId.Value && !s.IsDeleted, cancellationToken);

        if (staff is null)
            return Result<Unit>.Failure("Staff member not found.", 404);

        if (!passwordHasher.Verify(request.CurrentPassword, staff.PasswordHash))
            return Result<Unit>.Failure("Your current password is incorrect.", 400);

        if (passwordHasher.Verify(request.NewPassword, staff.PasswordHash))
            return Result<Unit>.Failure("Choose a password you have not used before.", 400);

        staff.SetPassword(passwordHasher.Hash(request.NewPassword), mustChangePassword: false);

        await context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}

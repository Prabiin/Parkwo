using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;

namespace ParkingApp.Application.Staffs.Commands.ResetPassword;

/// <summary>
/// BackOffice issues a fresh generated password and emails it. The account is
/// pushed back to the forced-change state, so a reset cannot be used to hand
/// someone a long-lived shared secret.
/// </summary>
public sealed record ResetStaffPasswordCommand(Guid StaffId)
    : IRequestResult<ResetStaffPasswordCommand, Unit>;

public sealed class ResetStaffPasswordCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    ICurrentUserService currentUser)
    : IRequestResultHandler<ResetStaffPasswordCommand, Unit>
{
    public async Task<Result<Unit>> Handle(
        ResetStaffPasswordCommand request,
        CancellationToken cancellationToken = default)
    {
        var staff = await context.Staffs
            .FirstOrDefaultAsync(s => s.Id == request.StaffId && !s.IsDeleted, cancellationToken);

        if (staff is null)
            return Result<Unit>.Failure("Staff member not found.", 404);

        var password = PasswordGenerator.Generate();
        staff.SetPassword(passwordHasher.Hash(password), mustChangePassword: true);
        staff.UpdatedBy = currentUser.UserId;

        await context.SaveChangesAsync(cancellationToken);

        await emailSender.SendAsync(
            staff.Email,
            "Your Parkwo staff password was reset",
            StaffEmailTemplates.PasswordReset(staff.FullName, staff.Email, password),
            cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}

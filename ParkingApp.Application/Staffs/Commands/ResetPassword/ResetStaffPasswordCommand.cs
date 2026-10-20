using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;

namespace ParkingApp.Application.Staffs.Commands.ResetPassword;

/// <summary>
/// BackOffice issues a fresh generated password and emails it. This is both the
/// "forgot it" reset and the "the email never arrived" resend: a lost password
/// is only ever held as a hash, so there is nothing to re-send and a new secret
/// is the only honest answer. The account is pushed back to the forced-change
/// state, so neither use can hand someone a long-lived shared secret.
/// </summary>
public sealed record ResetStaffPasswordCommand(Guid StaffId)
    : IRequestResult<ResetStaffPasswordCommand, Unit>;

public sealed class ResetStaffPasswordCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    ILogger<ResetStaffPasswordCommandHandler> logger,
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

        // Rotate first, then try to deliver. Unlike a first send there is no
        // row to protect — the old password was already unusable to whoever
        // asked for this, either because they forgot it or because they never
        // received it — so the new hash is what the account should hold and a
        // failed delivery just means resend again. Holding the rotation back
        // until the mail succeeded would leave the account on a secret the
        // caller has already told us is no good.
        staff.SetPassword(passwordHasher.Hash(password), mustChangePassword: true);
        staff.UpdatedBy = currentUser.UserId;

        await context.SaveChangesAsync(cancellationToken);

        await StaffPasswordDelivery.SendAsync(
            emailSender,
            logger,
            staff,
            "Your Parkwo staff password was reset",
            StaffEmailTemplates.PasswordReset(staff.FullName, staff.Email, password),
            cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}

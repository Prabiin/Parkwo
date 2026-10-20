using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;

namespace ParkingApp.Application.Staffs.Commands.ResendInvite;

/// <summary>
/// BackOffice re-issues the first password because the staff member never got
/// the emailed one — a wrong address at creation, a relay that was down, or a
/// message lost on the way.
/// <para>
/// A genuinely separate action from the password reset even though it writes the
/// same columns, because the two answer different problems. A reset is "they
/// lost the password they were already given" and can happen at any point in the
/// account's life; this is "the invite was never received", so it is only legal
/// while the invite is still outstanding. Gating it on
/// <see cref="Staff.MustChangePassword"/> is what keeps the two apart: once the
/// staff member has set their own password the invite is spent, and a locked-out
/// staff member is a reset, not a resend. Answering that with a resend would
/// quietly mint a fresh credential for an established account.
/// </para>
/// <para>
/// The new password replaces the old hash for the same reason a reset does: the
/// original exists only as a hash, so "re-send the same one" is not something
/// the server is able to do. The one that was never delivered simply stops
/// working.
/// </para>
/// </summary>
public sealed record ResendStaffInviteCommand(Guid StaffId)
    : IRequestResult<ResendStaffInviteCommand, Unit>;

public sealed class ResendStaffInviteCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    ILogger<ResendStaffInviteCommandHandler> logger,
    ICurrentUserService currentUser)
    : IRequestResultHandler<ResendStaffInviteCommand, Unit>
{
    public async Task<Result<Unit>> Handle(
        ResendStaffInviteCommand request,
        CancellationToken cancellationToken = default)
    {
        var staff = await context.Staffs
            .FirstOrDefaultAsync(s => s.Id == request.StaffId && !s.IsDeleted, cancellationToken);

        if (staff is null)
            return Result<Unit>.Failure("Staff member not found.", 404);

        if (!StaffPasswordDelivery.IsPasswordOutstanding(staff))
            return Result<Unit>.Failure(
                "This staff member has already set their own password. Reset the password instead.", 409);

        if (!staff.IsActive)
            return Result<Unit>.Failure(
                "This staff member is deactivated. Reactivate the account before resending the invite.", 409);

        var password = PasswordGenerator.Generate();

        // Persist then deliver, matching the create flow: the account has to
        // end up holding the password that was actually emailed, so the row is
        // written first and a failed send is retried by pressing the button
        // again. Nothing is undone by a delivery failure, because the previous
        // password was already known to be undelivered — that is why this was
        // called.
        staff.SetPassword(passwordHasher.Hash(password), mustChangePassword: true);
        staff.UpdatedBy = currentUser.UserId;

        await context.SaveChangesAsync(cancellationToken);

        await StaffPasswordDelivery.SendAsync(
            emailSender,
            logger,
            staff,
            "Your Parkwo staff account",
            StaffEmailTemplates.Welcome(staff.FullName, staff.Email, password),
            cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}

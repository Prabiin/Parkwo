using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ParkingApp.Application.BackOffice;
using ParkingApp.Application.BackOffice.Audit;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;

namespace ParkingApp.Application.BackOffice.Commands.ResendInvite;

/// <summary>
/// BackOffice re-issues the first password because the user never got the
/// emailed one — a wrong address at creation, a relay that was down, or a
/// message lost on the way.
/// <para>
/// Only legal while the invite is still outstanding
/// (<see cref="Domain.BackOfficeUser.MustChangePassword"/> is true). Once the
/// user has set their own password the invite is spent; a locked-out user needs
/// a reset, not a resend. The new password replaces the old hash because the
/// original exists only as a hash — "send the same one again" is not something
/// the server can do.
/// </para>
/// </summary>
public sealed record ResendUserInviteCommand(Guid UserId)
    : IRequestResult<ResendUserInviteCommand, Unit>;

public sealed class ResendUserInviteCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    ILogger<ResendUserInviteCommandHandler> logger,
    ICurrentUserService currentUser,
    IBackOfficeAuditWriter auditWriter)
    : IRequestResultHandler<ResendUserInviteCommand, Unit>
{
    public async Task<Result<Unit>> Handle(
        ResendUserInviteCommand request,
        CancellationToken cancellationToken = default)
    {
        var user = await context.BackOfficeUsers
            .FirstOrDefaultAsync(u => !u.IsDeleted && u.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result<Unit>.Failure("User not found.", 404);

        if (!BackOfficeUserPasswordDelivery.IsPasswordOutstanding(user))
            return Result<Unit>.Failure(
                "This user has already set their own password.", 409);

        if (!user.IsActive)
            return Result<Unit>.Failure(
                "This user is deactivated. Reactivate the account before resending the invite.", 409);

        var password = PasswordGenerator.Generate();

        // Persist then deliver, matching the create flow: the account has to
        // end up holding the password that was actually emailed, so the row is
        // written first and a failed send is retried by pressing the button
        // again.
        user.SetPassword(passwordHasher.Hash(password), mustChangePassword: true);
        user.UpdatedBy = currentUser.UserId;

        await auditWriter.RecordAsync(
            "ResendInvite",
            "BackOfficeUser",
            user.Id,
            $"Reissued the password for BackOffice user '{user.UserName}'.",
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        await BackOfficeUserPasswordDelivery.SendAsync(
            emailSender,
            logger,
            user,
            "Your Parkwo admin account",
            BackOfficeUserEmailTemplates.Welcome(user.FullName, user.UserName, user.Email, password),
            cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}

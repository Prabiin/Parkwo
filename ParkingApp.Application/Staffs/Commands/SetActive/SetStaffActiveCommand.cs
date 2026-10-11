using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;

namespace ParkingApp.Application.Staffs.Commands.SetActive;

/// <summary>
/// BackOffice deactivates or reactivates a staff member. A deactivated account
/// cannot sign in and cannot scan — <see cref="Common.Helpers.GateAccess"/>
/// rejects it — but its gate history stays attributable because the row is kept.
/// </summary>
public sealed record SetStaffActiveCommand(Guid StaffId, bool IsActive)
    : IRequestResult<SetStaffActiveCommand, Unit>;

public sealed class SetStaffActiveCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestResultHandler<SetStaffActiveCommand, Unit>
{
    public async Task<Result<Unit>> Handle(
        SetStaffActiveCommand request,
        CancellationToken cancellationToken = default)
    {
        var staff = await context.Staffs
            .FirstOrDefaultAsync(s => s.Id == request.StaffId && !s.IsDeleted, cancellationToken);

        if (staff is null)
            return Result<Unit>.Failure("Staff member not found.", 404);

        staff.SetActive(request.IsActive);
        staff.UpdatedBy = currentUser.UserId;

        await context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}

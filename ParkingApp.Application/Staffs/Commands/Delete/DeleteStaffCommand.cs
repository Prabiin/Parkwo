using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;

namespace ParkingApp.Application.Staffs.Commands.Delete;

/// <summary>
/// BackOffice soft-deletes a staff member: the account disappears from the
/// roster and can no longer sign in, but the row survives so bookings that name
/// them as the gate operator still resolve to a real record.
/// </summary>
public sealed record DeleteStaffCommand(Guid StaffId)
    : IRequestResult<DeleteStaffCommand, Unit>;

public sealed class DeleteStaffCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestResultHandler<DeleteStaffCommand, Unit>
{
    public async Task<Result<Unit>> Handle(
        DeleteStaffCommand request,
        CancellationToken cancellationToken = default)
    {
        var staff = await context.Staffs
            .FirstOrDefaultAsync(s => s.Id == request.StaffId && !s.IsDeleted, cancellationToken);

        if (staff is null)
            return Result<Unit>.Failure("Staff member not found.", 404);

        staff.SoftDelete();
        staff.UpdatedBy = currentUser.UserId;

        await context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}

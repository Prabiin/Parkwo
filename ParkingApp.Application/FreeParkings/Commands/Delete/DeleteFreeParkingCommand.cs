using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;

namespace ParkingApp.Application.FreeParkings.Commands.Delete;

/// <summary>
/// BackOffice-only: soft-deletes a free parking. The row is kept (IsDeleted)
/// so history survives, and riders stop seeing it immediately.
/// </summary>
public sealed record DeleteFreeParkingCommand(Guid FreeParkingFacilityId)
    : IRequestResult<DeleteFreeParkingCommand, Unit>;

public sealed class DeleteFreeParkingCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestResultHandler<DeleteFreeParkingCommand, Unit>
{
    public async Task<Result<Unit>> Handle(DeleteFreeParkingCommand request, CancellationToken cancellationToken = default)
    {
        var facility = await context.FreeParkingFacilities
            .FirstOrDefaultAsync(f => f.Id == request.FreeParkingFacilityId && !f.IsDeleted, cancellationToken);

        if (facility is null)
            return Result<Unit>.Failure("Free parking not found.", 404);

        facility.IsDeleted = true;
        facility.IsActive = false;
        facility.UpdatedAtUtc = DateTimeOffset.UtcNow;
        facility.UpdatedBy = currentUser.UserId;

        await context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}

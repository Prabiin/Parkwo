using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Facilities.Commands.UpdateCapacity;

/// <summary>
/// Owner updates claimed capacity. Live lots keep serving the CURRENT numbers
/// while the new set waits in the pending columns for compliance; lots that
/// are not yet Verified are updated directly (nothing live to protect).
/// Partial updates allowed — unspecified fields keep their current value.
/// </summary>
public sealed record UpdateFacilityCapacityCommand(
    Guid FacilityId,
    int? TwoWheelerOccupancy,
    int? FourWheelerOccupancy,
    decimal? LandAreaSqM)
    : IRequestResult<UpdateFacilityCapacityCommand, Unit>;

public sealed class UpdateFacilityCapacityCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestResultHandler<UpdateFacilityCapacityCommand, Unit>
{
    public async Task<Result<Unit>> Handle(
        UpdateFacilityCapacityCommand request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<Unit>.Failure("Authentication required.", 401);

        var facility = await context.ParkingFacilities
            .FirstOrDefaultAsync(f => f.Id == request.FacilityId, cancellationToken);

        if (facility is null)
            return Result<Unit>.Failure("Parking facility not found.", 404);

        if (!await ProviderOwnership.IsOwnerAsync(context, userId.Value, facility.ProviderId, cancellationToken))
            return Result<Unit>.Failure(
                "You must own the parking provider to update this facility.", 403);

        var nextTwoWheeler = request.TwoWheelerOccupancy
            ?? facility.PendingTwoWheelerOccupancy
            ?? facility.TwoWheelerOccupancy;
        var nextFourWheeler = request.FourWheelerOccupancy
            ?? facility.PendingFourWheelerOccupancy
            ?? facility.FourWheelerOccupancy;
        var nextLandArea = request.LandAreaSqM
            ?? facility.PendingLandAreaSqM
            ?? facility.LandAreaSqM;

        if (nextTwoWheeler + nextFourWheeler < 1)
            return Result<Unit>.Failure(
                "At least one two-wheeler or four-wheeler space is required.", 400);

        if (facility.ApprovalStatus == ApprovalStatusEnum.Verified)
        {
            facility.PendingTwoWheelerOccupancy = nextTwoWheeler;
            facility.PendingFourWheelerOccupancy = nextFourWheeler;
            facility.PendingLandAreaSqM = nextLandArea;
        }
        else
        {
            facility.TwoWheelerOccupancy = nextTwoWheeler;
            facility.FourWheelerOccupancy = nextFourWheeler;
            facility.LandAreaSqM = nextLandArea;
            facility.PendingTwoWheelerOccupancy = null;
            facility.PendingFourWheelerOccupancy = null;
            facility.PendingLandAreaSqM = null;
        }

        facility.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}

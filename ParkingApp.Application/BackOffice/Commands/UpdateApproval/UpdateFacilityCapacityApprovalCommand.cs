using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;

namespace ParkingApp.Application.BackOffice.Commands.UpdateApproval;

/// <summary>
/// Compliance decision on a Verified lot's pending capacity change.
/// Approve flips the pending numbers live; reject clears them with a reason
/// the owner sees in GET /facilities. The lot itself stays Verified throughout.
/// </summary>
public sealed record UpdateFacilityCapacityApprovalCommand(
    Guid FacilityId,
    bool Approve,
    string? RejectionReason)
    : IRequestResult<UpdateFacilityCapacityApprovalCommand, Unit>;

public sealed class UpdateFacilityCapacityApprovalCommandHandler(IApplicationDbContext context)
    : IRequestResultHandler<UpdateFacilityCapacityApprovalCommand, Unit>
{
    public async Task<Result<Unit>> Handle(
        UpdateFacilityCapacityApprovalCommand request,
        CancellationToken cancellationToken = default)
    {
        var facility = await context.ParkingFacilities
            .FirstOrDefaultAsync(f => f.Id == request.FacilityId, cancellationToken);

        if (facility is null)
            return Result<Unit>.Failure("Parking facility not found.", 404);

        if (facility.PendingTwoWheelerOccupancy is null
            && facility.PendingFourWheelerOccupancy is null
            && facility.PendingLandAreaSqM is null)
            return Result<Unit>.Failure("No pending capacity change for this facility.", 400);

        if (request.Approve)
        {
            if (facility.PendingTwoWheelerOccupancy.HasValue)
                facility.TwoWheelerOccupancy = facility.PendingTwoWheelerOccupancy.Value;
            if (facility.PendingFourWheelerOccupancy.HasValue)
                facility.FourWheelerOccupancy = facility.PendingFourWheelerOccupancy.Value;
            if (facility.PendingLandAreaSqM.HasValue)
                facility.LandAreaSqM = facility.PendingLandAreaSqM.Value;

            facility.PendingTwoWheelerOccupancy = null;
            facility.PendingFourWheelerOccupancy = null;
            facility.PendingLandAreaSqM = null;
            facility.RejectionReason = null;
        }
        else
        {
            facility.PendingTwoWheelerOccupancy = null;
            facility.PendingFourWheelerOccupancy = null;
            facility.PendingLandAreaSqM = null;
            facility.RejectionReason = request.RejectionReason!.Trim();
        }

        facility.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}

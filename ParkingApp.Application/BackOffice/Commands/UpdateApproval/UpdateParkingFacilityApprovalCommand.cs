using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.BackOffice.Commands.UpdateApproval;

public sealed record UpdateParkingFacilityApprovalCommand(
    Guid FacilityId,
    ApprovalStatusEnum ApprovalStatus,
    string? RejectionReason)
    : IRequestResult<UpdateParkingFacilityApprovalCommand, Unit>;

public sealed class UpdateParkingFacilityApprovalCommandHandler(IApplicationDbContext context)
    : IRequestResultHandler<UpdateParkingFacilityApprovalCommand, Unit>
{
    public async Task<Result<Unit>> Handle(
        UpdateParkingFacilityApprovalCommand request,
        CancellationToken cancellationToken = default)
    {
        var facility = await context.ParkingFacilities
            .FirstOrDefaultAsync(f => f.Id == request.FacilityId, cancellationToken);

        if (facility is null)
            return Result<Unit>.Failure("Parking facility not found.", 404);

        if (!ApprovalTransitions.IsAllowed(facility.ApprovalStatus, request.ApprovalStatus))
            return Result<Unit>.Failure(
                $"Cannot move facility from {facility.ApprovalStatus.ToDescription()} to {request.ApprovalStatus.ToDescription()}.", 400);

        facility.ApprovalStatus = request.ApprovalStatus;
        facility.RejectionReason = request.ApprovalStatus == ApprovalStatusEnum.Verified
            ? null
            : string.IsNullOrWhiteSpace(request.RejectionReason) ? null : request.RejectionReason.Trim();
        facility.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}

using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.BackOffice.Commands.UpdateApproval;

public sealed record UpdateDrivingLicenseApprovalCommand(
    Guid LicenseId,
    ApprovalStatusEnum ApprovalStatus,
    string? RejectionReason)
    : IRequestResult<UpdateDrivingLicenseApprovalCommand, Unit>;

public sealed class UpdateDrivingLicenseApprovalCommandHandler(IApplicationDbContext context)
    : IRequestResultHandler<UpdateDrivingLicenseApprovalCommand, Unit>
{
    public async Task<Result<Unit>> Handle(
        UpdateDrivingLicenseApprovalCommand request,
        CancellationToken cancellationToken = default)
    {
        var license = await context.DrivingLicenses
            .FirstOrDefaultAsync(d => d.Id == request.LicenseId, cancellationToken);

        if (license is null)
            return Result<Unit>.Failure("Driving license not found.", 404);

        if (!ApprovalTransitions.IsAllowed(license.ApprovalStatus, request.ApprovalStatus))
            return Result<Unit>.Failure(
                $"Cannot move license from {license.ApprovalStatus.ToDescription()} to {request.ApprovalStatus.ToDescription()}.", 400);

        license.ApplyReview(request.ApprovalStatus, request.RejectionReason);

        await context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}

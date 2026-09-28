using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.BackOffice.Commands.UpdateApproval;

public sealed record UpdateOrganizationApprovalCommand(
    Guid OrganizationId,
    ApprovalStatusEnum ApprovalStatus,
    string? RejectionReason)
    : IRequestResult<UpdateOrganizationApprovalCommand, Unit>;

public sealed class UpdateOrganizationApprovalCommandHandler(IApplicationDbContext context)
    : IRequestResultHandler<UpdateOrganizationApprovalCommand, Unit>
{
    public async Task<Result<Unit>> Handle(
        UpdateOrganizationApprovalCommand request,
        CancellationToken cancellationToken = default)
    {
        var organization = await context.Organizations
            .FirstOrDefaultAsync(o => o.Id == request.OrganizationId, cancellationToken);

        if (organization is null)
            return Result<Unit>.Failure("Organization not found.", 404);

        if (!ApprovalTransitions.IsAllowed(organization.ApprovalStatus, request.ApprovalStatus))
            return Result<Unit>.Failure(
                $"Cannot move organization from {organization.ApprovalStatus.ToDescription()} to {request.ApprovalStatus.ToDescription()}.", 400);

        organization.ApprovalStatus = request.ApprovalStatus;
        organization.RejectionReason = request.ApprovalStatus == ApprovalStatusEnum.Verified
            ? null
            : string.IsNullOrWhiteSpace(request.RejectionReason) ? null : request.RejectionReason.Trim();
        organization.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}

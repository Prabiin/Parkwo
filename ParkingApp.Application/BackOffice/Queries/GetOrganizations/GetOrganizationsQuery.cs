using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.BackOffice.Queries.GetOrganizations;

public sealed record GetOrganizationsQuery(ApprovalStatusEnum? ApprovalStatus)
    : IRequestResult<GetOrganizationsQuery, GetOrganizationsResponse>;

public sealed class GetOrganizationsQueryHandler(IApplicationDbContext context)
    : IRequestResultHandler<GetOrganizationsQuery, GetOrganizationsResponse>
{
    public async Task<Result<GetOrganizationsResponse>> Handle(
        GetOrganizationsQuery request,
        CancellationToken cancellationToken = default)
    {
        var query = context.Organizations.AsNoTracking();

        if (request.ApprovalStatus.HasValue)
        {
            var approvalStatus = request.ApprovalStatus.Value;
            query = query.Where(o => o.ApprovalStatus == approvalStatus);
        }

        var organizations = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Select(o => new
            {
                o.Id,
                o.Name,
                o.RegistrationNumber,
                o.ContactNumber,
                o.Address,
                o.ApprovalStatus,
                o.CreatedAtUtc,
                OwnerUserId = o.OwnerUserId,
                OwnerName = o.OwnerUser != null ? o.OwnerUser.FullName : null,
                OwnerPhoneNumber = o.OwnerUser != null ? o.OwnerUser.PhoneNumber : null
            })
            .ToListAsync(cancellationToken);

        var items = organizations
            .Select(o => new OrganizationItemResponse(
                o.Id,
                o.Name,
                o.RegistrationNumber,
                o.ContactNumber,
                o.Address,
                o.ApprovalStatus,
                o.ApprovalStatus.ToDescription(),
                o.CreatedAtUtc,
                o.OwnerUserId,
                o.OwnerName,
                o.OwnerPhoneNumber))
            .ToList();

        return Result<GetOrganizationsResponse>.Success(
            new GetOrganizationsResponse(items));
    }
}
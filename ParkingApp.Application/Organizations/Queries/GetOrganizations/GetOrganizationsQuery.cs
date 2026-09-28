using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Organizations.Queries.GetOrganizations;

public sealed record GetOrganizationsQuery() : IRequestResult<GetOrganizationsQuery, GetOrganizationsResponse>;

public sealed class GetOrganizationsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestResultHandler<GetOrganizationsQuery, GetOrganizationsResponse>
{
    public async Task<Result<GetOrganizationsResponse>> Handle(GetOrganizationsQuery request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<GetOrganizationsResponse>.Failure("Authentication required.", 401);

        var memberships = await context.UserOrganizations
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.Organization!.CreatedAtUtc)
            .Select(m => new
            {
                Organization = m.Organization!,
                m.Role
            })
            .ToListAsync(cancellationToken);

        var items = memberships
            .Select(m => new OrganizationItemResponse(
                m.Organization.Id,
                m.Organization.Name,
                m.Organization.RegistrationNumber,
                m.Organization.ContactNumber,
                m.Organization.Address,
                m.Organization.ApprovalStatus,
                m.Organization.ApprovalStatus.ToDescription(),
                m.Role,
                m.Role.ToDescription()))
            .ToList();

        return Result<GetOrganizationsResponse>.Success(
            new GetOrganizationsResponse(items));
    }
}
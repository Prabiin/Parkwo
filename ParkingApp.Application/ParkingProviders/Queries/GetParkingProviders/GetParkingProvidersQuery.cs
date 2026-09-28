using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.ParkingProviders.Queries.GetParkingProviders;

public sealed record GetParkingProvidersQuery() : IRequestResult<GetParkingProvidersQuery, GetParkingProvidersResponse>;

public sealed class GetParkingProvidersQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestResultHandler<GetParkingProvidersQuery, GetParkingProvidersResponse>
{
    public async Task<Result<GetParkingProvidersResponse>> Handle(GetParkingProvidersQuery request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<GetParkingProvidersResponse>.Failure("Authentication required.", 401);

        var organizationIds = await context.UserOrganizations
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => m.OrganizationId)
            .ToListAsync(cancellationToken);

        var providers = await context.ParkingProviders
            .AsNoTracking()
            .Where(p => p.OwnerUserId == userId
                        || (p.OwnerOrganizationId != null && organizationIds.Contains(p.OwnerOrganizationId.Value)))
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => new
            {
                p.Id,
                p.ProviderType,
                p.ApprovalStatus,
                p.OwnerUserId,
                p.OwnerOrganizationId
            })
            .ToListAsync(cancellationToken);

        var items = providers
            .Select(p => new ParkingProviderItemResponse(
                p.Id,
                p.ProviderType,
                p.ProviderType.ToDescription(),
                p.ApprovalStatus,
                p.ApprovalStatus.ToDescription(),
                p.OwnerUserId,
                p.OwnerOrganizationId))
            .ToList();

        return Result<GetParkingProvidersResponse>.Success(
            new GetParkingProvidersResponse(items));
    }
}
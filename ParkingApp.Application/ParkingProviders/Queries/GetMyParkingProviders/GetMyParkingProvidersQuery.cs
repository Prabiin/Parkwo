using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.ParkingProviders.Queries.GetMyParkingProviders;

public sealed record GetMyParkingProvidersQuery() : IRequestResult<GetMyParkingProvidersQuery, GetMyParkingProvidersResponse>;

public sealed class GetMyParkingProvidersQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestResultHandler<GetMyParkingProvidersQuery, GetMyParkingProvidersResponse>
{
    public async Task<Result<GetMyParkingProvidersResponse>> Handle(GetMyParkingProvidersQuery request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<GetMyParkingProvidersResponse>.Failure("Authentication required.", 401);

        var myOrganizationIds = await context.UserOrganizations
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => m.OrganizationId)
            .ToListAsync(cancellationToken);

        var providers = await context.ParkingProviders
            .AsNoTracking()
            .Where(p => p.OwnerUserId == userId
                        || (p.OwnerOrganizationId != null && myOrganizationIds.Contains(p.OwnerOrganizationId.Value)))
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
            .Select(p => new MyParkingProviderItemResponse(
                p.Id,
                p.ProviderType,
                p.ProviderType.ToDescription(),
                p.ApprovalStatus,
                p.ApprovalStatus.ToDescription(),
                p.OwnerUserId,
                p.OwnerOrganizationId))
            .ToList();

        return Result<GetMyParkingProvidersResponse>.Success(
            new GetMyParkingProvidersResponse(items));
    }
}
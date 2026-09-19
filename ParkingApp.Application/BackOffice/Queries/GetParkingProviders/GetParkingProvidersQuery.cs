using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.BackOffice.Queries.GetParkingProviders;

public sealed record GetParkingProvidersQuery(ApprovalStatusEnum? ApprovalStatus)
    : IRequestResult<GetParkingProvidersQuery, GetParkingProvidersResponse>;

public sealed class GetParkingProvidersQueryHandler(IApplicationDbContext context)
    : IRequestResultHandler<GetParkingProvidersQuery, GetParkingProvidersResponse>
{
    public async Task<Result<GetParkingProvidersResponse>> Handle(
        GetParkingProvidersQuery request,
        CancellationToken cancellationToken = default)
    {
        var query = context.ParkingProviders.AsNoTracking();

        if (request.ApprovalStatus.HasValue)
        {
            var approvalStatus = request.ApprovalStatus.Value;
            query = query.Where(p => p.ApprovalStatus == approvalStatus);
        }

        var providers = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => new
            {
                p.Id,
                p.ProviderType,
                p.ApprovalStatus,
                p.CreatedAtUtc,
                p.OwnerUserId,
                p.OwnerOrganizationId,
                OwnerName = p.OwnerUserId != null
                    ? p.OwnerUser!.FullName
                    : (p.OwnerOrganization != null ? p.OwnerOrganization.Name : null),
                OwnerContactNumber = p.OwnerUserId != null
                    ? p.OwnerUser!.PhoneNumber
                    : (p.OwnerOrganization != null ? p.OwnerOrganization.ContactNumber : null),
                OwnerEmail = p.OwnerUser != null ? p.OwnerUser.Email : null
            })
            .ToListAsync(cancellationToken);

        var items = providers
            .Select(p => new ParkingProviderItemResponse(
                p.Id,
                p.ProviderType,
                p.ProviderType.ToDescription(),
                p.ApprovalStatus,
                p.ApprovalStatus.ToDescription(),
                p.CreatedAtUtc,
                p.OwnerUserId,
                p.OwnerOrganizationId,
                p.OwnerName,
                p.OwnerContactNumber,
                p.OwnerEmail))
            .ToList();

        return Result<GetParkingProvidersResponse>.Success(
            new GetParkingProvidersResponse(items));
    }
}
using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Facilities.Queries.GetParkingFacilities;

public sealed record GetParkingFacilitiesQuery() : IRequestResult<GetParkingFacilitiesQuery, GetParkingFacilitiesResponse>;

public sealed class GetParkingFacilitiesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestResultHandler<GetParkingFacilitiesQuery, GetParkingFacilitiesResponse>
{
    public async Task<Result<GetParkingFacilitiesResponse>> Handle(GetParkingFacilitiesQuery request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<GetParkingFacilitiesResponse>.Failure("Authentication required.", 401);

        var providerIds = await context.ParkingProviders
            .AsNoTracking()
            .Where(p => p.OwnerUserId == userId
                        || (p.OwnerOrganizationId != null
                            && context.UserOrganizations.Any(m =>
                                m.OrganizationId == p.OwnerOrganizationId
                                && m.UserId == userId)))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var facilities = await context.ParkingFacilities
            .AsNoTracking()
            .Where(f => providerIds.Contains(f.ProviderId))
            .Select(f => new
            {
                f.Id,
                f.ProviderId,
                f.Name,
                f.Description,
                f.Address,
                f.Latitude,
                f.Longitude,
                f.ApprovalStatus,
                f.CreatedAtUtc,
                TwoWheelerCount = f.Spots.Count(s => s.VehicleType == VehicleTypeEnum.TwoWheeler),
                FourWheelerCount = f.Spots.Count(s => s.VehicleType == VehicleTypeEnum.FourWheeler),
                ImageCount = f.Images.Count,
                f.AverageRating,
                f.RatingCount,
                f.HasMarkedParkingLot,
                f.RejectionReason
            })
            .OrderByDescending(f => f.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var items = facilities
            .Select(f => new ParkingFacilityItemResponse(
                f.Id,
                f.ProviderId,
                f.Name,
                f.Description,
                f.Address,
                f.Latitude,
                f.Longitude,
                f.ApprovalStatus,
                f.ApprovalStatus.ToDescription(),
                f.CreatedAtUtc,
                f.TwoWheelerCount,
                f.FourWheelerCount,
                f.ImageCount,
                f.AverageRating,
                f.RatingCount,
                f.HasMarkedParkingLot,
                f.RejectionReason))
            .ToList();

        return Result<GetParkingFacilitiesResponse>.Success(
            new GetParkingFacilitiesResponse(items));
    }
}
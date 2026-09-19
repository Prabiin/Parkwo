using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Facilities.Queries.GetMyParkingFacilities;

public sealed record GetMyParkingFacilitiesQuery() : IRequestResult<GetMyParkingFacilitiesQuery, GetMyParkingFacilitiesResponse>;

public sealed class GetMyParkingFacilitiesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestResultHandler<GetMyParkingFacilitiesQuery, GetMyParkingFacilitiesResponse>
{
    public async Task<Result<GetMyParkingFacilitiesResponse>> Handle(GetMyParkingFacilitiesQuery request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<GetMyParkingFacilitiesResponse>.Failure("Authentication required.", 401);

        var myProviderIds = await context.ParkingProviders
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
            .Where(f => myProviderIds.Contains(f.ProviderId))
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
                f.RatingCount
            })
            .OrderByDescending(f => f.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var items = facilities
            .Select(f => new MyParkingFacilityItemResponse(
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
                f.RatingCount))
            .ToList();

        return Result<GetMyParkingFacilitiesResponse>.Success(
            new GetMyParkingFacilitiesResponse(items));
    }
}
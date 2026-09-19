using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.BackOffice.Queries.GetParkingFacilities;

public sealed record GetParkingFacilitiesQuery(ApprovalStatusEnum? ApprovalStatus)
    : IRequestResult<GetParkingFacilitiesQuery, GetParkingFacilitiesResponse>;

public sealed class GetParkingFacilitiesQueryHandler(IApplicationDbContext context)
    : IRequestResultHandler<GetParkingFacilitiesQuery, GetParkingFacilitiesResponse>
{
    public async Task<Result<GetParkingFacilitiesResponse>> Handle(
        GetParkingFacilitiesQuery request,
        CancellationToken cancellationToken = default)
    {
        var query = context.ParkingFacilities.AsNoTracking();

        if (request.ApprovalStatus.HasValue)
        {
            var approvalStatus = request.ApprovalStatus.Value;
            query = query.Where(f => f.ApprovalStatus == approvalStatus);
        }

        var facilities = await query
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
                ProviderOwnerName = f.Provider!.OwnerUserId != null
                    ? f.Provider.OwnerUser!.FullName
                    : (f.Provider.OwnerOrganization != null ? f.Provider.OwnerOrganization.Name : null),
                ProviderOwnerContactNumber = f.Provider!.OwnerUserId != null
                    ? f.Provider.OwnerUser!.PhoneNumber
                    : (f.Provider.OwnerOrganization != null ? f.Provider.OwnerOrganization.ContactNumber : null),
                TwoWheelerCount = f.Spots.Count(s => s.VehicleType == VehicleTypeEnum.TwoWheeler),
                FourWheelerCount = f.Spots.Count(s => s.VehicleType == VehicleTypeEnum.FourWheeler),
                ImageCount = f.Images.Count,
                f.AverageRating,
                f.RatingCount
            })
            .OrderByDescending(f => f.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var items = facilities
            .Select(f => new BackOfficeParkingFacilityItemResponse(
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
                f.ProviderOwnerName,
                f.ProviderOwnerContactNumber,
                f.TwoWheelerCount,
                f.FourWheelerCount,
                f.ImageCount,
                f.AverageRating,
                f.RatingCount))
            .ToList();

        return Result<GetParkingFacilitiesResponse>.Success(
            new GetParkingFacilitiesResponse(items));
    }
}
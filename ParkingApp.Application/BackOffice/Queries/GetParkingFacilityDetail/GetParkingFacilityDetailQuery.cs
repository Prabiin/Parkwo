using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;
using ParkingApp.Application.Facilities;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.BackOffice.Queries.GetParkingFacilityDetail;

public sealed record GetParkingFacilityDetailQuery(Guid FacilityId)
    : IRequestResult<GetParkingFacilityDetailQuery, GetParkingFacilityDetailResponse>;

public sealed class GetParkingFacilityDetailQueryHandler(
    IApplicationDbContext context,
    ParkingStandards standards)
    : IRequestResultHandler<GetParkingFacilityDetailQuery, GetParkingFacilityDetailResponse>
{
    public async Task<Result<GetParkingFacilityDetailResponse>> Handle(
        GetParkingFacilityDetailQuery request,
        CancellationToken cancellationToken = default)
    {
        var facility = await context.ParkingFacilities
            .AsNoTracking()
            .Where(f => f.Id == request.FacilityId)
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
                f.AverageRating,
                f.RatingCount,
                f.HasMarkedParkingLot,
                f.RejectionReason,
                f.TwoWheelerOccupancy,
                f.FourWheelerOccupancy,
                f.LandAreaSqM,
                f.PendingTwoWheelerOccupancy,
                f.PendingFourWheelerOccupancy,
                f.PendingLandAreaSqM
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (facility is null)
            return Result<GetParkingFacilityDetailResponse>.Failure("Parking facility not found.", 404);

        var images = await context.ParkingFacilityImages
            .AsNoTracking()
            .Where(i => i.FacilityId == request.FacilityId)
            .OrderBy(i => i.SortOrder)
            .Select(i => new
            {
                i.Id,
                i.Url,
                i.FileName,
                i.ContentType,
                i.SizeInBytes,
                i.SortOrder
            })
            .ToListAsync(cancellationToken);

        var imageItems = images
            .Select(i => new ParkingFacilityImageResponse(
                i.Id,
                i.Url,
                i.FileName,
                i.ContentType,
                i.SizeInBytes,
                i.SortOrder))
            .ToList();

        return Result<GetParkingFacilityDetailResponse>.Success(
            new GetParkingFacilityDetailResponse(
                facility.Id,
                facility.ProviderId,
                facility.Name,
                facility.Description,
                facility.Address,
                facility.Latitude,
                facility.Longitude,
                facility.ApprovalStatus,
                facility.ApprovalStatus.ToDescription(),
                facility.CreatedAtUtc,
                facility.ProviderOwnerName,
                facility.ProviderOwnerContactNumber,
                facility.TwoWheelerOccupancy,
                facility.FourWheelerOccupancy,
                facility.LandAreaSqM,
                facility.PendingTwoWheelerOccupancy,
                facility.PendingFourWheelerOccupancy,
                facility.PendingLandAreaSqM,
                ParkingCapacity.EstimatedAreaRequiredSqM(
                    facility.TwoWheelerOccupancy,
                    facility.FourWheelerOccupancy,
                    standards),
                ParkingCapacity.ExceedsLandArea(
                    facility.TwoWheelerOccupancy,
                    facility.FourWheelerOccupancy,
                    facility.LandAreaSqM,
                    standards),
                imageItems,
                facility.AverageRating,
                facility.RatingCount,
                facility.HasMarkedParkingLot,
                facility.RejectionReason));
    }
}
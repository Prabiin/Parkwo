using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Facilities.Queries.GetParkingFacilityById;

public sealed record GetParkingFacilityByIdQuery(Guid FacilityId)
    : IRequestResult<GetParkingFacilityByIdQuery, GetParkingFacilityByIdResponse>;

public sealed class GetParkingFacilityByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestResultHandler<GetParkingFacilityByIdQuery, GetParkingFacilityByIdResponse>
{
    public async Task<Result<GetParkingFacilityByIdResponse>> Handle(GetParkingFacilityByIdQuery request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<GetParkingFacilityByIdResponse>.Failure("Authentication required.", 401);

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
                f.AverageRating,
                f.RatingCount,
                f.HasMarkedParkingLot,
                f.RejectionReason,
                f.TwoWheelerOccupancy,
                f.FourWheelerOccupancy,
                f.LandAreaSqM,
                f.TwoWheelerPricePerHourNpr,
                f.FourWheelerPricePerHourNpr,
                f.PendingTwoWheelerOccupancy,
                f.PendingFourWheelerOccupancy,
                f.PendingLandAreaSqM
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (facility is null)
            return Result<GetParkingFacilityByIdResponse>.Failure("Parking facility not found.", 404);

        if (!await ProviderOwnership.IsOwnerAsync(context, userId.Value, facility.ProviderId, cancellationToken))
            return Result<GetParkingFacilityByIdResponse>.Failure(
                "You must own the parking provider to view this facility.", 403);

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

        return Result<GetParkingFacilityByIdResponse>.Success(
            new GetParkingFacilityByIdResponse(
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
                facility.TwoWheelerOccupancy,
                facility.FourWheelerOccupancy,
                facility.LandAreaSqM,
                facility.TwoWheelerPricePerHourNpr,
                facility.FourWheelerPricePerHourNpr,
                facility.PendingTwoWheelerOccupancy,
                facility.PendingFourWheelerOccupancy,
                facility.PendingLandAreaSqM,
                imageItems,
                facility.AverageRating,
                facility.RatingCount,
                facility.HasMarkedParkingLot,
                facility.RejectionReason));
    }
}
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
                f.RejectionReason
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (facility is null)
            return Result<GetParkingFacilityByIdResponse>.Failure("Parking facility not found.", 404);

        if (!await ProviderOwnership.IsOwnerAsync(context, userId.Value, facility.ProviderId, cancellationToken))
            return Result<GetParkingFacilityByIdResponse>.Failure(
                "You must own the parking provider to view this facility.", 403);

        var spots = await context.ParkingSpots
            .AsNoTracking()
            .Where(s => s.FacilityId == request.FacilityId)
            .OrderBy(s => s.SpotNumber)
            .Select(s => new { s.Id, s.SpotNumber, s.VehicleType, s.PricePerHourNpr, s.IsActive })
            .ToListAsync(cancellationToken);

        var items = spots
            .Select(s => new ParkingSpotItemResponse(
                s.Id,
                s.SpotNumber,
                s.VehicleType,
                s.VehicleType.ToDescription(),
                s.PricePerHourNpr,
                s.IsActive))
            .ToList();

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
                items,
                items.Count(s => s.VehicleType == VehicleTypeEnum.TwoWheeler),
                items.Count(s => s.VehicleType == VehicleTypeEnum.FourWheeler),
                imageItems,
                facility.AverageRating,
                facility.RatingCount,
                facility.HasMarkedParkingLot,
                facility.RejectionReason));
    }
}
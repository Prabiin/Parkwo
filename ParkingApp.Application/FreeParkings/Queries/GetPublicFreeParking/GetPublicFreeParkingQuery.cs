using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;

namespace ParkingApp.Application.FreeParkings.Queries.GetPublicFreeParking;

/// <summary>
/// Rider-facing detail for the marker's "tap to view" screen. Only published,
/// non-deleted free parkings are visible; anything else answers 404 as if it
/// never existed.
/// </summary>
public sealed record GetPublicFreeParkingQuery(Guid FreeParkingFacilityId)
    : IRequestResult<GetPublicFreeParkingQuery, GetPublicFreeParkingResponse>;

public sealed class GetPublicFreeParkingQueryHandler(IApplicationDbContext context, IImageUrlResolver imageUrls)
    : IRequestResultHandler<GetPublicFreeParkingQuery, GetPublicFreeParkingResponse>
{
    public async Task<Result<GetPublicFreeParkingResponse>> Handle(
        GetPublicFreeParkingQuery request,
        CancellationToken cancellationToken = default)
    {
        var facility = await context.FreeParkingFacilities
            .AsNoTracking()
            .Where(f => f.Id == request.FreeParkingFacilityId && f.IsActive && !f.IsDeleted)
            .Select(f => new
            {
                f.Id,
                f.Name,
                f.Description,
                f.Address,
                f.Latitude,
                f.Longitude,
                f.TwoWheelerOccupancy,
                f.FourWheelerOccupancy
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (facility is null)
            return Result<GetPublicFreeParkingResponse>.Failure("Free parking not found.", 404);

        var images = await context.FreeParkingFacilityImages
            .AsNoTracking()
            .Where(i => i.FreeParkingFacilityId == request.FreeParkingFacilityId)
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
            .Select(i => new FreeParkingImageResponse(
                i.Id,
                imageUrls.Resolve(i.Url)!,
                i.FileName,
                i.ContentType,
                i.SizeInBytes,
                i.SortOrder))
            .ToList();

        return Result<GetPublicFreeParkingResponse>.Success(
            new GetPublicFreeParkingResponse(
                facility.Id,
                facility.Name,
                facility.Description,
                facility.Address,
                facility.Latitude,
                facility.Longitude,
                facility.TwoWheelerOccupancy,
                facility.FourWheelerOccupancy,
                imageItems));
    }
}

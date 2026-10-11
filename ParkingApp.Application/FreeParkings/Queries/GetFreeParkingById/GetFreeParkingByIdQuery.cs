using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;

namespace ParkingApp.Application.FreeParkings.Queries.GetFreeParkingById;

/// <summary>
/// BackOffice detail. Both active and inactive rows are visible, but a
/// soft-deleted row is not surfaced anywhere — it answers 404 like a row that
/// never existed.
/// </summary>
public sealed record GetFreeParkingByIdQuery(Guid FreeParkingFacilityId)
    : IRequestResult<GetFreeParkingByIdQuery, GetFreeParkingByIdResponse>;

public sealed class GetFreeParkingByIdQueryHandler(IApplicationDbContext context, IImageUrlResolver imageUrls)
    : IRequestResultHandler<GetFreeParkingByIdQuery, GetFreeParkingByIdResponse>
{
    public async Task<Result<GetFreeParkingByIdResponse>> Handle(
        GetFreeParkingByIdQuery request,
        CancellationToken cancellationToken = default)
    {
        var facility = await context.FreeParkingFacilities
            .AsNoTracking()
            .Where(f => f.Id == request.FreeParkingFacilityId && !f.IsDeleted)
            .Select(f => new
            {
                f.Id,
                f.Name,
                f.Description,
                f.Address,
                f.Latitude,
                f.Longitude,
                f.TwoWheelerOccupancy,
                f.FourWheelerOccupancy,
                f.IsActive,
                f.CreatedAtUtc,
                f.UpdatedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (facility is null)
            return Result<GetFreeParkingByIdResponse>.Failure("Free parking not found.", 404);

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

        return Result<GetFreeParkingByIdResponse>.Success(
            new GetFreeParkingByIdResponse(
                facility.Id,
                facility.Name,
                facility.Description,
                facility.Address,
                facility.Latitude,
                facility.Longitude,
                facility.TwoWheelerOccupancy,
                facility.FourWheelerOccupancy,
                facility.IsActive,
                facility.CreatedAtUtc,
                facility.UpdatedAtUtc,
                imageItems));
    }
}

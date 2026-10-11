using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;

namespace ParkingApp.Application.FreeParkings.Queries.GetFreeParkings;

/// <summary>
/// BackOffice list of free parkings, ordered by name. Soft-deleted rows are
/// never returned — not even here — so a row that is absent either does not
/// exist or is deleted (recoverable only by a manual data patch). By default
/// every non-deleted row comes back; <c>name</c> filters by a case-insensitive
/// partial match and <c>isActive</c> (true = Active, false = Inactive) narrows
/// by published state. Every filter is optional and they compose.
/// </summary>
public sealed record GetFreeParkingsQuery(string? Name, bool? IsActive)
    : IRequestResult<GetFreeParkingsQuery, GetFreeParkingsResponse>;

public sealed class GetFreeParkingsQueryHandler(IApplicationDbContext context, IImageUrlResolver imageUrls)
    : IRequestResultHandler<GetFreeParkingsQuery, GetFreeParkingsResponse>
{
    public async Task<Result<GetFreeParkingsResponse>> Handle(
        GetFreeParkingsQuery request,
        CancellationToken cancellationToken = default)
    {
        var query = context.FreeParkingFacilities
            .AsNoTracking()
            .Where(f => !f.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var term = request.Name.Trim().ToLower();
            query = query.Where(f => f.Name.ToLower().Contains(term));
        }

        if (request.IsActive.HasValue)
        {
            var isActive = request.IsActive.Value;
            query = query.Where(f => f.IsActive == isActive);
        }

        var rows = await query
            .OrderBy(f => f.Name)
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
                f.UpdatedAtUtc,
                ImageCount = f.Images.Count,
                FirstImageUrl = f.Images
                    .OrderBy(i => i.SortOrder)
                    .Select(i => i.Url)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(f => new FreeParkingItemResponse(
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
                f.UpdatedAtUtc,
                f.ImageCount,
                imageUrls.Resolve(f.FirstImageUrl)))
            .ToList();

        return Result<GetFreeParkingsResponse>.Success(
            new GetFreeParkingsResponse(items));
    }
}

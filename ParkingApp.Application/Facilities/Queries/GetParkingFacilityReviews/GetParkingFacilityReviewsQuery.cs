using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;

namespace ParkingApp.Application.Facilities.Queries.GetParkingFacilityReviews;

public sealed record GetParkingFacilityReviewsQuery(Guid FacilityId)
    : IRequestResult<GetParkingFacilityReviewsQuery, GetParkingFacilityReviewsResponse>;

public sealed class GetParkingFacilityReviewsQueryHandler(IApplicationDbContext context)
    : IRequestResultHandler<GetParkingFacilityReviewsQuery, GetParkingFacilityReviewsResponse>
{
    public async Task<Result<GetParkingFacilityReviewsResponse>> Handle(
        GetParkingFacilityReviewsQuery request,
        CancellationToken cancellationToken = default)
    {
        var facility = await context.ParkingFacilities
            .AsNoTracking()
            .Where(f => f.Id == request.FacilityId)
            .Select(f => new { f.AverageRating, f.RatingCount })
            .FirstOrDefaultAsync(cancellationToken);

        if (facility is null)
            return Result<GetParkingFacilityReviewsResponse>.Failure("Parking facility not found.", 404);

        var reviews = await context.ParkingFacilityReviews
            .AsNoTracking()
            .Where(r => r.FacilityId == request.FacilityId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new
            {
                r.Id,
                r.Rating,
                r.Comment,
                r.AuthorId,
                AuthorFullName = r.Author != null ? r.Author.FullName : null,
                r.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var items = reviews
            .Select(r => new ParkingFacilityReviewItemResponse(
                r.Id,
                r.Rating,
                r.Comment,
                r.AuthorId,
                r.AuthorFullName,
                r.CreatedAtUtc))
            .ToList();

        return Result<GetParkingFacilityReviewsResponse>.Success(
            new GetParkingFacilityReviewsResponse(
                request.FacilityId,
                facility.AverageRating,
                facility.RatingCount,
                items));
    }
}
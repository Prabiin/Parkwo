using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.SavedParkings.Queries.GetSavedParkings;

public sealed record GetSavedParkingsQuery
    : IRequestResult<GetSavedParkingsQuery, GetSavedParkingsResponse>;

/// <param name="IsFree">True for a Parkwo free parking (green card, Navigate only).</param>
/// <param name="IsAvailable">
/// False when the place can no longer be used: a paid lot that lost its
/// verification, or a free parking that was unpublished. Kept in the list so
/// the rider can remove it, but the app should not offer Book or Navigate.
/// </param>
/// <param name="TwoWheelerPricePerHourNpr">Null for free parkings and for lots with no space of that type.</param>
public sealed record SavedParkingItemResponse(
    Guid SavedId,
    bool IsFree,
    Guid? FacilityId,
    Guid? FreeParkingId,
    string Name,
    string Address,
    double? Latitude,
    double? Longitude,
    string? CoverImageUrl,
    double? AverageRating,
    int ReviewCount,
    int TwoWheelerOccupancy,
    int FourWheelerOccupancy,
    decimal? TwoWheelerPricePerHourNpr,
    decimal? FourWheelerPricePerHourNpr,
    bool IsAvailable,
    DateTimeOffset SavedAtUtc);

public sealed record GetSavedParkingsResponse(IReadOnlyList<SavedParkingItemResponse> Saved);

public sealed class GetSavedParkingsQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IImageUrlResolver imageUrls,
    IOptions<ParkwoPricingSettings> pricingSettings)
    : IRequestResultHandler<GetSavedParkingsQuery, GetSavedParkingsResponse>
{
    public async Task<Result<GetSavedParkingsResponse>> Handle(
        GetSavedParkingsQuery request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<GetSavedParkingsResponse>.Failure("Authentication required.", 401);

        var rows = await context.SavedParkings
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .Select(s => new
            {
                s.Id,
                s.CreatedAtUtc,
                s.FacilityId,
                s.FreeParkingFacilityId,
                Paid = s.Facility == null ? null : new
                {
                    s.Facility.Name,
                    s.Facility.Address,
                    s.Facility.Latitude,
                    s.Facility.Longitude,
                    s.Facility.ApprovalStatus,
                    s.Facility.TwoWheelerOccupancy,
                    s.Facility.FourWheelerOccupancy,
                    Cover = s.Facility.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                    Rating = s.Facility.Reviews.Average(r => (double?)r.Rating),
                    ReviewCount = s.Facility.Reviews.Count()
                },
                Free = s.FreeParkingFacility == null ? null : new
                {
                    s.FreeParkingFacility.Name,
                    s.FreeParkingFacility.Address,
                    s.FreeParkingFacility.Latitude,
                    s.FreeParkingFacility.Longitude,
                    s.FreeParkingFacility.IsActive,
                    s.FreeParkingFacility.IsDeleted,
                    s.FreeParkingFacility.TwoWheelerOccupancy,
                    s.FreeParkingFacility.FourWheelerOccupancy,
                    Cover = s.FreeParkingFacility.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault()
                }
            })
            .ToListAsync(cancellationToken);

        var pricing = pricingSettings.Value;
        var items = new List<SavedParkingItemResponse>(rows.Count);

        foreach (var r in rows)
        {
            if (r.Paid is { } p)
            {
                items.Add(new SavedParkingItemResponse(
                    r.Id, false, r.FacilityId, null,
                    p.Name, p.Address, p.Latitude, p.Longitude,
                    imageUrls.Resolve(p.Cover),
                    p.Rating is { } avg ? Math.Round(avg, 1) : null,
                    p.ReviewCount,
                    p.TwoWheelerOccupancy,
                    p.FourWheelerOccupancy,
                    p.TwoWheelerOccupancy > 0 ? pricing.TwoWheelerPricePerHourNpr : null,
                    p.FourWheelerOccupancy > 0 ? pricing.FourWheelerPricePerHourNpr : null,
                    p.ApprovalStatus == ApprovalStatusEnum.Verified,
                    r.CreatedAtUtc));
            }
            else if (r.Free is { } f)
            {
                // A deleted free parking surfaces nowhere, saved list included.
                if (f.IsDeleted)
                    continue;

                items.Add(new SavedParkingItemResponse(
                    r.Id, true, null, r.FreeParkingFacilityId,
                    f.Name, f.Address, f.Latitude, f.Longitude,
                    imageUrls.Resolve(f.Cover),
                    null, 0,
                    f.TwoWheelerOccupancy ?? 0,
                    f.FourWheelerOccupancy ?? 0,
                    null, null,
                    f.IsActive,
                    r.CreatedAtUtc));
            }
        }

        return Result<GetSavedParkingsResponse>.Success(new GetSavedParkingsResponse(items));
    }
}

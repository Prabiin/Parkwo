namespace ParkingApp.Application.Facilities.Queries.GetParkingFacilityReviews;

public record ParkingFacilityReviewItemResponse(
    Guid Id,
    int Rating,
    string? Comment,
    Guid AuthorId,
    string? AuthorFullName,
    DateTimeOffset CreatedAtUtc);

public record GetParkingFacilityReviewsResponse(
    Guid FacilityId,
    double? AverageRating,
    int RatingCount,
    IReadOnlyList<ParkingFacilityReviewItemResponse> Reviews);
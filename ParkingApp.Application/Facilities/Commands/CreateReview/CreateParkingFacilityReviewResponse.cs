namespace ParkingApp.Application.Facilities.Commands.CreateReview;

public record CreateParkingFacilityReviewResponse(
    Guid Id,
    Guid FacilityId,
    int Rating,
    string? Comment,
    Guid AuthorId,
    DateTimeOffset CreatedAtUtc,
    double? AverageRating,
    int RatingCount);
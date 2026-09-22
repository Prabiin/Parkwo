using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Facilities.Commands.CreateReview;

public sealed record CreateParkingFacilityReviewCommand(
    Guid FacilityId,
    int Rating,
    string? Comment)
    : IRequestResult<CreateParkingFacilityReviewCommand, Guid>;

public sealed class CreateParkingFacilityReviewCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestResultHandler<CreateParkingFacilityReviewCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateParkingFacilityReviewCommand request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<Guid>.Failure("Authentication required.", 401);

        var facility = await context.ParkingFacilities
            .FirstOrDefaultAsync(f => f.Id == request.FacilityId, cancellationToken);

        if (facility is null)
            return Result<Guid>.Failure("Parking facility not found.", 404);

        if (facility.ApprovalStatus != ApprovalStatusEnum.Verified)
            return Result<Guid>.Failure(
                "This facility must be verified before it can be reviewed.");

        // TODO(bookings): require a completed booking/payment for this facility + author,
        // so only riders who actually used it can review. (A dedicated visit table was
        // removed as redundant — bookings are the stronger proof.)

        var alreadyReviewed = await context.ParkingFacilityReviews
            .AnyAsync(r => r.FacilityId == request.FacilityId && r.AuthorId == userId, cancellationToken);

        if (alreadyReviewed)
            return Result<Guid>.Failure(
                "You have already reviewed this facility.", 409);

        var review = new ParkingFacilityReview
        {
            FacilityId = request.FacilityId,
            AuthorId = userId.Value,
            Rating = request.Rating,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var previousCount = facility.RatingCount;
        var newCount = previousCount + 1;
        var previousAverage = facility.AverageRating ?? 0;
        var newAverage = Math.Round((previousAverage * previousCount + request.Rating) / newCount, 1);

        facility.AverageRating = newAverage;
        facility.RatingCount = newCount;

        context.ParkingFacilityReviews.Add(review);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(review.Id);
    }
}
using FluentValidation;

namespace ParkingApp.Application.Facilities.Queries.GetParkingFacilityReviews;

public sealed class GetParkingFacilityReviewsQueryValidator : AbstractValidator<GetParkingFacilityReviewsQuery>
{
    public GetParkingFacilityReviewsQueryValidator()
    {
        RuleFor(x => x.FacilityId)
            .NotEmpty();
    }
}
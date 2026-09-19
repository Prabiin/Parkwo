using FluentValidation;

namespace ParkingApp.Application.Facilities.Commands.CreateReview;

public sealed class CreateParkingFacilityReviewCommandValidator : AbstractValidator<CreateParkingFacilityReviewCommand>
{
    public CreateParkingFacilityReviewCommandValidator()
    {
        RuleFor(x => x.FacilityId)
            .NotEmpty();

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5);

        RuleFor(x => x.Comment)
            .MaximumLength(1000);
    }
}
using FluentValidation;

namespace ParkingApp.Application.FreeParkings.Commands.Update;

public sealed class UpdateFreeParkingCommandValidator : AbstractValidator<UpdateFreeParkingCommand>
{
    public UpdateFreeParkingCommandValidator()
    {
        RuleFor(x => x.FreeParkingFacilityId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(1000);

        RuleFor(x => x.Address)
            .NotEmpty()
            .MaximumLength(300);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90d, 90d);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180d, 180d);

        RuleFor(x => x.TwoWheelerOccupancy)
            .GreaterThanOrEqualTo(0)
            .When(x => x.TwoWheelerOccupancy.HasValue);

        RuleFor(x => x.FourWheelerOccupancy)
            .GreaterThanOrEqualTo(0)
            .When(x => x.FourWheelerOccupancy.HasValue);
    }
}

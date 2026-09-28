using FluentValidation;

namespace ParkingApp.Application.Facilities.Commands.Create;

public sealed class CreateParkingFacilityCommandValidator : AbstractValidator<CreateParkingFacilityCommand>
{
    public CreateParkingFacilityCommandValidator()
    {
        RuleFor(x => x.ProviderId)
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
            .InclusiveBetween(-90d, 90d)
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180d, 180d)
            .When(x => x.Longitude.HasValue);

        RuleFor(x => x.TwoWheelerOccupancy)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.FourWheelerOccupancy)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x)
            .Must(x => x.TwoWheelerOccupancy + x.FourWheelerOccupancy >= 1)
            .WithMessage("At least one two-wheeler or four-wheeler space is required.")
            .OverridePropertyName(nameof(CreateParkingFacilityCommand.TwoWheelerOccupancy));

        RuleFor(x => x.LandAreaSqM)
            .GreaterThan(0)
            .When(x => x.LandAreaSqM.HasValue);

        RuleFor(x => x.TwoWheelerPricePerHourNpr)
            .GreaterThan(0);

        RuleFor(x => x.FourWheelerPricePerHourNpr)
            .GreaterThan(0);
    }
}
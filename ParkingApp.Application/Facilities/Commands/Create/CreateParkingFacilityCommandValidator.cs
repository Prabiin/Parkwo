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
    }
}
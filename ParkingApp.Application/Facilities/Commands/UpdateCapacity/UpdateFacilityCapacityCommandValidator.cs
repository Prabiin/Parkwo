using FluentValidation;

namespace ParkingApp.Application.Facilities.Commands.UpdateCapacity;

public sealed class UpdateFacilityCapacityCommandValidator : AbstractValidator<UpdateFacilityCapacityCommand>
{
    public UpdateFacilityCapacityCommandValidator()
    {
        RuleFor(x => x.FacilityId)
            .NotEmpty();

        RuleFor(x => x)
            .Must(x => x.TwoWheelerOccupancy.HasValue
                       || x.FourWheelerOccupancy.HasValue
                       || x.LandAreaSqM.HasValue)
            .WithMessage("At least one of twoWheelerOccupancy, fourWheelerOccupancy, landAreaSqM is required.")
            .OverridePropertyName(nameof(UpdateFacilityCapacityCommand.TwoWheelerOccupancy));

        RuleFor(x => x.TwoWheelerOccupancy)
            .GreaterThanOrEqualTo(0)
            .When(x => x.TwoWheelerOccupancy.HasValue);

        RuleFor(x => x.FourWheelerOccupancy)
            .GreaterThanOrEqualTo(0)
            .When(x => x.FourWheelerOccupancy.HasValue);

        RuleFor(x => x.LandAreaSqM)
            .GreaterThan(0)
            .When(x => x.LandAreaSqM.HasValue);
    }
}

using FluentValidation;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Facilities.Commands.CreateSpots;

public sealed class CreateParkingSpotsCommandValidator : AbstractValidator<CreateParkingSpotsCommand>
{
    public CreateParkingSpotsCommandValidator()
    {
        RuleFor(x => x.FacilityId)
            .NotEmpty();

        RuleFor(x => x.Spots)
            .NotEmpty();

        RuleForEach(x => x.Spots)
            .ChildRules(spot =>
            {
                spot.RuleFor(s => s.SpotNumber)
                    .NotEmpty()
                    .MaximumLength(20);

                spot.RuleFor(s => s.VehicleType)
                    .IsInEnum();

                spot.RuleFor(s => s.PricePerHourNpr)
                    .GreaterThan(0)
                    .LessThanOrEqualTo(100000);
            });
    }
}
using System.Text.RegularExpressions;
using FluentValidation;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Vehicles.Commands.Create;

public sealed class CreateVehicleCommandValidator : AbstractValidator<CreateVehicleCommand>
{
    public CreateVehicleCommandValidator()
    {
        RuleFor(x => x.VehicleType)
            .IsInEnum();

        RuleFor(x => x.VehicleCategory)
            .IsInEnum();

        RuleFor(x => x)
            .Must(x => LicenseCoverage.MatchesSpotType(x.VehicleType, x.VehicleCategory))
            .WithMessage("VehicleCategory must agree with VehicleType: Scooter/Motorcycle are TwoWheeler, Car / Jeep / Van is FourWheeler.")
            .OverridePropertyName(nameof(CreateVehicleCommand.VehicleCategory));

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.VehicleNumber)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(20)
            .Matches(@"^\s*[\p{IsDevanagari}A-Za-z0-9][\p{IsDevanagari}A-Za-z0-9\s\-\.]{1,19}\s*$",
                RegexOptions.IgnoreCase)
            .WithMessage("Vehicle number does not look like a valid number plate.");

        RuleFor(x => x.Brand)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Model)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Color)
            .NotEmpty()
            .MaximumLength(50);
    }
}
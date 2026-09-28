using FluentValidation;

namespace ParkingApp.Application.Licenses.Commands.Create;

public sealed class CreateDrivingLicenseCommandValidator : AbstractValidator<CreateDrivingLicenseCommand>
{
    public CreateDrivingLicenseCommandValidator()
    {
        RuleFor(x => x.LicenseNumber)
            .NotEmpty()
            .WithMessage("License number is required.")
            .MaximumLength(30);

        RuleFor(x => x.CategoriesRaw)
            .NotEmpty()
            .WithMessage("License categories are required.");

        RuleFor(x => x.ExpiryDateRaw)
            .NotEmpty()
            .WithMessage("Expiry date is required.");

        RuleFor(x => x.Front)
            .NotNull()
            .WithMessage("License photo is required.");

        RuleFor(x => x.Back)
            .NotNull()
            .WithMessage("License photo is required.");
    }
}

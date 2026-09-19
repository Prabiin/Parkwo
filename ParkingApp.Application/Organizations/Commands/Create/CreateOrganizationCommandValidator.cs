using FluentValidation;

namespace ParkingApp.Application.Organizations.Commands.Create;

public sealed class CreateOrganizationCommandValidator : AbstractValidator<CreateOrganizationCommand>
{
    public CreateOrganizationCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.RegistrationNumber)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.ContactNumber)
            .NotEmpty()
            .MaximumLength(20)
            .Matches(@"^\+?[0-9\- ]{7,19}$")
            .WithMessage("Contact number does not look like a valid phone number.");

        RuleFor(x => x.Address)
            .NotEmpty()
            .MaximumLength(300);
    }
}
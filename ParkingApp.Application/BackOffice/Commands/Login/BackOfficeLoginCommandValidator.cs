using FluentValidation;

namespace ParkingApp.Application.BackOffice.Commands.Login;

public sealed class BackOfficeLoginCommandValidator : AbstractValidator<BackOfficeLoginCommand>
{
    public BackOfficeLoginCommandValidator()
    {
        RuleFor(x => x.UserNameOrEmail)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(128);
    }
}
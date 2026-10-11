using FluentValidation;

namespace ParkingApp.Application.StaffAuth.Commands.Login;

public sealed class StaffLoginCommandValidator : AbstractValidator<StaffLoginCommand>
{
    public StaffLoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(128);
    }
}

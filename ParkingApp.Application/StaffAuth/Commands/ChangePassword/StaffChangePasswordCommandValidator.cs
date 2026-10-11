using FluentValidation;

namespace ParkingApp.Application.StaffAuth.Commands.ChangePassword;

public sealed class StaffChangePasswordCommandValidator : AbstractValidator<StaffChangePasswordCommand>
{
    public StaffChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("The new password must be different from the current one.");
    }
}

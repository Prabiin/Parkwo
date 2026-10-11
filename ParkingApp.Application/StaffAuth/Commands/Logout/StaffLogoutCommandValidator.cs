using FluentValidation;

namespace ParkingApp.Application.StaffAuth.Commands.Logout;

public sealed class StaffLogoutCommandValidator : AbstractValidator<StaffLogoutCommand>
{
    public StaffLogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty();
    }
}

using FluentValidation;

namespace ParkingApp.Application.StaffAuth.Commands.RefreshToken;

public sealed class StaffRefreshTokenCommandValidator : AbstractValidator<StaffRefreshTokenCommand>
{
    public StaffRefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty();
    }
}

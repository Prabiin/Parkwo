using FluentValidation;

namespace ParkingApp.Application.Features.SendOtp.Command;

public sealed class SendOtpCommandValidator : AbstractValidator<SendOtpCommand>
{
    public SendOtpCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.Channel)
            .IsInEnum();
    }
}
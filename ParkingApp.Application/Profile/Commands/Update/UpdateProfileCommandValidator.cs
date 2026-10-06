using FluentValidation;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Profile.Commands.Update;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(255)
            .EmailAddress();

        RuleFor(x => x.Gender)
            .IsInEnum();

        RuleFor(x => x.DateOfBirth)
            .NotEmpty()
            .LessThanOrEqualTo(today)
            .WithMessage("Date of birth cannot be in the future.")
            .GreaterThan(today.AddYears(-120))
            .WithMessage("Date of birth does not look valid.");
    }
}
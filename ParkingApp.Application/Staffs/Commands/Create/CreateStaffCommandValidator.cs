using FluentValidation;
using ParkingApp.Application.Common.Helpers;

namespace ParkingApp.Application.Staffs.Commands.Create;

public sealed class CreateStaffCommandValidator : AbstractValidator<CreateStaffCommand>
{
    public CreateStaffCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Must(PhoneNumbers.IsValidMobile)
            .WithMessage("Enter a valid Nepali mobile number.");

        // The email doubles as the login, so it must be a real address, not just
        // a unique string.
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);

        RuleFor(x => x.FacilityId)
            .NotEmpty();

        RuleFor(x => x.DeviceName)
            .MaximumLength(120);

        RuleFor(x => x.DeviceBrand)
            .MaximumLength(120);

        RuleFor(x => x.DateOfBirth)
            .LessThan(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .When(x => x.DateOfBirth.HasValue)
            .WithMessage("Date of birth must be in the past.");
    }
}

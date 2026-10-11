using FluentValidation;
using ParkingApp.Application.Common.Helpers;

namespace ParkingApp.Application.Staffs.Commands.Update;

public sealed class UpdateStaffCommandValidator : AbstractValidator<UpdateStaffCommand>
{
    public UpdateStaffCommandValidator()
    {
        RuleFor(x => x.StaffId)
            .NotEmpty();

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Must(PhoneNumbers.IsValidMobile)
            .WithMessage("Enter a valid Nepali mobile number.");

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

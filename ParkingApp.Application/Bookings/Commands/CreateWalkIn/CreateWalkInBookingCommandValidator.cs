using FluentValidation;
using ParkingApp.Application.Common.Helpers;

namespace ParkingApp.Application.Bookings.Commands.CreateWalkIn;

public sealed class CreateWalkInBookingCommandValidator : AbstractValidator<CreateWalkInBookingCommand>
{
    // A walk-in longer than a day is almost always a mistyped digit; rejecting
    // it here keeps a stray "240" from holding a space for ten days.
    private const int MaxWalkInHours = 24;

    public CreateWalkInBookingCommandValidator()
    {
        RuleFor(x => x.FacilityId)
            .NotEmpty();

        RuleFor(x => x.VehicleType)
            .IsInEnum();

        RuleFor(x => x.Hours)
            .InclusiveBetween(1, MaxWalkInHours);

        RuleFor(x => x.RiderName)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Must(PhoneNumbers.IsValidMobile)
            .WithMessage("Enter a valid 10-digit Nepali mobile number.");

        RuleFor(x => x.PlateNumber)
            .NotEmpty()
            .MaximumLength(30);
    }
}

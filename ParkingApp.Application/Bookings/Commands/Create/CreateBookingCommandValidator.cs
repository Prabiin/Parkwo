using FluentValidation;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Bookings.Commands.Create;

public sealed class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.FacilityId)
            .NotEmpty();

        RuleFor(x => x.VehicleId)
            .NotEmpty();

        RuleFor(x => x.EndsAtUtc)
            .GreaterThan(x => x.StartsAtUtc)
            .WithMessage("Booking end time must be after the start time.");
    }
}
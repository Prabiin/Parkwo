using FluentValidation;
using ParkingApp.Application.Common.Helpers;

namespace ParkingApp.Application.Facilities.Queries.GetFacilityAvailability;

/// <summary>
/// The availability grid is asked for either a local day or one exact window,
/// never both. A day is limited to a week ahead — the furthest a booking can
/// run (<see cref="BookingPricing.MaxBillableHours"/>) — so the picker cannot
/// page into the indefinite future.
/// </summary>
public sealed class GetFacilityAvailabilityQueryValidator : AbstractValidator<GetFacilityAvailabilityQuery>
{
    public const int MaxFutureDays = 7;

    public GetFacilityAvailabilityQueryValidator()
    {
        RuleFor(x => x.FacilityId)
            .NotEmpty();

        RuleFor(x => x.VehicleType)
            .IsInEnum();

        RuleFor(x => x.Date)
            .Must(BeWithinHorizon)
            .WithMessage($"Choose a date from today through {MaxFutureDays} days ahead.")
            .When(x => x.Date is not null);

        // A window needs both ends: a start with no end is a mistake, not
        // "open-ended".
        RuleFor(x => x.ToUtc)
            .NotNull()
            .WithMessage("toUtc is required when fromUtc is given.")
            .When(x => x.FromUtc is not null);

        RuleFor(x => x.ToUtc)
            .GreaterThan(x => x.FromUtc)
            .WithMessage("toUtc must be after fromUtc.")
            .When(x => x.FromUtc is not null && x.ToUtc is not null);

        RuleFor(x => x.ToUtc)
            .Must((query, to) => to - query.FromUtc <= TimeSpan.FromHours(BookingPricing.MaxBillableHours))
            .WithMessage($"A window can be at most {BookingPricing.MaxBillableHours} hours.")
            .When(x => x.FromUtc is not null && x.ToUtc is not null
                       && x.ToUtc > x.FromUtc);

        // Window and day are mutually exclusive; the day branch would silently
        // win otherwise, hiding the window the caller meant to ask about.
        RuleFor(x => x.FromUtc)
            .Must((query, _) => query.Date is null)
            .WithMessage("Provide either date or fromUtc/toUtc, not both.")
            .When(x => x.FromUtc is not null);
    }

    private static bool BeWithinHorizon(DateOnly? date)
    {
        var today = LocalTime.Today();
        return date!.Value >= today && date.Value <= today.AddDays(MaxFutureDays);
    }
}

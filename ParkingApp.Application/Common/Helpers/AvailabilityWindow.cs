namespace ParkingApp.Application.Common.Helpers;

/// <summary>
/// The interval an availability feed answers "how many spaces are free" for.
/// Availability is a property of a window, not an instant: a space booked
/// 4–5 PM is free at 3 PM and busy at 4 PM, so counting every live booking
/// regardless of its dates would let a harmless future stay hide a lot that is
/// free right now. This is the same overlap rule the booking gate
/// (<c>CreateBookingCommand</c>) uses, so a feed can never advertise a space the
/// gate would refuse with a 409.
/// </summary>
public readonly record struct AvailabilityWindow(DateTimeOffset FromUtc, DateTimeOffset ToUtc);

public static class AvailabilityWindows
{
    /// <summary>
    /// The window a map pin reports on: now until the end of the minimum paid
    /// stay (<see cref="BookingPricing.MinBillableHours"/>). A pin means "the
    /// stay I would book this second", so a booking that starts within that hour
    /// — or is already in progress — blocks it.
    /// </summary>
    public static AvailabilityWindow StartingNow(DateTimeOffset nowUtc)
        => new(nowUtc, nowUtc.AddHours(BookingPricing.MinBillableHours));
}

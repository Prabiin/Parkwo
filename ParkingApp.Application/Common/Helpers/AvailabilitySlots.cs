namespace ParkingApp.Application.Common.Helpers;

/// <summary>
/// The hourly grid a rider browses before booking: "1–2 PM", "2–3 PM", and so
/// on for a Kathmandu calendar day. Slots are aligned to <em>local</em> hours,
/// not UTC ones — Nepal is UTC+5:45, so a UTC-hour boundary would show up as
/// 1:15–2:15 PM on the rider's screen.
/// </summary>
public static class AvailabilitySlots
{
    public const int MinutesPerSlot = 60;

    /// <summary>A local day is 24 hours and Nepal has no daylight saving.</summary>
    public const int MaxSlotsPerDay = 24;

    /// <summary>
    /// The bookable slots of <paramref name="date"/> at <paramref name="nowUtc"/>.
    /// Today starts at the next local hour boundary, so an hour that has already
    /// begun is never offered; a future day is offered whole. A date already
    /// past yields no slots, which the caller can return as an empty grid.
    /// </summary>
    public static IReadOnlyList<AvailabilityWindow> ForLocalDay(DateOnly date, DateTimeOffset nowUtc)
    {
        var dayStart = LocalTime.StartOfLocalDayUtc(date);
        var dayEnd = LocalTime.EndOfLocalDayUtc(date);
        var today = LocalTime.LocalDateOf(nowUtc);

        if (date < today)
            return [];

        var firstFrom = date == today
            ? LocalTime.CeilToLocalHour(nowUtc)
            : dayStart;

        if (firstFrom < dayStart)
            firstFrom = dayStart;

        var slots = new List<AvailabilityWindow>();

        for (var from = firstFrom;
             slots.Count < MaxSlotsPerDay && from.AddMinutes(MinutesPerSlot) <= dayEnd;
             from = from.AddMinutes(MinutesPerSlot))
        {
            var to = from.AddMinutes(MinutesPerSlot);
            slots.Add(new AvailabilityWindow(from, to));
        }

        return slots;
    }
}

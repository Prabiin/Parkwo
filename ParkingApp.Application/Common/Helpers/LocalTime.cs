using System.Globalization;

namespace ParkingApp.Application.Common.Helpers;

/// <summary>
/// Formats a UTC timestamp into what a rider actually reads on screen.
/// Stays and payments are stored in UTC; this is the one place they are
/// converted to Kathmandu time for display. It also owns the "today" boundary
/// for day-scoped queries, so a shift's parkings are grouped the way staff
/// experience the day rather than by the server's UTC midnight.
/// </summary>
public static class LocalTime
{
    // The app is Nepal-only (NPR, Khalti, eSewa), so Asia/Kathmandu (UTC+5:45).
    private static readonly TimeZoneInfo Kathmandu = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kathmandu");

    public static string? Format(DateTimeOffset? utc)
        => utc is null ? null : Format(utc.Value);

    public static string Format(DateTimeOffset utc)
        => TimeZoneInfo.ConvertTime(utc, Kathmandu)
            .ToString("d MMM yyyy, h:mm tt", CultureInfo.InvariantCulture);

    /// <summary>Time of day only, e.g. "1:00 PM" — the label on a booking slot.</summary>
    public static string FormatTime(DateTimeOffset utc)
        => TimeZoneInfo.ConvertTime(utc, Kathmandu)
            .ToString("h:mm tt", CultureInfo.InvariantCulture);

    /// <summary>The Kathmandu calendar date a UTC instant falls on.</summary>
    public static DateOnly LocalDateOf(DateTimeOffset utc)
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utc, Kathmandu).DateTime);

    /// <summary>Today on the Nepal clock.</summary>
    public static DateOnly Today() => LocalDateOf(DateTimeOffset.UtcNow);

    /// <summary>
    /// Midnight on <paramref name="date"/> in Kathmandu, expressed in UTC — the
    /// inclusive start of a "that day" range. Nepal has no daylight saving, so
    /// the local offset is constant and the boundary is stable.
    /// </summary>
    public static DateTimeOffset StartOfLocalDayUtc(DateOnly date)
    {
        var localMidnight = date.ToDateTime(TimeOnly.MinValue);
        var offset = Kathmandu.GetUtcOffset(localMidnight);
        return new DateTimeOffset(localMidnight, offset).ToUniversalTime();
    }

    /// <summary>
    /// Midnight the following day in Kathmandu, in UTC — the exclusive end of
    /// <paramref name="date"/>'s range.
    /// </summary>
    public static DateTimeOffset EndOfLocalDayUtc(DateOnly date)
        => StartOfLocalDayUtc(date.AddDays(1));

    /// <summary>
    /// The next Kathmandu hour boundary at or after <paramref name="utc"/> — an
    /// instant already on the hour is returned unchanged, so the hour in progress
    /// stays bookable. Alignment happens in local time on purpose: Kathmandu is
    /// UTC+5:45, so a UTC-hour boundary would read as 1:15 PM.
    /// </summary>
    public static DateTimeOffset CeilToLocalHour(DateTimeOffset utc)
    {
        var local = TimeZoneInfo.ConvertTime(utc, Kathmandu);

        if (local.Minute == 0 && local.Second == 0 && local.Millisecond == 0)
            return utc;

        var nextHour = new DateTimeOffset(
            local.Year, local.Month, local.Day, local.Hour, 0, 0, local.Offset).AddHours(1);

        return nextHour.ToUniversalTime();
    }

    /// <summary>
    /// Midnight today in Kathmandu, expressed in UTC — the inclusive start of
    /// today's range.
    /// </summary>
    public static DateTimeOffset StartOfTodayUtc() => StartOfLocalDayUtc(Today());

    /// <summary>Midnight tomorrow in Kathmandu, in UTC — the exclusive end of today's range.</summary>
    public static DateTimeOffset EndOfTodayUtc() => EndOfLocalDayUtc(Today());
}
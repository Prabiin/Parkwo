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

    /// <summary>
    /// Midnight today in Kathmandu, expressed in UTC — the inclusive start of a
    /// "that day" range. Nepal has no daylight saving, so the local offset is
    /// constant and the boundary is stable.
    /// </summary>
    public static DateTimeOffset StartOfTodayUtc()
    {
        var localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Kathmandu);
        return new DateTimeOffset(
            localNow.Year, localNow.Month, localNow.Day, 0, 0, 0, localNow.Offset).ToUniversalTime();
    }

    /// <summary>Midnight tomorrow in Kathmandu, in UTC — the exclusive end of today's range.</summary>
    public static DateTimeOffset EndOfTodayUtc()
        => StartOfTodayUtc().AddDays(1);
}
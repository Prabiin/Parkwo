using System.Globalization;

namespace ParkingApp.Application.Common.Helpers;

/// <summary>
/// Formats a UTC timestamp into what a rider actually reads on screen.
/// Stays and payments are stored in UTC; this is the one place they are
/// converted to Kathmandu time for display.
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
}
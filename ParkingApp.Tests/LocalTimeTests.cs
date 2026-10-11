using ParkingApp.Application.Common.Helpers;

namespace ParkingApp.Tests;

/// <summary>
/// The walk-in phone lookup is scoped to "today", and today is a Kathmandu day
/// (UTC+5:45) rather than a UTC one — otherwise a shift that spans local
/// midnight would lose the vehicles it is looking for. These pin that boundary.
/// </summary>
public class LocalTimeTests
{
    private static readonly TimeZoneInfo Kathmandu = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kathmandu");

    [Fact]
    public void Todays_Range_Is_A_Twenty_Four_Hour_Day_Containing_Now()
    {
        var start = LocalTime.StartOfTodayUtc();
        var end = LocalTime.EndOfTodayUtc();
        var now = DateTimeOffset.UtcNow;

        Assert.Equal(TimeSpan.FromHours(24), end - start);
        Assert.True(start <= now, "today's range should have already started");
        Assert.True(now < end, "today's range should not have ended yet");
    }

    [Fact]
    public void Today_Starts_At_Local_Midnight_In_Kathmandu()
    {
        // The boundary is expressed in UTC, but it is midnight on the Nepal
        // clock — the day the staff are actually working.
        var local = TimeZoneInfo.ConvertTime(LocalTime.StartOfTodayUtc(), Kathmandu);

        Assert.Equal(0, local.Hour);
        Assert.Equal(0, local.Minute);
        Assert.Equal(0, local.Second);
    }
}

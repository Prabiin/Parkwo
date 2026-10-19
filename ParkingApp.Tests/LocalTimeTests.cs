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

    private static readonly DateOnly Day = new(2026, 10, 19);

    [Fact]
    public void A_Local_Day_Is_Twenty_Four_Hours_From_Local_Midnight()
    {
        var start = LocalTime.StartOfLocalDayUtc(Day);
        var end = LocalTime.EndOfLocalDayUtc(Day);

        Assert.Equal(TimeSpan.FromHours(24), end - start);

        var local = TimeZoneInfo.ConvertTime(start, Kathmandu);
        Assert.Equal(0, local.Hour);
        Assert.Equal(0, local.Minute);
    }

    [Fact]
    public void Local_Date_Follows_The_Nepal_Clock_Not_Utc()
    {
        // 00:30 on the 19th in Kathmandu is still the 18th in UTC.
        var instant = LocalTime.StartOfLocalDayUtc(Day).AddMinutes(30);

        Assert.Equal(Day, LocalTime.LocalDateOf(instant));
        Assert.NotEqual(Day, DateOnly.FromDateTime(instant.UtcDateTime));
    }

    [Fact]
    public void FormatTime_Shows_The_Local_Hour()
        => Assert.Equal("12:00 AM", LocalTime.FormatTime(LocalTime.StartOfLocalDayUtc(Day)));

    [Fact]
    public void Ceil_To_Local_Hour_Rounds_Up_Off_The_Hour()
    {
        var offset = Kathmandu.GetUtcOffset(new DateTime(2026, 10, 19, 13, 15, 0));
        var atQuarterPastOne = new DateTimeOffset(new DateTime(2026, 10, 19, 13, 15, 0), offset).ToUniversalTime();

        Assert.Equal("2:00 PM", LocalTime.FormatTime(LocalTime.CeilToLocalHour(atQuarterPastOne)));
    }

    [Fact]
    public void Ceil_To_Local_Hour_Leaves_An_On_The_Hour_Instant_Alone()
    {
        var offset = Kathmandu.GetUtcOffset(new DateTime(2026, 10, 19, 13, 0, 0));
        var atOne = new DateTimeOffset(new DateTime(2026, 10, 19, 13, 0, 0), offset).ToUniversalTime();

        Assert.Equal(atOne, LocalTime.CeilToLocalHour(atOne));
    }
}

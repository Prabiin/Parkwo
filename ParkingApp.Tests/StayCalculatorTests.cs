using ParkingApp.Application.Common.Helpers;

namespace ParkingApp.Tests;

public class StayCalculatorTests
{
    private static readonly DateTimeOffset Entry = new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Stay_Measures_Entry_To_Exit()
    {
        Assert.Equal(90, StayCalculator.StayMinutes(Entry, Entry.AddMinutes(90)));
    }

    [Fact]
    public void Stay_Truncates_Partial_Minutes()
    {
        Assert.Equal(89, StayCalculator.StayMinutes(Entry, Entry.AddMinutes(89.9)));
    }

    [Fact]
    public void Stay_Of_An_Instant_Exit_Is_Zero()
    {
        Assert.Equal(0, StayCalculator.StayMinutes(Entry, Entry));
    }

    [Fact]
    public void Stay_Never_Goes_Negative_On_Clock_Skew()
    {
        Assert.Equal(0, StayCalculator.StayMinutes(Entry, Entry.AddMinutes(-5)));
    }

    [Fact]
    public void Overstay_Is_Zero_When_Leaving_On_Time()
    {
        Assert.Equal(0, StayCalculator.OverstayMinutes(Entry.AddHours(2), Entry.AddHours(2)));
    }

    [Fact]
    public void Overstay_Is_Zero_When_Leaving_Early()
    {
        Assert.Equal(0, StayCalculator.OverstayMinutes(Entry.AddHours(2), Entry.AddHours(1)));
    }

    [Fact]
    public void Overstay_Counts_Minutes_Past_The_Window()
    {
        Assert.Equal(45, StayCalculator.OverstayMinutes(Entry.AddHours(2), Entry.AddMinutes(165)));
    }

    [Fact]
    public void Overstay_Of_Exactly_The_Grace_Is_Not_Flagged()
    {
        Assert.False(StayCalculator.IsOverstay(overstayMinutes: 10, graceMinutes: 10));
    }

    [Fact]
    public void Overstay_Past_The_Grace_Is_Flagged()
    {
        Assert.True(StayCalculator.IsOverstay(overstayMinutes: 11, graceMinutes: 10));
    }

    [Fact]
    public void A_Negative_Grace_Does_Not_Flag_Every_Exit()
    {
        Assert.False(StayCalculator.IsOverstay(overstayMinutes: 0, graceMinutes: -5));
    }

    [Fact]
    public void A_Long_Stay_Covers_The_Window_Plus_Overstay()
    {
        // The realistic bad case: prepaid 2 hours, parked for 5.
        var exit = Entry.AddHours(5);

        Assert.Equal(300, StayCalculator.StayMinutes(Entry, exit));
        Assert.Equal(180, StayCalculator.OverstayMinutes(Entry.AddHours(2), exit));
    }
}

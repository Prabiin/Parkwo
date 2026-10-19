using ParkingApp.Application.Common.Helpers;

namespace ParkingApp.Tests;

/// <summary>
/// The booking grid is hourly and it is a <em>Kathmandu</em> day. Nepal is
/// UTC+5:45, so slots have to land on local hour boundaries — otherwise the
/// rider would see "1:15–2:15 PM". Today also starts at the next local hour, so
/// an hour that has already begun is never offered.
/// </summary>
public class AvailabilitySlotsTests
{
    private static readonly TimeSpan NepalOffset = TimeSpan.FromHours(5) + TimeSpan.FromMinutes(45);
    private static readonly DateOnly Day = new(2026, 10, 19);

    private static DateTimeOffset Local(int hour, int minute = 0)
        => new DateTimeOffset(Day.Year, Day.Month, Day.Day, hour, minute, 0, NepalOffset).ToUniversalTime();

    [Fact]
    public void Today_Starts_At_The_Next_Local_Hour_When_Now_Is_Off_The_Hour()
    {
        var slots = AvailabilitySlots.ForLocalDay(Day, Local(13, 15));

        Assert.Equal(Local(14), slots[0].FromUtc);
        Assert.Equal(Local(15), slots[0].ToUtc);
    }

    [Fact]
    public void The_Hour_In_Progress_Is_Offered_When_Now_Is_Exactly_On_The_Hour()
    {
        var slots = AvailabilitySlots.ForLocalDay(Day, Local(13));

        Assert.Equal(Local(13), slots[0].FromUtc);
        Assert.Equal(Local(14), slots[0].ToUtc);
    }

    [Fact]
    public void Today_Runs_From_The_First_Bookable_Hour_To_Midnight()
    {
        var slots = AvailabilitySlots.ForLocalDay(Day, Local(13));

        // 1 PM through 11 PM–midnight.
        Assert.Equal(11, slots.Count);
        Assert.Equal(Local(23), slots[^1].FromUtc);
        Assert.Equal(LocalTime.EndOfLocalDayUtc(Day), slots[^1].ToUtc);
    }

    [Fact]
    public void A_Later_Now_Leaves_Fewer_Slots()
    {
        var atOne = AvailabilitySlots.ForLocalDay(Day, Local(13));
        var atTwoPmFifteen = AvailabilitySlots.ForLocalDay(Day, Local(14, 15));

        // 15:00 through 23:00.
        Assert.Equal(9, atTwoPmFifteen.Count);
        Assert.Equal(Local(15), atTwoPmFifteen[0].FromUtc);
        Assert.Equal(atOne.Count - 2, atTwoPmFifteen.Count);
    }

    [Fact]
    public void The_Last_Hour_Before_Midnight_Is_Not_Offered_Once_It_Has_Begun()
    {
        Assert.Empty(AvailabilitySlots.ForLocalDay(Day, Local(23, 30)));
    }

    [Fact]
    public void A_Past_Day_Has_No_Bookable_Slots()
    {
        Assert.Empty(AvailabilitySlots.ForLocalDay(Day.AddDays(-1), Local(13)));
    }

    [Fact]
    public void A_Future_Day_Is_Offered_Whole_From_Local_Midnight()
    {
        var future = Day.AddDays(3);
        var slots = AvailabilitySlots.ForLocalDay(future, Local(13));

        Assert.Equal(AvailabilitySlots.MaxSlotsPerDay, slots.Count);
        Assert.Equal(LocalTime.StartOfLocalDayUtc(future), slots[0].FromUtc);
        Assert.Equal(LocalTime.EndOfLocalDayUtc(future), slots[^1].ToUtc);
    }

    [Fact]
    public void Every_Slot_Is_An_Hour_Long_And_Aligned_To_Local_Hours()
    {
        var slots = AvailabilitySlots.ForLocalDay(Day, Local(9, 30));

        Assert.All(slots, slot =>
        {
            Assert.Equal(TimeSpan.FromHours(1), slot.ToUtc - slot.FromUtc);
            Assert.Equal(0, slot.FromUtc.ToOffset(NepalOffset).Minute);
            Assert.Equal(0, slot.FromUtc.ToOffset(NepalOffset).Second);
        });
    }
}

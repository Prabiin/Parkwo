using ParkingApp.Application.Common.Helpers;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Tests;

public class BookingAvailabilityTests
{
    private static readonly DateTimeOffset Base = new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Overlaps_When_Windows_Intersect()
    {
        Assert.True(BookingAvailability.Overlaps(
            Base, Base.AddHours(2),
            Base.AddHours(1), Base.AddHours(3)));
    }

    [Fact]
    public void Overlaps_When_One_Contains_The_Other()
    {
        Assert.True(BookingAvailability.Overlaps(
            Base, Base.AddHours(4),
            Base.AddHours(1), Base.AddHours(2)));
    }

    [Fact]
    public void No_Overlap_When_Windows_Are_Sequential()
    {
        Assert.False(BookingAvailability.Overlaps(
            Base, Base.AddHours(2),
            Base.AddHours(2), Base.AddHours(4)));
    }

    [Fact]
    public void No_Overlap_When_Windows_Are_Disjoint()
    {
        Assert.False(BookingAvailability.Overlaps(
            Base, Base.AddHours(1),
            Base.AddHours(5), Base.AddHours(6)));
    }

    [Theory]
    [InlineData(12, 3, 9)]
    [InlineData(12, 0, 12)]
    [InlineData(12, 12, 0)]
    public void Available_Subtracts_Live_Bookings_From_Capacity(int capacity, int taken, int expected)
    {
        Assert.Equal(expected, BookingAvailability.Available(capacity, taken));
    }

    [Fact]
    public void Available_Clamps_At_Zero_When_Oversold()
    {
        Assert.Equal(0, BookingAvailability.Available(2, 5));
    }

    [Fact]
    public void ConsumesCapacity_Live_Pending_Hold_Occupyes_A_Space()
    {
        Assert.True(BookingAvailability.ConsumesCapacity(
            BookingStatusEnum.PendingPayment, Base.AddMinutes(10), Base));
    }

    [Fact]
    public void ConsumesCapacity_Expired_Pending_Hold_Releases_The_Space()
    {
        // The sweeper may not have run yet; availability must still be correct.
        Assert.False(BookingAvailability.ConsumesCapacity(
            BookingStatusEnum.PendingPayment, Base.AddMinutes(-1), Base));
    }

    [Theory]
    [InlineData(BookingStatusEnum.Confirmed)]
    [InlineData(BookingStatusEnum.Active)]
    public void ConsumesCapacity_Paid_Bookings_Occupy_Even_Past_Hold_Expiry(BookingStatusEnum status)
    {
        Assert.True(BookingAvailability.ConsumesCapacity(
            status, Base.AddMinutes(-1), Base));
    }

    [Theory]
    [InlineData(BookingStatusEnum.Cancelled)]
    [InlineData(BookingStatusEnum.Expired)]
    [InlineData(BookingStatusEnum.Completed)]
    [InlineData(BookingStatusEnum.Refunded)]
    public void ConsumesCapacity_Terminal_Bookings_Release_The_Space(BookingStatusEnum status)
    {
        Assert.False(BookingAvailability.ConsumesCapacity(
            status, Base.AddHours(10), Base));
    }

    [Fact]
    public void EffectiveEnd_Is_The_Raw_Window_When_Nothing_Was_Released_Early()
    {
        Assert.Equal(
            Base.AddHours(4),
            BookingAvailability.EffectiveEnd(Base.AddHours(4), null));
    }

    [Fact]
    public void EffectiveEnd_Shrinks_To_An_Early_Exit()
    {
        // Booked 10:00-14:00, drove out at 11:00: the space is free from 11:00.
        Assert.Equal(
            Base.AddHours(1),
            BookingAvailability.EffectiveEnd(Base.AddHours(4), Base.AddHours(1)));
    }

    [Fact]
    public void EffectiveEnd_Ignores_A_Release_After_The_Window()
    {
        // A late sweeper stamp must not extend a booking past what was paid for.
        Assert.Equal(
            Base.AddHours(4),
            BookingAvailability.EffectiveEnd(Base.AddHours(4), Base.AddHours(9)));
    }

    [Fact]
    public void Exit_Scan_Frees_A_Space_Before_Its_Window_Ends()
    {
        // The regression that matters: a lot full of cars that already left.
        var releasedAt = Base.AddHours(1);

        Assert.False(BookingAvailability.ConsumesCapacity(
            BookingStatusEnum.Confirmed,
            Base.AddHours(10),
            releasedAt,
            Base.AddHours(2)));
    }

    [Fact]
    public void Space_Still_Occupied_Until_The_Exit_Scan_Lands()
    {
        Assert.True(BookingAvailability.ConsumesCapacity(
            BookingStatusEnum.Active,
            Base.AddHours(10),
            null,
            Base.AddHours(2)));
    }

    [Fact]
    public void An_Exit_Scan_Does_Not_Resurrect_A_Lapsed_Pending_Hold()
    {
        var releasedAt = Base.AddHours(1);

        Assert.False(BookingAvailability.ConsumesCapacity(
            BookingStatusEnum.PendingPayment,
            Base.AddMinutes(-5),
            releasedAt,
            Base.AddHours(2)));
    }

    [Fact]
    public void A_Vehicle_Leaving_Early_Lets_A_Later_Booking_Overlap_The_Remaining_Window()
    {
        // Booked 10:00-14:00, out at 11:00. A new rider wanting 12:00-13:00 must
        // be told there is space, even though the raw windows still intersect.
        var startsAt = Base.AddHours(2);
        var endsAt = startsAt.AddHours(1);

        var overlapsAfterRelease = BookingAvailability.Overlaps(
            Base,
            BookingAvailability.EffectiveEnd(Base.AddHours(4), Base.AddHours(1)),
            startsAt,
            endsAt);

        Assert.False(overlapsAfterRelease);
    }
}

using ParkingApp.Application.Common.Helpers;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Tests;

public class FacilityAvailabilityTests
{
    private static readonly Guid Facility = Guid.NewGuid();

    private static readonly Dictionary<(Guid FacilityId, VehicleTypeEnum VehicleType), int> NoBookings = [];

    [Fact]
    public void Available_Is_Full_Occupancy_When_Nothing_Is_Taken()
    {
        Assert.Equal(12, FacilityAvailability.Available(
            12, NoBookings, Facility, VehicleTypeEnum.TwoWheeler));
    }

    [Fact]
    public void Available_Subtracts_The_Taken_Count_For_That_Type()
    {
        var taken = new Dictionary<(Guid, VehicleTypeEnum), int>
        {
            [(Facility, VehicleTypeEnum.TwoWheeler)] = 5,
            [(Facility, VehicleTypeEnum.FourWheeler)] = 2
        };

        Assert.Equal(7, FacilityAvailability.Available(
            12, taken, Facility, VehicleTypeEnum.TwoWheeler));
        Assert.Equal(2, FacilityAvailability.Available(
            4, taken, Facility, VehicleTypeEnum.FourWheeler));
    }

    [Fact]
    public void Available_Clamps_At_Zero_When_Oversold()
    {
        var taken = new Dictionary<(Guid, VehicleTypeEnum), int>
        {
            [(Facility, VehicleTypeEnum.TwoWheeler)] = 15
        };

        Assert.Equal(0, FacilityAvailability.Available(
            12, taken, Facility, VehicleTypeEnum.TwoWheeler));
    }

    [Fact]
    public void Available_Ignores_Another_Facilitys_Bookings()
    {
        var taken = new Dictionary<(Guid, VehicleTypeEnum), int>
        {
            [(Guid.NewGuid(), VehicleTypeEnum.TwoWheeler)] = 9
        };

        Assert.Equal(12, FacilityAvailability.Available(
            12, taken, Facility, VehicleTypeEnum.TwoWheeler));
    }

    // A day's worth of intervals for one facility, so the per-slot count can be
    // exercised without a database.
    private static readonly DateTimeOffset Midnight =
        new(2026, 10, 19, 0, 0, 0, TimeSpan.Zero);

    private static FacilityAvailability.OccupiedInterval Occupied(
        int fromHour, int toHour,
        Guid? facilityId = null,
        VehicleTypeEnum vehicleType = VehicleTypeEnum.TwoWheeler)
        => new(facilityId ?? Facility, vehicleType, Midnight.AddHours(fromHour), Midnight.AddHours(toHour));

    private static int TakenFor(IReadOnlyList<FacilityAvailability.OccupiedInterval> intervals,
        int fromHour, int toHour, VehicleTypeEnum vehicleType = VehicleTypeEnum.TwoWheeler)
        => FacilityAvailability.CountOverlapping(
            intervals, Facility, vehicleType, new AvailabilityWindow(Midnight.AddHours(fromHour), Midnight.AddHours(toHour)));

    [Fact]
    public void CountOverlapping_Counts_A_Booking_Only_In_The_Slots_It_Touches()
    {
        IReadOnlyList<FacilityAvailability.OccupiedInterval> intervals = [Occupied(14, 15)];

        Assert.Equal(1, TakenFor(intervals, 14, 15));
        Assert.Equal(1, TakenFor(intervals, 13, 16));
        Assert.Equal(0, TakenFor(intervals, 15, 16)); // the next hour is free
        Assert.Equal(0, TakenFor(intervals, 13, 14)); // the hour before too
    }

    [Fact]
    public void CountOverlapping_Counts_A_Long_Booking_Against_Every_Slot_It_Covers()
    {
        IReadOnlyList<FacilityAvailability.OccupiedInterval> intervals = [Occupied(14, 17)];

        Assert.Equal(1, TakenFor(intervals, 14, 15));
        Assert.Equal(1, TakenFor(intervals, 15, 16));
        Assert.Equal(1, TakenFor(intervals, 16, 17));
    }

    [Fact]
    public void CountOverlapping_Ignores_Other_Vehicle_Types_And_Facilities()
    {
        IReadOnlyList<FacilityAvailability.OccupiedInterval> intervals =
        [
            Occupied(14, 15, vehicleType: VehicleTypeEnum.FourWheeler),
            Occupied(14, 15, facilityId: Guid.NewGuid())
        ];

        Assert.Equal(0, TakenFor(intervals, 14, 15));
    }

    // The trap the grid cannot see on its own: a 2-space lot with 2–3 PM and
    // 3–4 PM taken shows one free hour in each cell, but nothing free across
    // 2–4 PM. This is why the booking screen re-checks a multi-slot selection as
    // one window before submitting.
    [Fact]
    public void CountOverlapping_Can_Be_Higher_Across_A_Window_Than_In_Any_Single_Slot()
    {
        IReadOnlyList<FacilityAvailability.OccupiedInterval> intervals = [Occupied(14, 15), Occupied(15, 16)];

        Assert.Equal(1, TakenFor(intervals, 14, 15));
        Assert.Equal(1, TakenFor(intervals, 15, 16));
        Assert.Equal(2, TakenFor(intervals, 14, 16));
    }

    [Fact]
    public void StartingNow_Window_Covers_The_Minimum_Stay()
    {
        var now = DateTimeOffset.UtcNow;
        var window = AvailabilityWindows.StartingNow(now);

        Assert.Equal(now, window.FromUtc);
        Assert.Equal(now.AddHours(BookingPricing.MinBillableHours), window.ToUtc);
    }

    // A lot only "serves" a category when it has at least one space of it, so a
    // zero-occupancy type drops out of pins, the detail screen and the booking
    // vehicle list rather than being rendered as unavailable.
    [Theory]
    [InlineData(VehicleTypeEnum.TwoWheeler, true)]
    [InlineData(VehicleTypeEnum.FourWheeler, false)]
    public void ServesType_Only_For_A_Type_With_Spaces(VehicleTypeEnum type, bool expected)
    {
        Assert.Equal(expected, FacilityAvailability.ServesType(3, 0, type));
    }

    [Fact]
    public void ServesType_False_When_Both_Types_Are_Zero()
    {
        Assert.False(FacilityAvailability.ServesType(0, 0, VehicleTypeEnum.TwoWheeler));
        Assert.False(FacilityAvailability.ServesType(0, 0, VehicleTypeEnum.FourWheeler));
    }
}

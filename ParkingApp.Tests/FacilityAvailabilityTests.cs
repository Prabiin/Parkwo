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
}

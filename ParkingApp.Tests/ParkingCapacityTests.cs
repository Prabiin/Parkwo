using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Configuration;

namespace ParkingApp.Tests;

public class ParkingCapacityTests
{
    private static readonly ParkingStandards Standards = new();

    [Theory]
    [InlineData(100, 2, 50)]
    [InlineData(12.5, 12.5, 1)]
    [InlineData(1.9, 2, 0)]
    public void MaxFittable_Floors_By_Standard(decimal land, decimal perVehicle, int expected)
    {
        Assert.Equal(expected, ParkingCapacity.MaxFittable(land, perVehicle));
    }

    [Fact]
    public void MaxFittable_Null_Land_Returns_Null()
    {
        Assert.Null(ParkingCapacity.MaxFittable(null, 2));
    }

    [Fact]
    public void EstimatedArea_Sums_Both_Types()
    {
        // 10 bikes * 2 + 4 cars * 12.5 = 70
        Assert.Equal(70, ParkingCapacity.EstimatedAreaRequiredSqM(10, 4, Standards));
    }

    [Theory]
    [InlineData(10, 4, 100, false)]
    [InlineData(10, 4, 69.9, true)]
    public void ExceedsLandArea_Flags_Overclaim(int twoW, int fourW, decimal land, bool expected)
    {
        Assert.Equal(expected, ParkingCapacity.ExceedsLandArea(twoW, fourW, land, Standards));
    }

    [Fact]
    public void ExceedsLandArea_Null_Land_Returns_Null()
    {
        Assert.Null(ParkingCapacity.ExceedsLandArea(10, 4, null, Standards));
    }
}

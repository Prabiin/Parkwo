using ParkingApp.Domain;

namespace ParkingApp.Tests;

public class FreeParkingFacilityTests
{
    [Fact]
    public void New_Facility_Is_Active_And_Not_Deleted()
    {
        var facility = new FreeParkingFacility();

        Assert.True(facility.IsActive);
        Assert.False(facility.IsDeleted);
    }

    [Fact]
    public void SetCoordinates_Keeps_Latitude_And_Longitude()
    {
        var facility = new FreeParkingFacility();

        facility.SetCoordinates(27.7172, 85.3240);

        Assert.Equal(27.7172, facility.Latitude);
        Assert.Equal(85.3240, facility.Longitude);
    }

    [Fact]
    public void SetCoordinates_Builds_A_Point_With_Longitude_As_X_And_Latitude_As_Y()
    {
        var facility = new FreeParkingFacility();

        facility.SetCoordinates(27.7172, 85.3240);

        Assert.NotNull(facility.Location);
        // PostGIS is (x = longitude, y = latitude) — reversing these is the
        // classic bug that silently places every marker in the wrong country.
        Assert.Equal(85.3240, facility.Location!.X);
        Assert.Equal(27.7172, facility.Location.Y);
        Assert.Equal(4326, facility.Location.SRID);
    }

    [Fact]
    public void SetCoordinates_Replaces_A_Previous_Point()
    {
        var facility = new FreeParkingFacility();
        facility.SetCoordinates(27.7172, 85.3240);

        facility.SetCoordinates(28.2096, 83.9856);

        Assert.Equal(28.2096, facility.Latitude);
        Assert.Equal(83.9856, facility.Longitude);
        Assert.Equal(83.9856, facility.Location!.X);
        Assert.Equal(28.2096, facility.Location.Y);
    }
}

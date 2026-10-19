using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Facilities.Queries.GetNearbyFacilities;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Tests;

/// <summary>
/// The nearby query contract: coordinates are always required, a blank query
/// means "parkings near me", and a typed one has to be a usable search term.
/// The radius is no longer a parameter — it lives in NearbySettings.
/// </summary>
public class GetNearbyFacilitiesQueryValidatorTests
{
    private static readonly GetNearbyFacilitiesQueryValidator Validator = new();

    private static GetNearbyFacilitiesQuery Query(
        double latitude = 27.6721,
        double longitude = 85.3178,
        VehicleTypeEnum? vehicleType = null,
        string? search = null)
        => new(latitude, longitude, vehicleType, search);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Bo")]
    [InlineData("Boudha")]
    public void Accepts_Blank_And_Usable_Search_Terms(string? search)
        => Assert.True(Validator.Validate(Query(search: search)).IsValid);

    [Theory]
    [InlineData("a")] // too short to be selective
    [InlineData(" a ")] // trims to one character
    public void Rejects_A_Search_Term_Below_The_Minimum(string search)
        => Assert.False(Validator.Validate(Query(search: search)).IsValid);

    [Fact]
    public void Accepts_A_Search_Term_At_The_Maximum()
        => Assert.True(Validator.Validate(
            Query(search: new string('a', FacilityTextSearch.MaxQueryLength))).IsValid);

    [Fact]
    public void Rejects_A_Search_Term_Above_The_Maximum()
        => Assert.False(Validator.Validate(
            Query(search: new string('a', FacilityTextSearch.MaxQueryLength + 1))).IsValid);

    [Theory]
    [InlineData(90.1, 85.0)]
    [InlineData(-90.1, 85.0)]
    [InlineData(27.0, 180.1)]
    [InlineData(27.0, -180.1)]
    public void Rejects_Coordinates_Outside_The_Globe(double latitude, double longitude)
        => Assert.False(Validator.Validate(Query(latitude: latitude, longitude: longitude)).IsValid);

    [Fact]
    public void Rejects_An_Undefined_Vehicle_Type()
        => Assert.False(Validator.Validate(Query(vehicleType: (VehicleTypeEnum)99)).IsValid);
}

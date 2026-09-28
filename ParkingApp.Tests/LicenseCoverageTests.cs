using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Tests;

public class LicenseCoverageTests
{
    [Theory]
    [InlineData(LicenseCategoryEnum.K)]
    [InlineData(LicenseCategoryEnum.A)]
    [InlineData(LicenseCategoryEnum.A1)]
    public void Scooter_CoveredBy_K_A_A1(LicenseCategoryEnum held)
    {
        Assert.True(LicenseCoverage.Covers([held], VehicleCategoryEnum.Scooter));
    }

    [Fact]
    public void Scooter_NotCoveredBy_B()
    {
        Assert.False(LicenseCoverage.Covers([LicenseCategoryEnum.B], VehicleCategoryEnum.Scooter));
    }

    [Theory]
    [InlineData(LicenseCategoryEnum.A)]
    [InlineData(LicenseCategoryEnum.A1)]
    public void Motorcycle_CoveredBy_A_A1(LicenseCategoryEnum held)
    {
        Assert.True(LicenseCoverage.Covers([held], VehicleCategoryEnum.Motorcycle));
    }

    [Theory]
    [InlineData(LicenseCategoryEnum.K)]
    [InlineData(LicenseCategoryEnum.B)]
    public void Motorcycle_NotCoveredBy_K_Or_B(LicenseCategoryEnum held)
    {
        Assert.False(LicenseCoverage.Covers([held], VehicleCategoryEnum.Motorcycle));
    }

    [Fact]
    public void CarJeepVan_CoveredOnlyBy_B()
    {
        Assert.True(LicenseCoverage.Covers([LicenseCategoryEnum.B], VehicleCategoryEnum.CarJeepVan));
        Assert.False(LicenseCoverage.Covers([LicenseCategoryEnum.A], VehicleCategoryEnum.CarJeepVan));
        Assert.False(LicenseCoverage.Covers([LicenseCategoryEnum.A1], VehicleCategoryEnum.CarJeepVan));
        Assert.False(LicenseCoverage.Covers([LicenseCategoryEnum.K], VehicleCategoryEnum.CarJeepVan));
    }

    [Fact]
    public void MultiCategory_Card_Covers_All()
    {
        var held = new[] { LicenseCategoryEnum.A, LicenseCategoryEnum.B };
        Assert.True(LicenseCoverage.Covers(held, VehicleCategoryEnum.Scooter));
        Assert.True(LicenseCoverage.Covers(held, VehicleCategoryEnum.Motorcycle));
        Assert.True(LicenseCoverage.Covers(held, VehicleCategoryEnum.CarJeepVan));
    }

    [Fact]
    public void Empty_Held_Covers_Nothing()
    {
        Assert.False(LicenseCoverage.Covers([], VehicleCategoryEnum.Scooter));
        Assert.False(LicenseCoverage.Covers([], VehicleCategoryEnum.Motorcycle));
        Assert.False(LicenseCoverage.Covers([], VehicleCategoryEnum.CarJeepVan));
    }

    [Theory]
    [InlineData(VehicleTypeEnum.TwoWheeler, VehicleCategoryEnum.Scooter, true)]
    [InlineData(VehicleTypeEnum.TwoWheeler, VehicleCategoryEnum.Motorcycle, true)]
    [InlineData(VehicleTypeEnum.TwoWheeler, VehicleCategoryEnum.CarJeepVan, false)]
    [InlineData(VehicleTypeEnum.FourWheeler, VehicleCategoryEnum.CarJeepVan, true)]
    [InlineData(VehicleTypeEnum.FourWheeler, VehicleCategoryEnum.Scooter, false)]
    [InlineData(VehicleTypeEnum.FourWheeler, VehicleCategoryEnum.Motorcycle, false)]
    public void SpotType_MustAgreeWith_Category(
        VehicleTypeEnum type, VehicleCategoryEnum category, bool expected)
    {
        Assert.Equal(expected, LicenseCoverage.MatchesSpotType(type, category));
    }
}

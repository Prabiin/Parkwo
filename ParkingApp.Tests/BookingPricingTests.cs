using ParkingApp.Application.Common.Helpers;

namespace ParkingApp.Tests;

public class BookingPricingTests
{
    [Theory]
    [InlineData(60, 1)]
    [InlineData(61, 2)]
    [InlineData(120, 2)]
    [InlineData(90, 2)]
    [InlineData(1440, 24)]
    public void BillableHours_Rounds_Partial_Up(int minutes, int expected)
    {
        Assert.Equal(expected, BookingPricing.BillableHours(TimeSpan.FromMinutes(minutes)));
    }

    [Fact]
    public void BillableHours_Zero_Window_Is_Free()
    {
        Assert.Equal(0, BookingPricing.BillableHours(TimeSpan.Zero));
    }

    [Fact]
    public void BillableHours_Negative_Window_Is_Free()
    {
        Assert.Equal(0, BookingPricing.BillableHours(TimeSpan.FromHours(-3)));
    }

    [Fact]
    public void BillableHours_Seconds_Below_An_Hour_Still_Costs_One_Hour()
    {
        Assert.Equal(1, BookingPricing.BillableHours(TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void BillableHours_Clamps_To_The_Maximum_Window()
    {
        Assert.Equal(
            BookingPricing.MaxBillableHours,
            BookingPricing.BillableHours(TimeSpan.FromDays(30)));
    }

    [Fact]
    public void TotalNpr_Multiplies_Rate_By_Whole_Hours()
    {
        // 3 hours at 20/hr
        Assert.Equal(60m, BookingPricing.TotalNpr(20m, 3));
    }

    [Fact]
    public void ToPaisa_Converts_Rupees_To_Paisa()
    {
        Assert.Equal(2000L, BookingPricing.ToPaisa(20m));
    }

    [Fact]
    public void ToPaisa_Rounds_Half_Paisa_Away_From_Zero()
    {
        // 20.005 * 100 = 2000.5 -> 2001, so we never under-charge a rider
        Assert.Equal(2001L, BookingPricing.ToPaisa(20.005m));
    }

    [Fact]
    public void ToPaisa_Zero_Amount_Is_Zero()
    {
        Assert.Equal(0L, BookingPricing.ToPaisa(0m));
    }

    [Fact]
    public void PricePerHourFor_Null_When_Type_Not_Offered()
    {
        Assert.Null(BookingPricing.PricePerHourFor(
            ParkingApp.Domain.Common.Enums.VehicleTypeEnum.TwoWheeler, 0, 20m));
    }

    [Fact]
    public void PricePerHourFor_Returned_When_Type_Offered()
    {
        Assert.Equal(20m, BookingPricing.PricePerHourFor(
            ParkingApp.Domain.Common.Enums.VehicleTypeEnum.FourWheeler, 5, 20m));
    }
}
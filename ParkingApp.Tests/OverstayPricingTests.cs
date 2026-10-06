using ParkingApp.Application.Common.Helpers;

namespace ParkingApp.Tests;

public class OverstayPricingTests
{
    [Fact]
    public void Leaving_With_No_Overstay_Charges_Nothing()
    {
        Assert.Null(OverstayPricing.Calculate(100m, overstayMinutes: 0, graceMinutes: 10));
    }

    [Fact]
    public void Leaving_Early_Charges_Nothing()
    {
        Assert.Null(OverstayPricing.Calculate(100m, overstayMinutes: -20, graceMinutes: 10));
    }

    [Fact]
    public void An_Overstay_Inside_The_Grace_Charges_Nothing()
    {
        Assert.Null(OverstayPricing.Calculate(100m, overstayMinutes: 5, graceMinutes: 10));
    }

    [Fact]
    public void An_Overstay_Of_Exactly_The_Grace_Charges_Nothing()
    {
        Assert.Null(OverstayPricing.Calculate(100m, overstayMinutes: 10, graceMinutes: 10));
    }

    [Fact]
    public void A_Single_Minute_Beyond_The_Grace_Costs_A_Full_Hour()
    {
        var charge = OverstayPricing.Calculate(100m, overstayMinutes: 11, graceMinutes: 10);

        Assert.NotNull(charge);
        Assert.Equal(1, charge.Value.BillableHours);
        Assert.Equal(10_000, charge.Value.AmountPaisa);
    }

    [Fact]
    public void Sixty_Minutes_Beyond_The_Grace_Is_Still_One_Hour()
    {
        var charge = OverstayPricing.Calculate(100m, overstayMinutes: 70, graceMinutes: 10);

        Assert.NotNull(charge);
        Assert.Equal(1, charge.Value.BillableHours);
    }

    [Fact]
    public void SixtyOne_Minutes_Beyond_The_Grace_Rounds_Up_To_Two_Hours()
    {
        var charge = OverstayPricing.Calculate(100m, overstayMinutes: 71, graceMinutes: 10);

        Assert.NotNull(charge);
        Assert.Equal(2, charge.Value.BillableHours);
        Assert.Equal(20_000, charge.Value.AmountPaisa);
    }

    [Fact]
    public void Forty_Minutes_Late_With_Ten_Minutes_Grace_Is_Billed_As_One_Hour()
    {
        var charge = OverstayPricing.Calculate(150m, overstayMinutes: 40, graceMinutes: 10);

        Assert.NotNull(charge);
        Assert.Equal(1, charge.Value.BillableHours);
        Assert.Equal(15_000, charge.Value.AmountPaisa);
    }

    [Fact]
    public void A_Zero_Grace_Charges_From_The_First_Late_Minute()
    {
        var charge = OverstayPricing.Calculate(100m, overstayMinutes: 1, graceMinutes: 0);

        Assert.NotNull(charge);
        Assert.Equal(1, charge.Value.BillableHours);
    }

    [Fact]
    public void A_Negative_Grace_Does_Not_Charge_Every_Exit()
    {
        Assert.Null(OverstayPricing.Calculate(100m, overstayMinutes: 0, graceMinutes: -5));
    }

    [Fact]
    public void A_Facility_That_Charges_Nothing_Owes_Nothing()
    {
        Assert.Null(OverstayPricing.Calculate(0m, overstayMinutes: 120, graceMinutes: 10));
    }

    [Fact]
    public void The_Amount_Rounds_Up_To_The_Nearest_Paisa()
    {
        // 10.005 * 100 = 1000.5 paisa — gateways take integers, so round away
        // from zero rather than silently shaving the rider a paisa.
        var charge = OverstayPricing.Calculate(10.005m, overstayMinutes: 11, graceMinutes: 10);

        Assert.NotNull(charge);
        Assert.Equal(1_001, charge.Value.AmountPaisa);
    }

    [Fact]
    public void A_Long_Overstay_Bills_Every_Hour_Beyond_The_Grace()
    {
        // Booked until 14:00, left 17:05, 10 minute grace => 175 minutes billed.
        var charge = OverstayPricing.Calculate(100m, overstayMinutes: 185, graceMinutes: 10);

        Assert.NotNull(charge);
        Assert.Equal(3, charge.Value.BillableHours);
        Assert.Equal(30_000, charge.Value.AmountPaisa);
    }
}

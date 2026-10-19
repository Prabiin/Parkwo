using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Facilities.Queries.GetFacilityAvailability;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Tests;

/// <summary>
/// The availability grid is asked either as a local day (the picker) or as one
/// exact window (checking a multi-slot selection). Never both, a window needs
/// both ends, and a day cannot be in the past or beyond the booking horizon.
/// </summary>
public class GetFacilityAvailabilityQueryValidatorTests
{
    private static readonly GetFacilityAvailabilityQueryValidator Validator = new();
    private static readonly DateOnly Today = LocalTime.Today();

    private static GetFacilityAvailabilityQuery Query(
        Guid? facilityId = null,
        VehicleTypeEnum vehicleType = VehicleTypeEnum.TwoWheeler,
        DateOnly? date = null,
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null)
        => new(facilityId ?? Guid.NewGuid(), vehicleType, date, fromUtc, toUtc);

    [Fact]
    public void Accepts_No_Parameters_So_The_Screen_Defaults_To_Today()
        => Assert.True(Validator.Validate(Query()).IsValid);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    public void Accepts_A_Day_From_Today_Through_The_Horizon(int daysAhead)
        => Assert.True(Validator.Validate(Query(date: Today.AddDays(daysAhead))).IsValid);

    [Fact]
    public void Rejects_A_Day_Before_Today()
        => Assert.False(Validator.Validate(Query(date: Today.AddDays(-1))).IsValid);

    [Fact]
    public void Rejects_A_Day_Beyond_The_Horizon()
        => Assert.False(Validator.Validate(
            Query(date: Today.AddDays(GetFacilityAvailabilityQueryValidator.MaxFutureDays + 1))).IsValid);

    [Fact]
    public void Accepts_An_Exact_Window()
    {
        var from = DateTimeOffset.UtcNow.AddMinutes(30);
        Assert.True(Validator.Validate(Query(fromUtc: from, toUtc: from.AddHours(2))).IsValid);
    }

    [Fact]
    public void Rejects_A_Window_With_Only_A_Start()
        => Assert.False(Validator.Validate(
            Query(fromUtc: DateTimeOffset.UtcNow.AddHours(1))).IsValid);

    [Fact]
    public void Rejects_A_Window_That_Ends_Before_It_Starts()
    {
        var from = DateTimeOffset.UtcNow.AddHours(1);
        Assert.False(Validator.Validate(Query(fromUtc: from, toUtc: from)).IsValid);
    }

    [Fact]
    public void Rejects_A_Window_Longer_Than_A_Booking_May_Run()
    {
        var from = DateTimeOffset.UtcNow;
        Assert.False(Validator.Validate(
            Query(fromUtc: from, toUtc: from.AddHours(BookingPricing.MaxBillableHours + 1))).IsValid);
    }

    [Fact]
    public void Rejects_Asking_For_A_Day_And_A_Window_At_Once()
    {
        var from = DateTimeOffset.UtcNow;
        Assert.False(Validator.Validate(
            Query(date: Today, fromUtc: from, toUtc: from.AddHours(1))).IsValid);
    }

    [Fact]
    public void Rejects_An_Empty_Facility_Id()
        => Assert.False(Validator.Validate(Query(facilityId: Guid.Empty)).IsValid);

    [Fact]
    public void Rejects_An_Undefined_Vehicle_Type()
        => Assert.False(Validator.Validate(Query(vehicleType: (VehicleTypeEnum)99)).IsValid);
}

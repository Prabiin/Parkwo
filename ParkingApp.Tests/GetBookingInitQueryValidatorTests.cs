using ParkingApp.Application.Bookings.Queries.GetBookingInit;

namespace ParkingApp.Tests;

public class GetBookingInitQueryValidatorTests
{
    private static readonly GetBookingInitQueryValidator Validator = new();

    [Fact]
    public void Accepts_A_Facility_Id()
        => Assert.True(Validator.Validate(new GetBookingInitQuery(Guid.NewGuid())).IsValid);

    [Fact]
    public void Rejects_An_Empty_Facility_Id()
        => Assert.False(Validator.Validate(new GetBookingInitQuery(Guid.Empty)).IsValid);
}

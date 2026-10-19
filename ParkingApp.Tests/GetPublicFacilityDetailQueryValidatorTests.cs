using ParkingApp.Application.Facilities.Queries.GetPublicFacilityDetail;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Tests;

public class GetPublicFacilityDetailQueryValidatorTests
{
    private static readonly GetPublicFacilityDetailQueryValidator Validator = new();

    [Fact]
    public void Accepts_A_Facility_Id_With_No_Vehicle_Type()
        => Assert.True(Validator.Validate(new GetPublicFacilityDetailQuery(Guid.NewGuid(), null)).IsValid);

    [Fact]
    public void Accepts_A_Known_Vehicle_Type()
        => Assert.True(Validator.Validate(
            new GetPublicFacilityDetailQuery(Guid.NewGuid(), VehicleTypeEnum.FourWheeler)).IsValid);

    [Fact]
    public void Rejects_An_Empty_Facility_Id()
        => Assert.False(Validator.Validate(new GetPublicFacilityDetailQuery(Guid.Empty, null)).IsValid);

    [Fact]
    public void Rejects_An_Unknown_Vehicle_Type()
        => Assert.False(Validator.Validate(
            new GetPublicFacilityDetailQuery(Guid.NewGuid(), (VehicleTypeEnum)99)).IsValid);
}

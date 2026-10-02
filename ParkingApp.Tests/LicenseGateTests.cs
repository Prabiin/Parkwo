using ParkingApp.Application.Common.Helpers;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Tests;

public class LicenseGateTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

    private static DrivingLicense License(params LicenseCategoryEnum[] categories)
        => DrivingLicense.Create(
            Guid.NewGuid(),
            "TEST-123",
            categories,
            "https://files.example/front.jpg",
            "https://files.example/back.jpg",
            Today.AddYears(2));

    private static DrivingLicense Approved(
        ApprovalStatusEnum status,
        params LicenseCategoryEnum[] categories)
    {
        var license = License(categories);
        license.ApplyReview(status, null);
        return license;
    }

    [Fact]
    public void Check_Blocks_When_No_License_On_File()
    {
        Assert.NotNull(LicenseGate.Check(
            [], VehicleCategoryEnum.CarJeepVan));
    }

    [Fact]
    public void Check_Blocks_When_License_Is_Still_Pending()
    {
        Assert.NotNull(LicenseGate.Check(
            [License(LicenseCategoryEnum.B)], VehicleCategoryEnum.CarJeepVan));
    }

    [Fact]
    public void Check_Blocks_When_License_Is_Expired()
    {
        var license = DrivingLicense.Create(
            Guid.NewGuid(), "OLD-1", [LicenseCategoryEnum.B],
            "https://files.example/f.jpg", "https://files.example/b.jpg",
            Today.AddDays(-1));

        license.ApplyReview(ApprovalStatusEnum.Verified, null);

        Assert.NotNull(LicenseGate.Check([license], VehicleCategoryEnum.CarJeepVan));
    }

    [Fact]
    public void Check_Allows_Verified_Covering_License()
    {
        Assert.Null(LicenseGate.Check(
            [Approved(ApprovalStatusEnum.Verified, LicenseCategoryEnum.B)],
            VehicleCategoryEnum.CarJeepVan));
    }

    [Fact]
    public void Check_Blocks_Verified_License_That_Does_Not_Cover_The_Vehicle()
    {
        // A four-wheeler licence cannot park a scooter.
        Assert.NotNull(LicenseGate.Check(
            [Approved(ApprovalStatusEnum.Verified, LicenseCategoryEnum.B)],
            VehicleCategoryEnum.Motorcycle));
    }

    [Fact]
    public void Check_Allows_A_Higher_Class_To_Cover_A_Lower_One()
    {
        Assert.Null(LicenseGate.Check(
            [Approved(ApprovalStatusEnum.Verified, LicenseCategoryEnum.A1)],
            VehicleCategoryEnum.Scooter));
    }

    [Fact]
    public void Check_Falls_Back_To_Another_Verified_License()
    {
        var licenses = new[]
        {
            Approved(ApprovalStatusEnum.Verified, LicenseCategoryEnum.K),
            Approved(ApprovalStatusEnum.Verified, LicenseCategoryEnum.A)
        };

        Assert.Null(LicenseGate.Check(licenses, VehicleCategoryEnum.Motorcycle));
    }
}
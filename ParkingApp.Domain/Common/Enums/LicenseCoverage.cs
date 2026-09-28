namespace ParkingApp.Domain.Common.Enums;

/// <summary>
/// Nepal DoTM license-to-vehicle coverage. A higher two-wheeler class
/// covers the lower ones (A covers scooter, K does not cover motorcycle);
/// no two-wheeler class covers four-wheelers and vice versa.
/// </summary>
public static class LicenseCoverage
{
    public static bool Covers(
        IEnumerable<LicenseCategoryEnum> held,
        VehicleCategoryEnum vehicle)
    {
        var set = held.ToHashSet();

        return vehicle switch
        {
            VehicleCategoryEnum.Scooter =>
                set.Contains(LicenseCategoryEnum.K)
                || set.Contains(LicenseCategoryEnum.A)
                || set.Contains(LicenseCategoryEnum.A1),
            VehicleCategoryEnum.Motorcycle =>
                set.Contains(LicenseCategoryEnum.A)
                || set.Contains(LicenseCategoryEnum.A1),
            VehicleCategoryEnum.CarJeepVan =>
                set.Contains(LicenseCategoryEnum.B),
            _ => false
        };
    }

    /// <summary>
    /// The vehicle size class must agree with the spot size class:
    /// scooters/motorcycles are two-wheelers, cars/jeeps/vans are four-wheelers.
    /// </summary>
    public static bool MatchesSpotType(
        VehicleTypeEnum vehicleType,
        VehicleCategoryEnum vehicleCategory)
        => (vehicleType, vehicleCategory) switch
        {
            (VehicleTypeEnum.TwoWheeler, VehicleCategoryEnum.Scooter) => true,
            (VehicleTypeEnum.TwoWheeler, VehicleCategoryEnum.Motorcycle) => true,
            (VehicleTypeEnum.FourWheeler, VehicleCategoryEnum.CarJeepVan) => true,
            _ => false
        };
}

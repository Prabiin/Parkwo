using ParkingApp.Application.Configuration;

namespace ParkingApp.Application.Common.Helpers;

/// <summary>
/// Area plausibility math for compliance: how many vehicles of a type fit
/// the declared land area, and whether claimed occupancy exceeds it.
/// Advisory only (driveways and layout vary) — compliance decides.
/// </summary>
public static class ParkingCapacity
{
    public static int? MaxFittable(decimal? landAreaSqM, decimal perVehicleSqM)
        => landAreaSqM is null
            ? null
            : (int)Math.Floor(landAreaSqM.Value / perVehicleSqM);

    public static decimal EstimatedAreaRequiredSqM(
        int twoWheelerOccupancy,
        int fourWheelerOccupancy,
        ParkingStandards standards)
        => twoWheelerOccupancy * standards.TwoWheelerAreaSqM
            + fourWheelerOccupancy * standards.FourWheelerAreaSqM;

    public static bool? ExceedsLandArea(
        int twoWheelerOccupancy,
        int fourWheelerOccupancy,
        decimal? landAreaSqM,
        ParkingStandards standards)
        => landAreaSqM is null
            ? null
            : EstimatedAreaRequiredSqM(twoWheelerOccupancy, fourWheelerOccupancy, standards)
                > landAreaSqM.Value;
}

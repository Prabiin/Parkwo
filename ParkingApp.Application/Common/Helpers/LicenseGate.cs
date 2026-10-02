using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Common.Helpers;

/// <summary>
/// Booking-time license gate. Vehicles can be registered without a license, so
/// this is the first point where a rider must prove they may drive the vehicle
/// they booked. All three conditions are required: verified by BackOffice, a
/// category that covers the vehicle, and not expired.
/// </summary>
public static class LicenseGate
{
    /// <summary>
    /// Returns null when the rider may book, otherwise the 403 reason.
    /// A rider with no license on file is blocked — unlike vehicle registration,
    /// where a license is only checked if one happens to exist.
    /// </summary>
    public static string? Check(
        IEnumerable<DrivingLicense> licenses,
        VehicleCategoryEnum vehicleCategory)
    {
        var verified = licenses
            .Where(l => l.ApprovalStatus == ApprovalStatusEnum.Verified)
            .ToList();

        if (verified.Count == 0)
            return "A verified driving license is required to book parking.";

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var unexpired = verified.Where(l => l.ExpiryDate >= today).ToList();

        if (unexpired.Count == 0)
            return "Your driving license has expired. Update it before booking.";

        if (unexpired.Any(l => LicenseCoverage.Covers(l.Categories, vehicleCategory)))
            return null;

        return $"Your license does not cover {vehicleCategory.ToDescription()}.";
    }
}
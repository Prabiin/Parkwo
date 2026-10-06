using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Common.Helpers;

/// <summary>
/// Prepaid pricing rules. Partial hours round up to the next whole hour, so a
/// rider cannot reserve 61 minutes for the price of one.
/// </summary>
public static class BookingPricing
{
    public const int MinBillableHours = 1;
    public const int MaxBillableHours = 24 * 7;

    /// <summary>Whole hours charged for a window, rounded up.</summary>
    public static int BillableHours(TimeSpan window)
    {
        if (window <= TimeSpan.Zero)
            return 0;

        var hours = (int)Math.Ceiling(window.TotalHours);

        return Math.Clamp(hours, MinBillableHours, MaxBillableHours);
    }

    public static decimal TotalNpr(decimal pricePerHourNpr, int billableHours)
        => pricePerHourNpr * billableHours;

    /// <summary>NPR rupees to the paisa integer gateways charge in.</summary>
    public static long ToPaisa(decimal amountNpr)
        => (long)Math.Round(amountNpr * 100m, MidpointRounding.AwayFromZero);

    /// <summary>Paisa back to NPR rupees, for anything a human reads.</summary>
    public static decimal ToNpr(long amountPaisa)
        => amountPaisa / 100m;

    /// <summary>
    /// The price for a vehicle type at a facility, or null when that type is
    /// not offered. Owners set both numbers together on the facility.
    /// </summary>
    public static decimal? PricePerHourFor(VehicleTypeEnum vehicleType, int occupancy, decimal pricePerHourNpr)
        => occupancy > 0 ? pricePerHourNpr : null;
}
namespace ParkingApp.Application.Configuration;

/// <summary>
/// The per-hour rates Parkwo itself sets. Providers never propose prices — the
/// marketplace rate is global, lives in configuration, and can be changed
/// (e.g. promos) without a migration or a booking rebuild. Bookings snapshot
/// the rate at create, so past bookings are never re-priced.
/// </summary>
public class ParkwoPricingSettings
{
    public const string SectionName = "ParkwoPricing";

    /// <summary>Per-hour price for scooters / motorcycles, in NPR.</summary>
    public decimal TwoWheelerPricePerHourNpr { get; set; } = 40m;

    /// <summary>Per-hour price for cars / jeeps / vans, in NPR.</summary>
    public decimal FourWheelerPricePerHourNpr { get; set; } = 100m;
}
namespace ParkingApp.Application.Configuration;

/// <summary>
/// Rider discovery tuning. The "near me" feed is no longer caller-tuned: the
/// radius is fixed here (small on purpose — nobody walks kilometres to park)
/// and a rider who wants another area searches for it by name instead of
/// widening the circle. Lives in configuration, like ParkwoPricing, so it can
/// be adjusted per environment without a rebuild.
/// </summary>
public class NearbySettings
{
    public const string SectionName = "Nearby";

    /// <summary>The shipped "near me" radius, and the fallback for a bad value.</summary>
    public const double DefaultNearMeRadiusKm = 1;

    /// <summary>Radius of the default "parkings near me" feed, in kilometres.</summary>
    public double NearMeRadiusKm { get; set; } = DefaultNearMeRadiusKm;
}

namespace ParkingApp.Application.Common.Helpers;

/// <summary>
/// Platform cut taken from a booking's gross amount. Flat NPR per booking for
/// now — a percentage model would need per-provider terms, which do not exist.
/// Stored on the booking so settlement never re-derives it from a moving rate.
/// </summary>
public static class PlatformFeeCalculator
{
    public const decimal FlatFeeNpr = 5m;

    public static decimal Calculate(decimal grossAmountNpr)
        => grossAmountNpr <= 0m ? 0m : FlatFeeNpr;
}
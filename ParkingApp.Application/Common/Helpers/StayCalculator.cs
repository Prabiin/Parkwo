namespace ParkingApp.Application.Common.Helpers;

/// <summary>
/// Stay measurement. Entry and exit are real timestamps taken at the gate, so
/// the measured stay can disagree with the window the rider prepaid in both
/// directions — early in, on time out; or a full window plus an overstay.
/// </summary>
public static class StayCalculator
{
    /// <summary>Whole minutes between entry and exit. Never negative.</summary>
    public static int StayMinutes(DateTimeOffset enteredAtUtc, DateTimeOffset exitedAtUtc)
    {
        var minutes = (int)Math.Floor((exitedAtUtc - enteredAtUtc).TotalMinutes);
        return Math.Max(0, minutes);
    }

    /// <summary>
    /// Minutes the vehicle held the space beyond the paid window. Zero when the
    /// rider leaves early or exactly on time.
    /// </summary>
    public static int OverstayMinutes(DateTimeOffset endsAtUtc, DateTimeOffset exitedAtUtc)
    {
        var minutes = (int)Math.Floor((exitedAtUtc - endsAtUtc).TotalMinutes);
        return Math.Max(0, minutes);
    }

    /// <summary>
    /// Whether the overstay is large enough to be worth telling staff about.
    /// The grace absorbs a slow exit; anything past it is flagged, still never
    /// charged automatically.
    /// </summary>
    public static bool IsOverstay(int overstayMinutes, int graceMinutes)
        => overstayMinutes > Math.Max(0, graceMinutes);
}

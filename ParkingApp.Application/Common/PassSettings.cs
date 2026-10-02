namespace ParkingApp.Application.Configuration;

public class PassSettings
{
    public const string SectionName = "Pass";

    /// <summary>
    /// HMAC key that signs gate passes. Distinct from the JWT secret on purpose:
    /// passes are handed to staff phones and read by scanners, and rotating one
    /// must never force staff to re-authenticate. Keep it out of source control.
    /// </summary>
    public string SigningKey { get; set; } = default!;

    /// <summary>
    /// Minutes before <c>StartsAtUtc</c> that entry is accepted, so a rider who
    /// arrives early is not turned away. Keeps the gate lenient in one direction
    /// only: leaving late is handled as overstay, not as a refusal.
    /// </summary>
    public int EarlyEntryGraceMinutes { get; set; } = 15;

    /// <summary>
    /// Minutes past <c>EndsAtUtc</c> that entry is still accepted. After this
    /// the pass is treated as lapsed and the rider must rebook, because holding
    /// a space indefinitely would let one booking block a lane forever.
    /// </summary>
    public int LateEntryGraceMinutes { get; set; } = 30;

    /// <summary>
    /// Extra minutes of unpaid parking tolerated before the gate reports
    /// overstay. Reported to staff and recorded, never auto-charged.
    /// </summary>
    public int OverstayGraceMinutes { get; set; } = 10;
}

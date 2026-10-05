namespace ParkingApp.Application.Configuration;

/// <summary>
/// Throttle limits for the OTP endpoints. The limiter itself is always enabled:
/// a limiter that only exists in production is never exercised locally, so
/// registration and middleware-ordering mistakes stay hidden until they matter.
/// Development instead gets the lenient "Dev" values below, which Dependency
/// Injection applies automatically via <c>IsDevelopment()</c>.
/// </summary>
public class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";

    /// <summary>Kill switch. Leave enabled; it exists for incident response.</summary>
    public bool Enabled { get; set; } = true;

    // --- Per client IP (fixed window) ---

    public int OtpSendPermitLimit { get; set; } = 5;
    public int OtpSendWindowMinutes { get; set; } = 15;
    public int OtpVerifyPermitLimit { get; set; } = 20;
    public int OtpVerifyWindowMinutes { get; set; } = 15;

    // --- Per phone number, enforced in the handler against the Otps table ---
    // IP limits cannot stop a botnet SMS-bombing one victim, so this is a
    // separate, database-backed counter.

    public int OtpSendPerPhoneLimit { get; set; } = 3;
    public int OtpSendPerPhoneWindowMinutes { get; set; } = 60;

    // --- Lenient values used in Development ---

    public int OtpSendDevPermitLimit { get; set; } = 100;
    public int OtpVerifyDevPermitLimit { get; set; } = 500;
    public int OtpSendPerPhoneDevLimit { get; set; } = 50;
}
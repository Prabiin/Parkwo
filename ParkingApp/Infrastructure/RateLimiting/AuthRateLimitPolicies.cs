namespace ParkingApp.Api.Infrastructure.RateLimiting;

public static class AuthRateLimitPolicies
{
    /// <summary>Per client IP. Guards against OTP flooding and SMS cost abuse.</summary>
    public const string OtpSend = "otp-send";

    /// <summary>Per client IP. Defence in depth; the Otp entity already caps
    /// failed verify attempts at 5 per issued code.</summary>
    public const string OtpVerify = "otp-verify";
}
namespace ParkingApp.Application.Configuration;

public class OtpSettings
{
    public const string SectionName = "Otp";

    /// <summary>
    /// Includes the one-time code in the send-otp response as "devCode" so
    /// testers can complete login without a working SMS gateway.
    /// Defaults to false. DependencyInjection forces this off whenever the
    /// host is not running in the Development environment, so the code can
    /// never leak from a misconfigured production deployment.
    /// </summary>
    public bool ExposeDevCode { get; set; }
}
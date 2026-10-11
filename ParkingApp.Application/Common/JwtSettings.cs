namespace ParkingApp.Application.Configuration;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = default!;
    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public int AccessTokenExpirationMinutes { get; set; } = 30;
    public int RefreshTokenExpirationDays { get; set; } = 30;

    /// <summary>
    /// Staff sessions are deliberately short. A staff device keeps its refresh
    /// token and unlocks it locally with a PIN or fingerprint, but the server
    /// only sees that token, so its lifetime is what forces the password to be
    /// re-typed roughly once a day. Never set this as long as the rider value:
    /// a month-long staff token would silently defeat the daily password.
    /// </summary>
    public int StaffRefreshTokenExpirationDays { get; set; } = 1;
}
namespace ParkingApp.Application.Configuration;

public class KhaltiSettings
{
    public const string SectionName = "Khalti";

    /// <summary>Secret key from the Khalti merchant dashboard. Never the public key.</summary>
    public string SecretKey { get; set; } = default!;

    public string PublicKey { get; set; } = default!;

    /// <summary>
    /// Base URL of the ePayment v2 API. Sandbox is https://dev.khalti.com/api/v2/,
    /// production is https://a.khalti.com/api/v2/ — override per environment rather
    /// than branching on a sandbox flag in code.
    /// </summary>
    public string BaseUrl { get; set; } = "https://dev.khalti.com/api/v2/";

    /// <summary>Absolute URL Khalti redirects the payer back to. Must be publicly reachable.</summary>
    public string ReturnUrl { get; set; } = default!;

    /// <summary>Khalti requires a website URL on every initiate call.</summary>
    public string WebsiteUrl { get; set; } = default!;
}
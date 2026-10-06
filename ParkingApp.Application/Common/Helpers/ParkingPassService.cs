using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ParkingApp.Application.Configuration;

namespace ParkingApp.Application.Common.Helpers;

/// <summary>
/// The QR pass a rider shows at the gate. Deliberately a signed, self-describing
/// token rather than a bare booking id: a scanner can validate it offline (bad
/// signal at a gate is the normal case), and a rider cannot mint a pass for
/// someone else's booking by editing a URL.
/// <para>
/// The server still re-checks everything on scan. Offline validation exists to
/// tell staff "this is a real pass for this lot, ask the rider to confirm",
/// not to skip the authoritative check.
/// </para>
/// </summary>
public sealed class ParkingPassService(IOptions<PassSettings> options)
{
    public const string Prefix = "PKP1";
    private const int MaxTokenLength = 512;

    /// <summary>
    /// Explains a refused pass request when no signing key is configured, so a
    /// support ticket says "this environment is missing a variable" instead of
    /// the rider being told their valid pass is forged.
    /// </summary>
    public const string NotConfiguredMessage =
        "Gate passes are not configured on this server. "
        + "Set Pass__SigningKey in the environment and retry.";

    private readonly PassSettings _settings = options.Value;

    /// <summary>
    /// False when no signing key is configured. Handlers check this and return
    /// <see cref="NotConfiguredMessage"/> rather than crashing, so a deployment
    /// missing a secret comes up with every other feature working and one clear
    /// diagnostic to act on.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_settings.SigningKey);

    public sealed record PassClaims(
        Guid BookingId,
        Guid FacilityId,
        Guid PassNonce,
        long IssuedAtUnix,
        long ExpiresAtUnix);

    public sealed record Validity(DateTimeOffset From, DateTimeOffset Until);

    /// <summary>
    /// The window a pass for this booking is valid in: opens with the early-entry
    /// grace, closes with the late-entry grace. Shared by pass issuing and
    /// rotation so a fresh code can never be handed out with a different window
    /// than the one the rider was already told about.
    /// </summary>
    public Validity ValidityFor(DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc)
        // Both graces come from configuration (Pass:EarlyEntryGraceMinutes,
        // Pass:LateEntryGraceMinutes).
        => new(
            startsAtUtc.AddMinutes(-Math.Max(0, _settings.EarlyEntryGraceMinutes)),
            endsAtUtc.AddMinutes(Math.Max(0, _settings.LateEntryGraceMinutes)));

    /// <summary>
    /// Mints a pass valid from <paramref name="validFrom"/> to the end of the
    /// paid window plus the late-entry grace.
    /// </summary>
    public string Issue(
        Guid bookingId,
        Guid facilityId,
        Guid passNonce,
        DateTimeOffset validFrom,
        DateTimeOffset validUntil)
    {
        if (!IsConfigured)
            throw new InvalidOperationException(NotConfiguredMessage);

        var claims = new PassClaims(
            bookingId,
            facilityId,
            passNonce,
            validFrom.ToUnixTimeSeconds(),
            validUntil.ToUnixTimeSeconds());

        var payload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(claims));
        var signature = Sign($"{Prefix}.{payload}");

        return $"{Prefix}.{payload}.{signature}";
    }

    /// <summary>
    /// Verifies signature and time window. Returns null for anything that is not
    /// a well-formed, correctly signed, currently valid pass — deliberately
    /// collapsing every failure into one result so callers cannot accidentally
    /// leak which check failed.
    /// </summary>
    public PassClaims? Verify(string? token, DateTimeOffset nowUtc)
    {
        if (TryRead(token, out var claims, out _) && claims is not null)
        {
            var now = nowUtc.ToUnixTimeSeconds();

            // Both bounds, so a pass only ever works inside the window the rider
            // was shown. Callers that want a specific "too early" or "too late"
            // answer should use VerifySignatureOnly and apply GateRules, which
            // produce a message worth showing a gate attendant.
            if (now >= claims.IssuedAtUnix && now <= claims.ExpiresAtUnix)
                return claims;
        }

        return null;
    }

    /// <summary>
    /// Checks signature and shape but ignores the time window. Only the exit
    /// scan uses this: a rider whose window has already elapsed is exactly the
    /// person who must be let out of the lot.
    /// </summary>
    public PassClaims? VerifySignatureOnly(string? token)
        => TryRead(token, out var claims, out _) ? claims : null;

    /// <summary>
    /// True when the token carries a valid signature but its time window has
    /// passed. The gate uses this to say "expired" rather than "forged", which
    /// is the difference between a rebook and a support ticket.
    /// </summary>
    public bool IsExpired(string? token, DateTimeOffset nowUtc)
        => TryRead(token, out var claims, out var signatureValid)
           && signatureValid
           && claims is not null
           && nowUtc.ToUnixTimeSeconds() > claims.ExpiresAtUnix;

    private bool TryRead(string? token, out PassClaims? claims, out bool signatureValid)
    {
        claims = null;
        signatureValid = false;

        // Fail closed rather than signing or verifying with an empty key. A pass
        // produced without a real secret would be forgeable by anyone who guessed
        // the (empty) key, which is worse than no passes at all.
        if (!IsConfigured)
            return false;

        if (string.IsNullOrWhiteSpace(token) || token.Length > MaxTokenLength)
            return false;

        var parts = token.Split('.');
        if (parts.Length != 3 || parts[0] != Prefix)
            return false;

        signatureValid = FixedTimeEquals(Sign($"{Prefix}.{parts[1]}"), parts[2]);
        if (!signatureValid)
            return false;

        try
        {
            claims = JsonSerializer.Deserialize<PassClaims>(Base64UrlDecode(parts[1]));
        }
        catch (JsonException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }

        return claims is not null && claims.BookingId != Guid.Empty && claims.FacilityId != Guid.Empty;
    }

    private string Sign(string value)
        // SigningKey from Pass:SigningKey. Signing never runs with an empty key —
        // IsConfigured is checked by every public entry point first.
        => Base64UrlEncode(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(_settings.SigningKey),
            Encoding.UTF8.GetBytes(value)));

    private static bool FixedTimeEquals(string left, string right)
    {
        var a = Encoding.UTF8.GetBytes(left);
        var b = Encoding.UTF8.GetBytes(right);

        // Length is not secret here — every valid signature is 256 bits — and
        // FixedTimeEquals requires equal lengths, so a mismatch is simply false.
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = (padded.Length % 4) switch
        {
            2 => padded + "==",
            3 => padded + "=",
            0 => padded,
            _ => throw new FormatException("Invalid base64url length.")
        };

        return Convert.FromBase64String(padded);
    }
}

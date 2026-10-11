namespace ParkingApp.Application.Common.Helpers;

/// <summary>
/// Canonical form for Nepali mobile numbers, shared by walk-in creation and the
/// exit-by-phone lookup so a staff member typing "98 1234 5678" and one typing
/// "+9779812345678" resolve to the same stored key. Deliberately only shapes the
/// string — it says nothing about who owns the number, which the walk-in design
/// never claims to know.
/// </summary>
public static class PhoneNumbers
{
    /// <summary>Digits only, country code dropped: "+977 98-1234-5678" -> "9812345678".</summary>
    public static string Normalize(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        var digits = new string(phone.Where(char.IsDigit).ToArray());

        // Strip a leading 977 (Nepal) when the subscriber part is the full ten
        // digits, so +977-98… and 98… land on one value.
        if (digits.Length == 13 && digits.StartsWith("977", StringComparison.Ordinal))
            digits = digits[3..];

        return digits;
    }

    /// <summary>
    /// A plausible Nepali mobile subscriber number: ten digits, leading 9. Shape
    /// only, so it catches typos without pretending to verify ownership.
    /// </summary>
    public static bool IsValidMobile(string? phone)
    {
        var digits = Normalize(phone);
        return digits.Length == 10 && digits[0] == '9';
    }
}

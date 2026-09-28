using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Common.Helpers;

/// <summary>
/// Parses the multipart "categories" field: comma-separated
/// <see cref="LicenseCategoryEnum"/> codes (e.g. "2,4" — see GET /licenses/init).
/// Returns null when the field is missing or holds anything else.
/// </summary>
public static class LicenseCategoryParser
{
    public static List<LicenseCategoryEnum>? Parse(string raw)
    {
        var parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return null;

        var parsed = new List<LicenseCategoryEnum>(parts.Length);
        foreach (var part in parts)
        {
            if (!int.TryParse(part, out var code)
                || !Enum.IsDefined(typeof(LicenseCategoryEnum), code))
                return null;

            parsed.Add((LicenseCategoryEnum)code);
        }

        return parsed.Distinct().ToList();
    }
}

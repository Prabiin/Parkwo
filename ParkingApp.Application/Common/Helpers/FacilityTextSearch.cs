namespace ParkingApp.Application.Common.Helpers;

/// <summary>
/// Blunt, predictable text matching for rider discovery. Normalisation is
/// deliberately crude — lowercase and remove every space — because Kathmandu
/// place names are written every which way ("Durbarmarg", "Durbar Marg",
/// "durbarmarg"), and a rider should not have to guess the owner's spacing.
/// The query normalises here; the database column normalises the same way in
/// the nearby query, and both sides meet at a plain substring test.
/// </summary>
public static class FacilityTextSearch
{
    /// <summary>Below this a "search" is not selective enough to be useful.</summary>
    public const int MinQueryLength = 2;

    /// <summary>Above this it is not a search term.</summary>
    public const int MaxQueryLength = 50;

    /// <summary>True when the rider typed something worth searching for.</summary>
    public static bool HasQuery(string? query) => !string.IsNullOrWhiteSpace(query);

    /// <summary>
    /// Lowercase, trimmed, every space removed — the single shape both the query
    /// and the stored name/address are compared in. Applied to the search term
    /// here, and to the columns in SQL (the "lower" and "replace" below must
    /// stay in lockstep with this method).
    /// </summary>
    public static string Normalize(string value)
        => value.Trim().ToLowerInvariant().Replace(" ", string.Empty);

    /// <summary>
    /// Match quality for ordering search results, best first:
    /// 0 = the name starts with the term, 1 = the name contains it,
    /// 2 = only the address contains it (the caller's WHERE guarantees a match).
    /// </summary>
    public static int Rank(string name, string normalizedQuery)
    {
        var normalizedName = Normalize(name);

        if (normalizedName.StartsWith(normalizedQuery, StringComparison.Ordinal))
            return 0;

        if (normalizedName.Contains(normalizedQuery, StringComparison.Ordinal))
            return 1;

        return 2;
    }
}

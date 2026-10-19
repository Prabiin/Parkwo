using ParkingApp.Application.Common.Helpers;

namespace ParkingApp.Tests;

/// <summary>
/// Rider search matching: bold enough that "Durbarmarg" finds "Durbar Marg",
/// predictable enough that a rider's own "%" or "_" can never turn into a
/// wildcard. The search term normalises here while the stored columns normalise
/// in SQL, so this is also the contract the nearby query's "lower/replace" copy
/// has to agree with.
/// </summary>
public class FacilityTextSearchTests
{
    [Theory]
    [InlineData("Durbar Marg", "durbarmarg")]
    [InlineData("durbarmarg", "durbarmarg")]
    [InlineData("DURBARMARG", "durbarmarg")]
    [InlineData("  Durbar   Marg  ", "durbarmarg")]
    [InlineData("Gwarko, Lalitpur", "gwarko,lalitpur")]
    [InlineData("", "")]
    public void Normalize_Lowercases_And_Removes_Every_Space(string input, string expected)
        => Assert.Equal(expected, FacilityTextSearch.Normalize(input));

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("a", true)]
    [InlineData(" Boudha ", true)]
    public void HasQuery_Only_True_When_Something_Was_Typed(string? query, bool expected)
        => Assert.Equal(expected, FacilityTextSearch.HasQuery(query));

    [Theory]
    [InlineData("Durbarmarg Parking", "durbarmarg", 0)] // name starts with the term
    [InlineData("New Durbarmarg Lot", "durbarmarg", 1)] // name contains the term
    [InlineData("durbar marg parking", "durbarmarg", 0)] // spacing ignored
    [InlineData("DURBAR MARG", "durbarmarg", 0)] // case ignored
    [InlineData("Everest Mall", "durbarmarg", 2)] // not in the name — matched via the address
    public void Rank_Leads_With_The_Best_Name_Match(string name, string query, int expected)
        => Assert.Equal(expected, FacilityTextSearch.Rank(name, query));
}

using ParkingApp.Application.Common.Helpers;

namespace ParkingApp.Tests;

/// <summary>
/// Phone handling for the walk-in exit fallback. Staff type numbers by hand, so
/// the canonical form has to be forgiving about formatting while staying strict
/// about shape — and it must never imply the number was verified.
/// </summary>
public class PhoneNumbersTests
{
    [Theory]
    [InlineData("9800000000", "9800000000")]
    [InlineData("98 0000 0000", "9800000000")]
    [InlineData("+977 9800000000", "9800000000")]
    [InlineData("977-980-000-0000", "9800000000")]
    [InlineData("+9779800000000", "9800000000")]
    [InlineData("0812345678", "0812345678")]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    public void Normalize_Lands_Formatting_On_One_Value(string? input, string expected)
        => Assert.Equal(expected, PhoneNumbers.Normalize(input));

    [Theory]
    [InlineData("9800000000", true)]
    [InlineData("+977 9812345678", true)]
    [InlineData("0812345678", false)]
    [InlineData("98000000", false)]
    [InlineData("98000000000", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidMobile_Checks_Shape_Not_Ownership(string? input, bool expected)
        => Assert.Equal(expected, PhoneNumbers.IsValidMobile(input));
}

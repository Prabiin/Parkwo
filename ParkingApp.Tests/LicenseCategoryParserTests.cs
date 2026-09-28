using ParkingApp.Application.Common.Helpers;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Tests;

public class LicenseCategoryParserTests
{
    [Fact]
    public void Parses_CommaSeparated_Codes()
    {
        var result = LicenseCategoryParser.Parse("2,4");

        Assert.Equal(
            new[] { LicenseCategoryEnum.A, LicenseCategoryEnum.B },
            result);
    }

    [Fact]
    public void Trims_Spaces_And_Dedupes()
    {
        var result = LicenseCategoryParser.Parse(" 2, 2 , 4 ");

        Assert.Equal(
            new[] { LicenseCategoryEnum.A, LicenseCategoryEnum.B },
            result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("X")]
    [InlineData("2,X")]
    [InlineData("99")]
    [InlineData("0")]
    public void Returns_Null_For_Missing_Or_Invalid(string raw)
    {
        Assert.Null(LicenseCategoryParser.Parse(raw));
    }
}

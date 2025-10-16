using LeakChecker.Content.Detection.ItemParsing;

namespace LeakProcessor.Tests.Content.Detection.ItemParsing;

public class TimeStampParserTests
{
    [Theory]
    // Unix Timestamps (Seconds)
    [InlineData("946684800", true)]          // 2000-01-01
    [InlineData("1609459200", true)]         // 2021-01-01
    [InlineData("1700000000", true)]         // 2023-11-14
    [InlineData("1893456000", true)]         // 2030-01-01

    // Unix Timestamps (Milliseconds)
    [InlineData("946684800000", true)]       // 2000-01-01
    [InlineData("1609459200000", true)]      // 2021-01-01
    [InlineData("1700000000000", true)]      // 2023-11-14
    [InlineData("1893456000000", true)]      // 2030-01-01

    // Windows FILETIME (100-ns since 1601)
    [InlineData("125911584000000000", true)] // 2000-01-01
    [InlineData("132537600000000000", true)] // 2021-01-01
    [InlineData("133401024000000000", true)] // 2023-11-14
    [InlineData("134774400000000000", true)] // 2028-01-01
    [InlineData("135694656000000000", true)] // 2030-12-31

    // .NET Ticks (100-ns since year 1)
    [InlineData("630822816000000000", true)] // 2000-01-01
    [InlineData("637450560000000000", true)] // 2021-01-01
    [InlineData("638545344000000000", true)] // 2023-11-14
    [InlineData("639838464000000000", true)] // 2028-01-01
    [InlineData("640883328000000000", true)] // 2031-01-01

    // Excel Serial Dates (days since 1899-12-30)   // TODO test or delete Excel feature from TimeStampParser
    // [InlineData("36526", true)]              // 2000-01-01
    // [InlineData("44197", true)]              // 2021-01-01
    // [InlineData("45200", true)]              // 2023-11-14
    // [InlineData("47483", true)]              // 2030-01-01
    // [InlineData("47845", true)]              // 2031-01-01
        
    // Negative test cases
    [InlineData("2145916800", false)]        // Date too high for unix seconds 2038-01-01
    [InlineData("2145916800000", false)]     // Date too high for unix milliseconds 2038-01-01
    [InlineData("-1", false)]                // Negative
    [InlineData("1", false)]                 // Date too low for all
    [InlineData("99999999999999999999", false)] // Date too high
    [InlineData("Dat@T1m@", false)]          // Non-numeric
    [InlineData("Hakuna matata", false)]     // Non-numeric
    public void TryParse_ShouldValidateVariousTimestampFormats(string input, bool expected)
    {
        // Act
        var result = TimeStampParser.TryParse(input, out var parsed);

        // Assert
        Assert.Equal(expected, result);

        if (expected)
        {
            Assert.InRange(parsed, new DateTime(2000, 1, 1), DateTime.UtcNow.AddYears(10));
        }
        else
        {
            Assert.Equal(default, parsed);
        }
    }
}
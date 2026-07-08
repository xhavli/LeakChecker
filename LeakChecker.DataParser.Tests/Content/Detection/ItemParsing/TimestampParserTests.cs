using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Content.Detection.ItemParsing;

namespace LeakChecker.DataParser.Tests.Content.Detection.ItemParsing;

public class TimestampParserTests
{
    // Unix Timestamps (Seconds)
    [Theory]
    [InlineData("946684800", ItemType.UnixSeconds)]          // 2000-01-01
    [InlineData("1609459200", ItemType.UnixSeconds)]         // 2021-01-01
    [InlineData("1700000000", ItemType.UnixSeconds)]         // 2023-11-14
    [InlineData("1893456000", ItemType.UnixSeconds)]         // 2030-01-01
    public void TryParse_ShouldParseUnixSeconds(string input, ItemType expected)
    {
        // Act
        _ = TimestampParser.TryParse(input, out ItemType result, out _);

        // Assert
        Assert.Equal(expected, result);
    }

    // Unix Timestamps (Milliseconds)
    [Theory]
    [InlineData("946684800000", ItemType.UnixMilliseconds)]       // 2000-01-01
    [InlineData("1609459200000", ItemType.UnixMilliseconds)]      // 2021-01-01
    [InlineData("1700000000000", ItemType.UnixMilliseconds)]      // 2023-11-14
    [InlineData("1893456000000", ItemType.UnixMilliseconds)]      // 2030-01-01
    public void TryParse_ShouldParseUnixMilliseconds(string input, ItemType expected)
    {
        // Act
        _ = TimestampParser.TryParse(input, out ItemType result, out _);

        // Assert
        Assert.Equal(expected, result);
    }

    // Windows FILETIME (100-ns since 1601)
    [Theory]
    [InlineData("125911584000000000", ItemType.FileTime)] // 2000-01-01
    [InlineData("132537600000000000", ItemType.FileTime)] // 2021-01-01
    [InlineData("133401024000000000", ItemType.FileTime)] // 2023-11-14
    [InlineData("134774400000000000", ItemType.FileTime)] // 2028-01-01
    [InlineData("135694656000000000", ItemType.FileTime)] // 2030-12-31
    public void TryParse_ShouldParseFileTime(string input, ItemType expected)
    {
        // Act
        _ = TimestampParser.TryParse(input, out ItemType result, out _);

        // Assert
        Assert.Equal(expected, result);
    }

    // .NET Ticks (100-ns since year 1)
    [Theory]
    [InlineData("630822816000000000", ItemType.NetTicks)] // 2000-01-01
    [InlineData("637450560000000000", ItemType.NetTicks)] // 2021-01-01
    [InlineData("638545344000000000", ItemType.NetTicks)] // 2023-11-14
    [InlineData("639838464000000000", ItemType.NetTicks)] // 2028-01-01
    [InlineData("640883328000000000", ItemType.NetTicks)] // 2031-01-01
    public void TryParse_ShouldParseNetTicks(string input, ItemType expected)
    {
        // Act
        _ = TimestampParser.TryParse(input, out ItemType result, out _);

        // Assert
        Assert.Equal(expected, result);
    }
        
    // Negative test cases
    [Theory]
    [InlineData("2145916800", false)]        // Date too high for unix seconds 2038-01-01
    [InlineData("2145916800000", false)]     // Date too high for unix milliseconds 2038-01-01
    [InlineData("-1", false)]                // Negative
    [InlineData("1", false)]                 // Date too low for all
    [InlineData("99999999999999999999", false)] // Date too high
    [InlineData("Dat@T1m@", false)]          // Non-numeric
    [InlineData("Hakuna matata", false)]     // Non-numeric
    public void TryParse_ShouldNotParseTimestamps(string input, bool expected)
    {
        // Act
        var result = TimestampParser.TryParse(input, out _, out _);

        // Assert
        Assert.Equal(expected, result);
    }
}
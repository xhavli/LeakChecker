using LeakChecker.Content.Detection.ItemRecognition;

namespace LeakProcessor.Tests.Content.Detection.ItemRecognition;

public class TimestampRecognizerTests
{
    [Theory]
    // Dates with dots
    [InlineData("15.10.2025", "15.10.2025")]
    [InlineData("01.01.2000", "01.01.2000")]
    [InlineData("31.12.1999", "31.12.1999")]
    public void TryRecognize_ShouldFindDotDates(string input, string expectedText)
    {
        // Act
        var ok = TimestampRecognizer.TryRecognize(input, out List<string> foundTexts, out List<DateTime> times);

        Assert.True(ok);
        Assert.Contains(expectedText, foundTexts);
        Assert.NotEmpty(times);
    }

    [Theory]
    // Dates with slashes
    [InlineData("10/15/2025", "10/15/2025")]
    [InlineData("1/1/2000", "1/1/2000")]
    [InlineData("12/31/1999", "12/31/1999")]
    public void TryRecognize_ShouldFindSlashDates(string input, string expectedText)
    {
        // Act
        var ok = TimestampRecognizer.TryRecognize(input, out List<string> foundTexts, out List<DateTime> times);

        Assert.True(ok);
        Assert.Contains(expectedText, foundTexts);
        Assert.NotEmpty(times);
    }
        
    [Theory]
    // Dates with dashes or spaces
    [InlineData("2025-10-15", "2025-10-15")]
    [InlineData("2023 11 14", "2023 11 14")]
    [InlineData("2000-01-01", "2000-01-01")]
    public void TryRecognize_ShouldFindDashedOrSpacedDates(string input, string expectedText)
    {
        // Act
        var ok = TimestampRecognizer.TryRecognize(input, out List<string> foundTexts, out List<DateTime> times);

        Assert.True(ok);
        Assert.Contains(expectedText, foundTexts);
        Assert.NotEmpty(times);
    }

    [Theory]
    // Day and month only (no year)
    [InlineData("7/2", "7/2")]
    [InlineData("23/5", "23/5")]
    [InlineData("12/9", "12/9")]
    public void TryRecognize_ShouldFindDayMonthDates(string input, string expectedText)
    {
        // Act
        var ok = TimestampRecognizer.TryRecognize(input, out List<string> stringTimeStamps, out List<DateTime> dts);

        Assert.True(ok);
        Assert.Contains(expectedText, stringTimeStamps);
        Assert.NotEmpty(dts);
    }

    [Theory]
    // Full birthdates (day, month, year)
    [InlineData("Born on 15.10.1990", "15.10.1990")]
    [InlineData("Date of birth: 01/01/1985", "01/01/1985")]
    [InlineData("My birthday is 31-12-2000", "31-12-2000")]
    public void TryRecognize_ShouldFindBirthDates(string input, string expectedText)
    {
        // Act
        var ok = TimestampRecognizer.TryRecognize(input, out List<string> stringTimeStamps, out List<DateTime> dts);

        Assert.True(ok);
        Assert.Contains(expectedText, stringTimeStamps);
        Assert.NotEmpty(dts);
    }

    [Theory]
    // Times in various formats
    [InlineData("17:45", "17:45")]
    [InlineData("23:59", "23:59")]
    [InlineData("11:30 PM", "11:30 PM")]
    [InlineData("06:45:20", "06:45:20")]
    [InlineData("6am", "6am")]
    public void TryRecognize_ShouldFindTimes(string input, string expectedText)
    {
        // Act
        var ok = TimestampRecognizer.TryRecognize(input, out List<string> stringTimeStamps, out List<DateTime> dts);

        Assert.True(ok);
        Assert.Contains(expectedText, stringTimeStamps, StringComparer.OrdinalIgnoreCase);
        Assert.NotEmpty(dts);
    }

    [Theory]
    // Full timestamps (date + time)
    [InlineData(":2025-10-15 14:30:", "2025-10-15 14:30")]
    [InlineData("15-10-2025 23:59:59", "15-10-2025 23:59:59")]
    [InlineData("2023-11-14T22:13:20", "2023-11-14T22:13:20")]
    [InlineData("| 26-2-2025 21:58:39 |", "26-2-2025 21:58:39")]
    [InlineData("some 2023-11-14 22:13:20 text", "2023-11-14 22:13:20")]
    [InlineData("10/15/2025 2:30 PM", "10/15/2025 2:30 PM")]
    [InlineData("15.10.2025 14:30", "15.10.2025 14:30")]
    public void TryRecognize_ShouldFindFullTimestamps(string input, string expectedText)
    {
        // Act
        var ok = TimestampRecognizer.TryRecognize(input, out List<string> stringTimeStamps, out List<DateTime> dts);

        Assert.True(ok);
        Assert.Contains(expectedText, stringTimeStamps, StringComparer.OrdinalIgnoreCase);
        Assert.NotEmpty(dts);
    }
        
    [Theory]
    [InlineData("11 January", "11 January")]
    [InlineData("29 February", "29 February")]  
    [InlineData("5 July", "5 July")]
    [InlineData("17 September", "17 September")]
    [InlineData("31 December", "31 December")]
    [InlineData("First of October 2016", "First of October 2016")]
    public void TryRecognize_ShouldFindTextualDayMonthDates(string input, string expectedText)
    {
        // Act
        var ok = TimestampRecognizer.TryRecognize(input, out List<string> stringTimeStamps, out List<DateTime> dts);

        Assert.True(ok, $"Recognizer should find a textual date in: {input}");
        Assert.Contains(expectedText, stringTimeStamps, StringComparer.InvariantCultureIgnoreCase);
        Assert.NotEmpty(dts);
    }
    
    [Theory]
    // 2 timestamps - with and without text
    [InlineData("blahblah 2025/10/15 text 2025/10/16 text", new[] { "2025/10/15", "2025/10/16" })]
    [InlineData("15.10.2025 14:30 16.10.2025 10:00", new[] { "15.10.2025 14:30", "16.10.2025 10:00" })]
    [InlineData("15.10.2025 at 14:30, then 16.10.2025 10:00", new[] { "15.10.2025 at 14:30", "16.10.2025 10:00" })]
    [InlineData("Random words 31-12-2025 23:59 and 1-1-2026 00:00", new[] { "31-12-2025 23:59", "1-1-2026 00:00" })]
    [InlineData("first:10/15/2025 2:30 PM second:10/16/2025 5:45 AM", new[] { "10/15/2025 2:30 PM", "10/16/2025 5:45 AM" })]
    [InlineData("start 2023-11-14T22:13:20 end 2025-01-01T00:00:00", new[] { "2023-11-14T22:13:20", "2025-01-01T00:00:00" })]
    [InlineData("Meeting 5 July 2025 14:30 and again 6 July 2025 09:00", new[] { "5 July 2025 14:30", "6 July 2025 09:00" })]
    [InlineData("Some text 17 September 2024 another 18 September 2024", new[] { "17 September 2024", "18 September 2024" })]
    [InlineData("We met 11 January 2024 6am then 12 January 2024 7pm", new[] { "11 January 2024 6am", "12 January 2024 7pm" })]
    [InlineData("text 2025-10-15 14:30 some words 2025-10-16 18:45 more text", new[] { "2025-10-15 14:30", "2025-10-16 18:45" })]
    
    // 3 timestamps - with and without text
    [InlineData("2023-11-14T22:13:20 something 2023-11-15T22:13:20 something 2023-11-16T22:13:20", new[] { "2023-11-14T22:13:20", "2023-11-15T22:13:20", "2023-11-16T22:13:20" })]
    public void TryRecognize_ShouldFindMultipleTimestamps(string input, string[] expectedTexts)
    {
        // Act
        var ok = TimestampRecognizer.TryRecognize(input, out List<string> stringTimeStamps, out List<DateTime> dts);

        // Assert
        Assert.True(ok, $"Recognizer should detect multiple timestamps in: {input}");
        Assert.Equal(expectedTexts.Length, dts.Count);
        Assert.Equal(expectedTexts.Length, stringTimeStamps.Count);
        foreach (var expected in expectedTexts)
            Assert.Contains(expected, stringTimeStamps, StringComparer.OrdinalIgnoreCase);
    }

    [Theory]
    // Negative test cases
    [InlineData("6541961")]
    [InlineData("d@te t1m#")]
    [InlineData("Hakuna matata")]
    [InlineData("32.13.2025")]           // Invalid day/month
    [InlineData("99/99/9999")]           // Out of range
    [InlineData("15:99")]                // Invalid minutes
    [InlineData("61:72")]                // Invalid hour
    [InlineData("abc.def.ghij")]         // Letters
    [InlineData("----")]                 // Dashes only
    [InlineData("time: none")]           // Text
    [InlineData("1.2.")]                 // Incomplete
    [InlineData("2025/14/15")]           // Invalid month
    [InlineData("182/431/99999")]        // Too many digits
    public void TryRecognize_ShouldRejectInvalidFormats(string input)
    {
        // Act
        var ok = TimestampRecognizer.TryRecognize(input, out List<string> stringTimeStamps, out List<DateTime> dts);

        Assert.False(ok, $"Recognizer should NOT detect a valid TimeStamp in: {input}");
        Assert.True(stringTimeStamps == null || stringTimeStamps.Count == 0, "No timestamp text should be returned.");
        Assert.True(dts == null || dts.Count == 0, "No DateTime values should be parsed.");
    }
}
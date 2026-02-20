using System.Text;
using LeakChecker.DataParser.Content.Parsing;
using LeakChecker.DataParser.Format;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Tests.Logging.Helpers.Parse;

namespace LeakChecker.DataParser.Tests.Content.Parsing.ContentParserTests.FormatDetectionTests;

public class MixedFormatDetectionTests
{
    private readonly IParseLogger _logger = new NullParseLogger(string.Empty);
    private readonly string _testDataDirectory;
        
    public MixedFormatDetectionTests()
    {
        // *\LeakChecker\LeakChecker.DataParser\bin\Release\net0.0
        DirectoryInfo? dir = new DirectoryInfo(Environment.CurrentDirectory);
        // *\LeakChecker
        dir = dir.Parent?.Parent?.Parent?.Parent;
        _testDataDirectory = Path.Combine(dir!.FullName, "LeakChecker.DataParser.Tests/Data/FormatMixed");
    }
        
    [Theory]
    [InlineData("SqlThenCsv_Clean.txt")]
    [InlineData("SqlThenCsv_Messy.txt")]
    public async Task ShouldDetect_SqlThenCsv(string fileName)
    {
        // Arrange
        string filePath = Path.Combine(_testDataDirectory, fileName);
        var stats = NullParseStats.Create(Guid.Empty, _logger, filePath);
        using var parser = await ContentParser.CreateAsync(filePath, _logger, stats, Encoding.UTF8, thresholdPercent: 50);

        // Act
        await parser.ProcessFile();

        // Assert
        Assert.Equal(2, stats.Formats.Count);
        Assert.Equal(FormatEnum.SqlInsert, stats.Formats[0]);
        Assert.Equal(FormatEnum.Csv, stats.Formats[1]);
    }
}
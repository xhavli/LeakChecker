using LeakChecker.DataParser.Content.Parsing;
using LeakChecker.DataParser.Format;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Tests.Logging.Helpers.Parse;

namespace LeakChecker.DataParser.Tests.Content.Parsing.ContentParserTests.FormatDetectionTests;

public class MixedFormatDetectionTests
{
    private readonly IParseLogger _logger = new NullParseLogger();
    private readonly string _testDataDirectory;
        
    public MixedFormatDetectionTests()
    {
        string projectDir = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.Parent?.FullName!;
        _testDataDirectory = Path.Combine(projectDir, "LeakChecker.DataParser.Tests/Data/FormatMixed");
    }
        
    [Theory]
    [InlineData("SqlThenCsv_Clean.txt")]
    [InlineData("SqlThenCsv_Messy.txt")]
    public async Task ShouldDetect_SqlThenCsv(string fileName)
    {
        // Arrange
        string filePath = Path.Combine(_testDataDirectory, fileName);
        var stats = NullParseStats.Create(Guid.Empty, _logger, filePath);
        using var parser = new ContentParser(filePath, _logger, stats, schemaThreshold: 50);

        // Act
        await parser.ParseFile();

        // Assert
        Assert.Equal(2, stats.Formats.Count);
        Assert.Equal(FormatEnum.SqlInsert, stats.Formats[0]);
        Assert.Equal(FormatEnum.Csv, stats.Formats[1]);
    }
}
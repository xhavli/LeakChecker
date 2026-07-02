using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Content.Parse;
using LeakChecker.DataParser.Helpers.Settings;
using LeakChecker.DataParser.Tests.Helpers.AppBuilder;
using LeakChecker.DataParser.Tests.Helpers.Logging.Parse;
using LeakChecker.DataParser.Tests.Helpers.Stats;
using Microsoft.Extensions.DependencyInjection;

namespace LeakChecker.DataParser.Tests.Content.Parse.ContentParserTests.FormatDetectionTests;

public class MixedFormatDetectionTests
{
    private readonly NullParseLogger _logger = new();
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
        NullParseStats stats = new NullParseStats();
        using var host = LeakCheckerApplicationFactory.CreateHost();
        var config = host.Services.GetRequiredService<ISettings>();

        using var parser = new ContentParser(filePath, _logger, stats, config);

        // Act
        await parser.ParseAsync();

        // Assert
        Assert.Equal(2, stats.Formats.Count);
        Assert.Equal(FormatType.SqlInsert, stats.Formats[0]);
        Assert.Equal(FormatType.Csv, stats.Formats[1]);
    }
    
    [Theory]
    [InlineData("CsvThenSql_Clean.txt")]
    [InlineData("CsvThenSql_Messy.txt")]
    public async Task ShouldDetect_CsvThenSql(string fileName)
    {
        // Arrange
        string filePath = Path.Combine(_testDataDirectory, fileName);
        NullParseStats stats = new NullParseStats();
        using var host = LeakCheckerApplicationFactory.CreateHost();
        var config = host.Services.GetRequiredService<ISettings>();

        using var parser = new ContentParser(filePath, _logger, stats, config);

        // Act
        await parser.ParseAsync();

        // Assert
        Assert.Equal(2, stats.Formats.Count);
        Assert.Equal(FormatType.Csv, stats.Formats[0]);
        Assert.Equal(FormatType.SqlInsert, stats.Formats[1]);
    }
    
    [Theory]
    [InlineData("SqlThenSql_Clean.txt")]
    [InlineData("SqlThenSql_Messy.txt")]
    public async Task ShouldDetect_SqlThenSql(string fileName)
    {
        // Arrange
        string filePath = Path.Combine(_testDataDirectory, fileName);
        NullParseStats stats = new NullParseStats();
        using var host = LeakCheckerApplicationFactory.CreateHost();
        var config = host.Services.GetRequiredService<ISettings>();

        using var parser = new ContentParser(filePath, _logger, stats, config);

        // Act
        await parser.ParseAsync();

        // Assert
        Assert.Equal(2, stats.Formats.Count);
        Assert.Equal(FormatType.SqlInsert, stats.Formats[0]);
        Assert.Equal(FormatType.SqlInsert, stats.Formats[1]);
    }
}
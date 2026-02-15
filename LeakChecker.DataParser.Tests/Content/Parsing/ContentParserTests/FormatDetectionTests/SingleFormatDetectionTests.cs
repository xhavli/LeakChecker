using System.Text;
using LeakChecker.DataParser.Content.Parsing;
using LeakChecker.DataParser.Format;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Tests.Logging.Helpers.Parse;

namespace LeakChecker.DataParser.Tests.Content.Parsing.ContentParserTests.FormatDetectionTests;

public class SingleFormatDetectionTests
{
    private readonly IParseLogger _logger = new NullParseLogger(string.Empty);
    private readonly string _testDataDirectory;
        
    public SingleFormatDetectionTests()
    {
        // *\LeakChecker\LeakChecker.DataParser\bin\Release\net0.0
        DirectoryInfo? dir = new DirectoryInfo(Environment.CurrentDirectory);
        // *\LeakChecker
        dir = dir.Parent?.Parent?.Parent?.Parent;
        _testDataDirectory = Path.Combine(dir!.FullName, "LeakChecker.DataParser.Tests/Data/SingleFormat");
    }
        
    [Theory]
    [InlineData("Csv/Colon_Clean.txt")]
    [InlineData("Csv/Colon_Messy.txt")]
    [InlineData("Csv/Comma_Clean.txt")]
    [InlineData("Csv/Comma_Messy.txt")]
    [InlineData("Csv/Pipe_Clean.txt")]
    [InlineData("Csv/Pipe_Messy.txt")]
    [InlineData("Csv/Semicolon_Clean.txt")]
    [InlineData("Csv/Semicolon_Messy.txt")]
    [InlineData("Csv/Space_Clean.txt")]
    [InlineData("Csv/Space_Messy.txt")]
    [InlineData("Csv/Tab_Clean.txt")]
    [InlineData("Csv/Tab_Messy.txt")]
    public async Task ShouldDetect_SingleCsv(string fileName)
    {
        // Arrange
        string filePath = Path.Combine(_testDataDirectory, fileName);
        var stats = NullParseStats.Create(Guid.Empty, _logger, filePath);
        using var parser = await ContentParser.CreateAsync(filePath, _logger, stats, Encoding.UTF8, thresholdPercent: 50);

        // Act
        await parser.ProcessFile();

        // Assert
        Assert.Single(stats.Formats);
        Assert.Single(stats.Delimiters);
        Assert.Equal(FormatEnum.Csv, stats.Formats.First());
    }
    
    [Theory]
    [InlineData("SqlInsert/MySQL_Clean.txt")]
    [InlineData("SqlInsert/MySQL_Messy.txt")]
    [InlineData("SqlInsert/PostgreSQL_Clean.txt")]
    [InlineData("SqlInsert/PostgreSQL_Messy.txt")]
    [InlineData("SqlInsert/SQLServer_Clean.txt")]
    [InlineData("SqlInsert/SQLServer_Messy.txt")]
    public async Task ShouldDetect_SingleSqlInsert(string fileName)
    {
        // Arrange
        string filePath = Path.Combine(_testDataDirectory, fileName);
        var stats = NullParseStats.Create(Guid.Empty, _logger, filePath);
        using var parser = await ContentParser.CreateAsync(filePath, _logger, stats, Encoding.UTF8, thresholdPercent: 50);

        // Act
        await parser.ProcessFile();

        // Assert
        Assert.Single(stats.Formats);
        Assert.Single(stats.Delimiters);
        Assert.Equal(FormatEnum.SqlInsert, stats.Formats.First());
    }
}
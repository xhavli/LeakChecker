using System.Text;
using LeakChecker.DataParser.Content.Parsing;
using LeakChecker.DataParser.Format;
using LeakChecker.DataParser.Tests.Logging.Helpers.Parse;

namespace LeakChecker.DataParser.Tests.Content.Parsing.ContentParserTests.FormatDetectionTests;

public class SingleFormatDetectionTests
{
    private readonly NullParseLogger _logger = new();
    private readonly string _testDataDirectory;
        
    public SingleFormatDetectionTests()
    {
        string projectDir = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.Parent?.FullName!;
        _testDataDirectory = Path.Combine(projectDir, "LeakChecker.DataParser.Tests/Data/FormatSingle");
        
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);  //TODO make test base for this
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
        NullParseStats stats = new NullParseStats();
        using var parser = new ContentParser(filePath, _logger, stats, schemaThreshold: 50);

        // Act
        await parser.ParseFile();

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
        NullParseStats stats = new NullParseStats();
        using var parser = new ContentParser(filePath, _logger, stats, schemaThreshold: 50);

        // Act
        await parser.ParseFile();

        // Assert
        Assert.Single(stats.Formats);
        Assert.Single(stats.Delimiters);
        Assert.Equal(FormatEnum.SqlInsert, stats.Formats.First());
    }
    
    [Theory]
    [InlineData("Excel/Single_Sheet_Clean.xlsx")]
    public async Task ShouldDetect_SingleExcel(string fileName)
    {
        // Arrange
        string filePath = Path.Combine(_testDataDirectory, fileName);
        NullParseStats stats = new NullParseStats();

        // Act
        await ExcelParser.ParseFile(filePath, _logger, stats, thresholdPercent: 50);

        // Assert
        Assert.Single(stats.Formats);
        Assert.Equal(FormatEnum.Excel, stats.Formats.First());
    }
    
    [Theory]
    [InlineData("AsciiTable/AsciiTable.txt")]
    public async Task ShouldDetect_SingleAsciiTable(string fileName)
    {
        // Arrange
        string filePath = Path.Combine(_testDataDirectory, fileName);
        NullParseStats stats = new NullParseStats();
        using var parser = new ContentParser(filePath, _logger, stats, schemaThreshold: 50);

        // Act
        await parser.ParseFile();

        // Assert
        Assert.Single(stats.Formats);
        Assert.Single(stats.Delimiters);
        Assert.Equal(FormatEnum.AsciiTable, stats.Formats.First());
    }
}
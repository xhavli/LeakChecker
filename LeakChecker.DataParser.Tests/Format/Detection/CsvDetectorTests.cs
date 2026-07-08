using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Content;
using LeakChecker.DataParser.Format.Detection;
using LeakChecker.DataParser.Tests.Helpers.Logging.Parse;
using LeakChecker.DataParser.Tests.Helpers.Settings;
using LeakChecker.DataParser.Tests.Helpers.Stats;

namespace LeakChecker.DataParser.Tests.Format.Detection;

public class CsvDetectorTests
{
    private readonly string _testDataDirectory;
    private readonly NullSettings _settings = new();
    private readonly NullParseLogger _logger = new();
    
    public CsvDetectorTests()
    {
        string projectDir = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.Parent?.FullName!;
        _testDataDirectory = Path.Combine(projectDir, "LeakChecker.DataParser.Tests/Data/FormatSingle");
    }
        
    [Theory]
    [InlineData("Csv/Colon_Clean.txt", ':')]
    [InlineData("Csv/Colon_Messy.txt", ':')]
    [InlineData("Csv/Semicolon_Clean.txt", ';')]
    [InlineData("Csv/Semicolon_Messy.txt", ';')]
    [InlineData("Csv/Tab_Clean.txt", '\t')]
    [InlineData("Csv/Tab_Messy.txt", '\t')]
    public async Task ShouldDetect_ExpectedCsvSchema(string fileName, char delimiter)
    {
        // Arrange
        Dictionary<int, ItemType> expected = new()
        {
            { 0, ItemType.Username },
            { 1, ItemType.Name },
            { 2, ItemType.Gender },
            { 3, ItemType.Timestamp },
            { 4, ItemType.Location },
            { 5, ItemType.Ipv4 },
            { 6, ItemType.Email },
            { 7, ItemType.Password },
        };
        string filePath = Path.Combine(_testDataDirectory, fileName);
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(stream);
        NullParseStats stats = new NullParseStats();
        ParsingContext detectionContext = new ParsingContext
        {
            Reader = reader,
            Logger = _logger,
            Stats = stats,
            Settings = _settings,
            Delimiter = delimiter,
            StartLine = 0,
        };

        // Act
        var result = await CsvDetector.DetectSchema(detectionContext);

        // Assert
        Assert.Equal(expected, result);
    }
    
    [Theory]
    [InlineData("Csv/Space_Clean.txt", ' ')]
    [InlineData("Csv/Space_Messy.txt", ' ')]
    public async Task ShouldDetect_ExpectedCsvSchemaWithSpaceDelimiter(string fileName, char delimiter)
    {
        // Arrange
        Dictionary<int, ItemType> expected = new()
        {
            { 0, ItemType.Gender },
            { 1, ItemType.Timestamp },
            { 2, ItemType.Location },
            { 3, ItemType.Ipv4 },
            { 4, ItemType.Email },
        };
        string filePath = Path.Combine(_testDataDirectory, fileName);
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(stream);
        NullParseStats stats = new NullParseStats();
        ParsingContext detectionContext = new ParsingContext
        {
            Reader = reader,
            Logger = _logger,
            Stats = stats,
            Settings = _settings,
            Delimiter = delimiter,
            StartLine = 0,
        };

        // Act
        var result = await CsvDetector.DetectSchema(detectionContext);

        // Assert
        Assert.Equal(expected, result);
    }
    
    [Theory]
    [InlineData("Csv/Pipe_Clean.txt", '|')]
    [InlineData("Csv/Pipe_Messy.txt", '|')]
    public async Task ShouldDetect_ExpectedCsvSchemaWithPipeDelimiter(string fileName, char delimiter)
    {
        // Arrange
        Dictionary<int, ItemType> expected = new()
        {
            { 0, ItemType.Gender },
            { 1, ItemType.Ipv4 },
        };
        string filePath = Path.Combine(_testDataDirectory, fileName);
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(stream);
        NullParseStats stats = new NullParseStats();
        ParsingContext detectionContext = new ParsingContext
        {
            Reader = reader,
            Logger = _logger,
            Stats = stats,
            Settings = _settings,
            Delimiter = delimiter,
            StartLine = 0,
        };

        // Act
        var result = await CsvDetector.DetectSchema(detectionContext);

        // Assert
        Assert.Equal(expected, result);
    }
    
    [Theory]
    [InlineData("Csv/Comma_Clean.txt", ',')]
    [InlineData("Csv/Comma_Messy.txt", ',')]
    public async Task ShouldDetect_ExpectedCsvSchemaWithCommaDelimiter(string fileName, char delimiter)
    {
        // Arrange
        Dictionary<int, ItemType> expected = new()
        {
            { 0, ItemType.PhoneNumber },
            { 1, ItemType.Timestamp },
        };
        string filePath = Path.Combine(_testDataDirectory, fileName);
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(stream);
        NullParseStats stats = new NullParseStats();
        ParsingContext detectionContext = new ParsingContext
        {
            Reader = reader,
            Logger = _logger,
            Stats = stats,
            Settings = _settings,
            Delimiter = delimiter,
            StartLine = 0,
        };

        // Act
        var result = await CsvDetector.DetectSchema(detectionContext);

        // Assert
        Assert.Equal(expected, result);
    }
}
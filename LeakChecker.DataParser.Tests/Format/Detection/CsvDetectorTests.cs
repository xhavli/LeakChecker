using LeakChecker.DataParser.Content;
using LeakChecker.DataParser.Format.Detection;
using LeakChecker.DataParser.Tests.Helpers.Logging.Parse;
using LeakChecker.DataParser.Tests.Helpers.Stats;

namespace LeakChecker.DataParser.Tests.Format.Detection;

public class CsvDetectorTests
{
    private const int CsvSamplesLimit = 103;
    private const int ThresholdPercent = 50;
    private readonly string _testDataDirectory;
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
        Dictionary<int, ItemEnum> expected = new()
        {
            { 0, ItemEnum.Username },
            { 1, ItemEnum.Name },
            { 2, ItemEnum.Gender },
            { 3, ItemEnum.Timestamp },
            { 4, ItemEnum.Location },
            { 5, ItemEnum.Ipv4 },
            { 6, ItemEnum.Email },
            { 7, ItemEnum.Password },
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
            Delimiter = delimiter,
            StartLine = 0,
            SamplesLimit = CsvSamplesLimit,
            Threshold = ThresholdPercent,
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
        Dictionary<int, ItemEnum> expected = new()
        {
            { 0, ItemEnum.Gender },
            { 1, ItemEnum.Timestamp },
            { 2, ItemEnum.Location },
            { 3, ItemEnum.Ipv4 },
            { 4, ItemEnum.Email },
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
            Delimiter = delimiter,
            StartLine = 0,
            SamplesLimit = CsvSamplesLimit,
            Threshold = ThresholdPercent,
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
        Dictionary<int, ItemEnum> expected = new()
        {
            { 0, ItemEnum.Gender },
            { 1, ItemEnum.Ipv4 },
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
            Delimiter = delimiter,
            StartLine = 0,
            SamplesLimit = CsvSamplesLimit,
            Threshold = ThresholdPercent,
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
        Dictionary<int, ItemEnum> expected = new()
        {
            { 0, ItemEnum.PhoneNumber },
            { 1, ItemEnum.Timestamp },
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
            Delimiter = delimiter,
            StartLine = 0,
            SamplesLimit = CsvSamplesLimit,
            Threshold = ThresholdPercent,
        };

        // Act
        var result = await CsvDetector.DetectSchema(detectionContext);

        // Assert
        Assert.Equal(expected, result);
    }
}
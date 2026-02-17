using LeakChecker.DataParser.Content;
using LeakChecker.DataParser.Format.Detection;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Tests.Logging.Helpers.Parse;

namespace LeakChecker.DataParser.Tests.Format.Detection;

public class CsvDetectorTests
{
    private const int CsvSamplesLimit = 103;
    private const int ThresholdPercent = 50;
    private readonly string _testDataDirectory;
    private readonly IParseLogger _logger = new NullParseLogger(string.Empty);
    
    private static readonly Dictionary<int, ItemEnum> CsvSchema = new()
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

    public CsvDetectorTests()
    {
        // *\LeakChecker\LeakChecker.DataParser.Tests\bin\Release\net0.0
        DirectoryInfo? dir = new DirectoryInfo(Environment.CurrentDirectory);
        // *\LeakChecker
        dir = dir.Parent?.Parent?.Parent?.Parent;
        _testDataDirectory = Path.Combine(dir!.FullName, "LeakChecker.DataParser.Tests/Data/SingleFormat");
    }
        
    [Theory]
    [InlineData("Csv/Colon_Clean.txt", ':')]
    [InlineData("Csv/Colon_Messy.txt", ':')]
    [InlineData("Csv/Comma_Clean.txt", ',')]
    [InlineData("Csv/Comma_Messy.txt", ',')]
    [InlineData("Csv/Pipe_Clean.txt", '|')]
    [InlineData("Csv/Pipe_Messy.txt", '|')]
    [InlineData("Csv/Semicolon_Clean.txt", ';')]
    [InlineData("Csv/Semicolon_Messy.txt", ';')]
    [InlineData("Csv/Tab_Clean.txt", '\t')]
    [InlineData("Csv/Tab_Messy.txt", '\t')]
    public async Task ShouldDetect_ExpectedCsvSchema(string fileName, char delimiter)
    {
        // Arrange
        string filePath = Path.Combine(_testDataDirectory, fileName);
        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var streamReader = new StreamReader(fileStream);

        // Act
        var schema = await CsvFileDetector.DetectFormat(0, delimiter, streamReader, _logger, CsvSamplesLimit, ThresholdPercent);

        // Assert
        Assert.Equal(CsvSchema, schema);
    }
    
    [Theory]
    [InlineData("Csv/Space_Clean.txt", ' ')]
    [InlineData("Csv/Space_Messy.txt", ' ')]
    public async Task ShouldDetect_ExpectedCsvSchemaWithSpaceDelimiter(string fileName, char delimiter)
    {
        // Arrange
        Dictionary<int, ItemEnum> schema = new()
        {
            { 0, ItemEnum.Gender },
            { 1, ItemEnum.Timestamp },
            { 2, ItemEnum.Location },
            { 3, ItemEnum.Ipv4 },
            { 4, ItemEnum.Email },
        };
        string filePath = Path.Combine(_testDataDirectory, fileName);
        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var streamReader = new StreamReader(fileStream);

        // Act
        var result = await CsvFileDetector.DetectFormat(0, delimiter, streamReader, _logger, CsvSamplesLimit, ThresholdPercent);

        // Assert
        Assert.Equal(schema, result);
    }
}
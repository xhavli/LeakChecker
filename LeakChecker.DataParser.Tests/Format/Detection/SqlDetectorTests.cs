using LeakChecker.DataParser.Content;
using LeakChecker.DataParser.Format.Detection;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Tests.Logging.Helpers.Parse;

namespace LeakChecker.DataParser.Tests.Format.Detection;

public class SqlDetectorTests
{
    private const int SqlSamplesLimit = 31;
    private const int ThresholdPercent = 50;
    private readonly string _testDataDirectory;
    private readonly NullParseLogger _logger = new();
    
    private static readonly Dictionary<int, ItemEnum> SqlSchema = new()
    {
        { 0, ItemEnum.Id },
        { 1, ItemEnum.Name },
        { 2, ItemEnum.Gender },
        { 3, ItemEnum.Timestamp },
        { 4, ItemEnum.Location },
        { 5, ItemEnum.Ipv4 },
        { 6, ItemEnum.Email },
        { 7, ItemEnum.Password },
    };

    public SqlDetectorTests()
    {
        string projectDir = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.Parent?.FullName!;
        _testDataDirectory = Path.Combine(projectDir, "LeakChecker.DataParser.Tests/Data/FormatSingle");
    }
    
    [Theory]
    [InlineData("SqlInsert/MySQL_Clean.txt")]
    [InlineData("SqlInsert/MySQL_Messy.txt")]
    [InlineData("SqlInsert/PostgreSQL_Clean.txt")]
    [InlineData("SqlInsert/PostgreSQL_Messy.txt")]
    [InlineData("SqlInsert/SQLServer_Clean.txt")]
    [InlineData("SqlInsert/SQLServer_Messy.txt")]
    public async Task ShouldDetect_ExpectedSqlSchema(string fileName)
    {
        // Arrange
        string filePath = Path.Combine(_testDataDirectory, fileName);
        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var streamReader = new StreamReader(fileStream);
        NullParseStats stats = new NullParseStats();
        ParsingContext detectionContext = new ParsingContext
        {
            Reader = streamReader,
            Logger = _logger,
            Stats = stats,
            StartLine = 0,
            SamplesLimit = SqlSamplesLimit,
            Threshold = ThresholdPercent,
        };

        // Act
        var schema = await SqlInsertDetector.DetectSchema(detectionContext);

        // Assert
        Assert.Equal(SqlSchema, schema);
    }
}
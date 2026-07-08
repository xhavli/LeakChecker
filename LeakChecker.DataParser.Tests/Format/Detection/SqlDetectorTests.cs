using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Content;
using LeakChecker.DataParser.Format.Detection;
using LeakChecker.DataParser.Tests.Helpers.Logging.Parse;
using LeakChecker.DataParser.Tests.Helpers.Settings;
using LeakChecker.DataParser.Tests.Helpers.Stats;

namespace LeakChecker.DataParser.Tests.Format.Detection;

public class SqlDetectorTests
{
    private readonly string _testDataDirectory;
    private readonly NullSettings _settings = new();
    private readonly NullParseLogger _logger = new();
    
    private static readonly Dictionary<int, ItemType> SqlSchema = new()
    {
        { 0, ItemType.Id },
        { 1, ItemType.Name },
        { 2, ItemType.Gender },
        { 3, ItemType.Timestamp },
        { 4, ItemType.Location },
        { 5, ItemType.Ipv4 },
        { 6, ItemType.Email },
        { 7, ItemType.Password },
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
            Settings = _settings,
            StartLine = 0,
        };

        // Act
        var schema = await SqlInsertDetector.DetectSchema(detectionContext);

        // Assert
        Assert.Equal(SqlSchema, schema);
    }
}
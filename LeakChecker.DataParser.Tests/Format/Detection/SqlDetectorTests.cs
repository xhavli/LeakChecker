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
    private readonly IParseLogger _logger = new NullParseLogger(string.Empty);
    
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
        // *\LeakChecker\LeakChecker.DataParser.Tests\bin\Release\net0.0
        DirectoryInfo? dir = new DirectoryInfo(Environment.CurrentDirectory);
        // *\LeakChecker
        dir = dir.Parent?.Parent?.Parent?.Parent;
        _testDataDirectory = Path.Combine(dir!.FullName, "LeakChecker.DataParser.Tests/Data/FormatSingle");
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

        // Act
        var schema = await SqlInsertDetector.DetectFormat(0, streamReader, _logger, SqlSamplesLimit, ThresholdPercent);

        // Assert
        Assert.Equal(SqlSchema, schema);
    }
}
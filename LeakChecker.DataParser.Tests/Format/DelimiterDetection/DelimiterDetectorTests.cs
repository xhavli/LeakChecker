using LeakChecker.DataParser.Format.Detection;

namespace LeakChecker.DataParser.Tests.Format.DelimiterDetection;

public class DelimiterDetectorTests
{
    private readonly string _testDataDirectory;
    private static readonly char[] SqlDelimiters = [',', ' '];

    public DelimiterDetectorTests()
    {
        // *\LeakChecker\LeakChecker.DataParser\bin\Release\net0.0
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
    [InlineData("Csv/Space_Clean.txt", ' ')]
    [InlineData("Csv/Space_Messy.txt", ' ')]
    [InlineData("Csv/Tab_Clean.txt", '\t')]
    [InlineData("Csv/Tab_Messy.txt", '\t')]
    public async Task ShouldDetect_CsvSpecifiedDelimiter(string fileName, char expectedDelimiter)
    {
        // Arrange
        string filePath = Path.Combine(_testDataDirectory, fileName);
        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var streamReader = new StreamReader(fileStream);

        // Act
        var delimiterResult = DelimiterHeuristic.Analyze(streamReader);

        // Assert
        Assert.Equal(expectedDelimiter, delimiterResult.BestDelimiter);
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
        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var streamReader = new StreamReader(fileStream);

        // Act
        var delimiterResult = DelimiterHeuristic.Analyze(streamReader);

        // Assert
        Assert.NotNull(delimiterResult.BestDelimiter);
        Assert.Contains(delimiterResult.BestDelimiter.Value, SqlDelimiters);
    }
}
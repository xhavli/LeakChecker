using System.Text;
using LeakChecker.DataParser.Content;
using LeakChecker.DataParser.Format.Detection;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Tests.Logging.Helpers.Parse;

namespace LeakChecker.DataParser.Tests.Format.Detection;

public class ExcelDetectorTests
{
    private const int SamplesLimit = 23;
    private const int ThresholdPercent = 50;
    private readonly string _testDataDirectory;
    private readonly IParseLogger _logger = new NullParseLogger(string.Empty);

    private static readonly Dictionary<int, ItemEnum> Schema = new()
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
    private static readonly Dictionary<int, Dictionary<int, ItemEnum>> ExcelSchema = new()
    {
        { 1, Schema }
    };

    public ExcelDetectorTests()
    {
        // *\LeakChecker\LeakChecker.DataParser.Tests\bin\Release\net0.0
        DirectoryInfo? dir = new DirectoryInfo(Environment.CurrentDirectory);
        // *\LeakChecker
        dir = dir.Parent?.Parent?.Parent?.Parent;
        _testDataDirectory = Path.Combine(dir!.FullName, "LeakChecker.DataParser.Tests/Data/SingleFormat");

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);  //TODO make test base for this
    }
    
    [Theory]
    [InlineData("Excel/Single_Sheet_Clean.xlsx")]
    public async Task ShouldDetect_ExpectedExcelSchema(string fileName)
    {
        // Arrange
        string filePath = Path.Combine(_testDataDirectory, fileName);
        
        // Act
        Dictionary<int, Dictionary<int, ItemEnum>> schema = 
            await ExcelDetector.DetectFormat(0, filePath, _logger, SamplesLimit, ThresholdPercent);

        // Assert
        Assert.Equal(ExcelSchema, schema);
    }
}
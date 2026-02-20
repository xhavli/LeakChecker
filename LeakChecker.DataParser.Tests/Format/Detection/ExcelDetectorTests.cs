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

    public ExcelDetectorTests()
    {
        // *\LeakChecker\LeakChecker.DataParser.Tests\bin\Release\net0.0
        DirectoryInfo? dir = new DirectoryInfo(Environment.CurrentDirectory);
        // *\LeakChecker
        dir = dir.Parent?.Parent?.Parent?.Parent;
        _testDataDirectory = Path.Combine(dir!.FullName, "LeakChecker.DataParser.Tests/Data");

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);  //TODO make test base for this
    }
    
    [Theory]
    [InlineData("FormatSingle/Excel/Single_Sheet_Clean.xlsx")]
    public async Task ShouldDetect_ExpectedExcelSchema(string fileName)
    {
        // Arrange
        Dictionary<int, ItemEnum> schema = new()
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
        Dictionary<int, Dictionary<int, ItemEnum>> expectedSchema = new()
        {
            { 1, schema }
        };
        
        string filePath = Path.Combine(_testDataDirectory, fileName);
        
        // Act
        Dictionary<int, Dictionary<int, ItemEnum>> result = 
            await ExcelDetector.DetectFormat(0, filePath, _logger, SamplesLimit, ThresholdPercent);

        // Assert
        Assert.Equal(expectedSchema, result);
    }
    
    [Theory]
    [InlineData("FormatMixed/Excel_Two_Sheets.xlsx")]
    public async Task ShouldDetect_ExpectedExcelTwoSchemas(string fileName)
    {
        // Arrange
        Dictionary<int, ItemEnum> firstSheet = new()
        {
            { 0, ItemEnum.Ipv4 },
            { 1, ItemEnum.Email },
            { 2, ItemEnum.Password },
            { 3, ItemEnum.Name },
        };
        Dictionary<int, ItemEnum> secondSheet = new()
        {
            { 0, ItemEnum.Empty },
            { 1, ItemEnum.Email },
            { 2, ItemEnum.Ipv4 },
            { 3, ItemEnum.Password },
        };
        Dictionary<int, Dictionary<int, ItemEnum>> expectedSchema = new()
        {
            { 1, firstSheet },
            { 2, secondSheet }
        };
        string filePath = Path.Combine(_testDataDirectory, fileName);
        
        // Act
        Dictionary<int, Dictionary<int, ItemEnum>> result = 
            await ExcelDetector.DetectFormat(0, filePath, _logger, SamplesLimit, ThresholdPercent);

        // Assert
        Assert.Equal(expectedSchema, result);
    }
}
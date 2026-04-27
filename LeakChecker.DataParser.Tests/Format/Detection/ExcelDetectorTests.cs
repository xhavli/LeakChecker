using System.Text;
using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Format.Detection;
using LeakChecker.DataParser.Tests.Helpers.Logging.Parse;
using LeakChecker.DataParser.Tests.Helpers.Settings;

namespace LeakChecker.DataParser.Tests.Format.Detection;

public class ExcelDetectorTests
{
    private readonly string _testDataDirectory;
    private readonly NullSettings _settings = new();
    private readonly NullParseLogger _logger = new();

    public ExcelDetectorTests()
    {
        string projectDir = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.Parent?.FullName!;
        _testDataDirectory = Path.Combine(projectDir, "LeakChecker.DataParser.Tests/Data");

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
        Dictionary<int, Dictionary<int, ItemEnum>> result = await ExcelDetector.DetectFormat(filePath, _logger, _settings);

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
        Dictionary<int, Dictionary<int, ItemEnum>> result = await ExcelDetector.DetectFormat(filePath, _logger, _settings);

        // Assert
        Assert.Equal(expectedSchema, result);
    }
}
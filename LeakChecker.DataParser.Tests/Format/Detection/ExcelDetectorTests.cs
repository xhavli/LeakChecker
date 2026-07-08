using System.Text;
using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Format.Detection;
using LeakChecker.DataParser.Tests.Helpers.Logging.Parse;
using LeakChecker.DataParser.Tests.Helpers.Settings;
using LeakChecker.DataParser.Tests.Helpers.Stats;

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
        Dictionary<int, ItemType> schema = new()
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
        
        Dictionary<int, Dictionary<int, ItemType>> expectedSchema = new()
        {
            { 1, schema }
        };
        
        NullParseStats stats = new NullParseStats();
        string filePath = Path.Combine(_testDataDirectory, fileName);
        
        // Act
        Dictionary<int, Dictionary<int, ItemType>> result = await ExcelDetector.DetectFormat(filePath, _logger, stats, _settings);

        // Assert
        Assert.Equal(expectedSchema, result);
    }
    
    [Theory]
    [InlineData("FormatMixed/Excel_Two_Sheets.xlsx")]
    public async Task ShouldDetect_ExpectedExcelTwoSchemas(string fileName)
    {
        // Arrange
        Dictionary<int, ItemType> firstSheet = new()
        {
            { 0, ItemType.Ipv4 },
            { 1, ItemType.Email },
            { 2, ItemType.Password },
            { 3, ItemType.Name },
        };
        
        Dictionary<int, ItemType> secondSheet = new()
        {
            { 0, ItemType.Empty },
            { 1, ItemType.Email },
            { 2, ItemType.Ipv4 },
            { 3, ItemType.Password },
        };
        
        Dictionary<int, Dictionary<int, ItemType>> expectedSchema = new()
        {
            { 1, firstSheet },
            { 2, secondSheet }
        };
        
        NullParseStats stats = new NullParseStats();
        string filePath = Path.Combine(_testDataDirectory, fileName);
        
        // Act
        Dictionary<int, Dictionary<int, ItemType>> result = await ExcelDetector.DetectFormat(filePath, _logger, stats, _settings);

        // Assert
        Assert.Equal(expectedSchema, result);
    }
}
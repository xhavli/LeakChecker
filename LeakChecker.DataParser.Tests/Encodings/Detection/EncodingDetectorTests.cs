using System.Text;
using LeakChecker.DataParser.Encodings;
using LeakChecker.DataParser.Encodings.Detection;
using LeakChecker.DataParser.Tests.Helpers.Logging.Parse;
using LeakChecker.DataParser.Tests.Helpers.Stats;

namespace LeakChecker.DataParser.Tests.Encodings.Detection;

public class EncodingDetectorTests
{
    private readonly string _testDataDirectory;

    public EncodingDetectorTests()
    {
        string projectDir = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.Parent?.FullName!;
        _testDataDirectory = Path.Combine(projectDir, "LeakChecker.DataParser.Tests/Data/EncodingSingle");

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }
    
    [Theory]
    [InlineData("ascii", 20127)]
    [InlineData("gb18030", 54936)]
    [InlineData("iso2022jp", 50220)]
    [InlineData("shift_jis", 932)]
    // [InlineData("utf7", 65000)]  //TODO how JD do that
    [InlineData("utf8",  65001)]
    [InlineData("utf16le", 1200)]
    [InlineData("utf16be", 1201)]
    [InlineData("utf32le", 12000)]
    [InlineData("utf32be", 12001)]
    public async Task ShouldDetect_ExpectedEncoding(string fileName, int codePage)
    {
        // Arrange
        string filePath = Path.Combine(_testDataDirectory, fileName);
        NullParseStats stats = new NullParseStats();
        NullParseLogger logger = new NullParseLogger();
        EncodingDetector encodingDetector = new(filePath, logger, stats);

        Encoding expected = Encoding.GetEncoding(codePage);

        // Act
        List<EncodingSegment> result = await encodingDetector.DetectEncodingSegments();

        // Assert
        Assert.Single(result);
        
        try
        {
            Assert.Equal(expected, result.First().Encoding);
        }
        catch when (codePage is 1200 or 1201 or 12000 or 12001) // UTF-16 and UTF-32
        {
            Assert.Equal(Encoding.ASCII, result.First().Encoding);
        }
    }
}
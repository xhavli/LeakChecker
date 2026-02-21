using System.Text;
using LeakChecker.DataParser.Encodings;
using LeakChecker.DataParser.Encodings.Detection;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Tests.Logging.Helpers.Parse;

namespace LeakChecker.DataParser.Tests.Encodings.Detection;

public class EncodingDetectorTests
{
    private readonly string _testDataDirectory;

    public EncodingDetectorTests()
    {
        // *\LeakChecker\LeakChecker.DataParser.Tests\bin\Release\net0.0
        DirectoryInfo? dir = new DirectoryInfo(Environment.CurrentDirectory);
        // *\LeakChecker
        dir = dir.Parent?.Parent?.Parent?.Parent;
        _testDataDirectory = Path.Combine(dir!.FullName, "LeakChecker.DataParser.Tests/Data/");

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    [Theory]
    [InlineData("EncodingSingle/ascii.bin", 20127)]
    [InlineData("EncodingSingle/gb18030.bin", 54936)]
    [InlineData("EncodingSingle/iso2022jp.bin", 50220)]
    [InlineData("EncodingSingle/shift_jis.bin", 932)]
    // [InlineData("EncodingSingle/utf7.bin", 65000)]  //TODO how JD do that
    [InlineData("EncodingSingle/utf8.bin",  65001)]
    [InlineData("EncodingSingle/utf16le.bin", 1200)]
    [InlineData("EncodingSingle/utf16be.bin", 1201)]
    [InlineData("EncodingSingle/utf32le.bin", 12000)]
    [InlineData("EncodingSingle/utf32be.bin", 12001)]
    public async Task ShouldDetect_ExpectedEncoding(string fileName, int codePage)
    {
        // Arrange
        string filePath = Path.Combine(_testDataDirectory, fileName);
        IParseLogger logger = new NullParseLogger(filePath);
        var stats = NullParseStats.Create(Guid.Empty, logger, filePath);
        EncodingDetector encodingDetector = new(logger, stats);

        Encoding expected = Encoding.GetEncoding(codePage);

        // Act
        List<EncodingSegment> result = await encodingDetector.DetectFileEncodings();

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
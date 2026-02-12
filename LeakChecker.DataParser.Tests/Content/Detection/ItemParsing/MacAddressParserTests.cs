using System.Net.NetworkInformation;
using LeakChecker.DataParser.Content.Detection.ItemParsing;

namespace LeakChecker.DataParser.Tests.Content.Detection.ItemParsing;

public class MacAddressParserTests
{
    [Theory]
    // Positive test cases
    [InlineData("00:11:22:33:44:55")]
    [InlineData("00-11-22-33-44-55")]
    [InlineData("0011.2233.4455")]      // Cisco style
    [InlineData("00 11 22 33 44 55")]   // spaces
    [InlineData("001122334455")]        // no separators
    [InlineData("0a:1b:2c:3d:4e:5f")]   // lowercase hex
    [InlineData("0A-1B-2C-3D-4E-5F")]   // uppercase hex, hyphens
    public void TryParse_ShouldParseValidMacAddresses(string input)
    {
        var ok = MacAddressParser.TryParse(input, out PhysicalAddress mac);

        Assert.True(ok, $"Should parse valid MAC address: {input}");
        Assert.NotNull(mac);
        Assert.NotEqual(PhysicalAddress.None, mac);
        Assert.Equal(6, mac.GetAddressBytes().Length);
    }

    [Theory]
    // Negative test cases
    [InlineData("00:11:22:33:44")]          // too short
    [InlineData("00:11:22:33:44:55:66")]    // too long
    [InlineData("GG:11:22:33:44:55")]       // invalid hex
    [InlineData("0011.2233.445")]           // wrong Cisco grouping
    [InlineData("00:11:22:33:44:5G")]       // invalid trailing hex
    [InlineData("")]                        // empty
    [InlineData(" ")]                       // whitespace
    [InlineData("00:11:22:33:44:5")]        // short octet
    [InlineData("0011223344")]              // too short plain hex
    [InlineData("08137e51edc9d3bf54fd051e3d91bd471c93a240")]    // SHA1 hash
    public void TryParse_ShouldRejectInvalidMacAddresses(string input)
    {
        var ok = MacAddressParser.TryParse(input, out PhysicalAddress mac);

        Assert.False(ok, $"Should NOT parse invalid MAC address: {input}");
        Assert.True(Equals(mac, PhysicalAddress.None) || mac.GetAddressBytes().Length != 6);
    }
}
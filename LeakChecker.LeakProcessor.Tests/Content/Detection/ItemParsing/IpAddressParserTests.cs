using System.Net;
using LeakChecker.Content;
using LeakChecker.Content.Detection.ItemParsing;

namespace LeakProcessor.Tests.Content.Detection.ItemParsing;

public class IpAddressParserTests
{
    [Theory]
    // Pure IPv4
    [InlineData("8.8.8.8", true, ItemEnum.IpV4)]        // Google DNS
    [InlineData("1.1.1.1", true, ItemEnum.IpV4)]        // Cloudflare DNS
    [InlineData("192.168.0.1", true, ItemEnum.IpV4)]    // Common private network gateway
    [InlineData("10.0.0.1", true, ItemEnum.IpV4)]       // Private network address
    [InlineData("172.16.0.5", true, ItemEnum.IpV4)]     // Private network address
    [InlineData("0x7F.0x00.0x00.0x01", true, ItemEnum.IpV4)]  // Hex format

    // IPv4 with ports
    [InlineData("8.8.8.8:53", true, ItemEnum.IpV4)]
    [InlineData("127.0.0.1:8080", true, ItemEnum.IpV4)]
    [InlineData("192.168.1.10:443", true, ItemEnum.IpV4)]
    [InlineData("10.0.0.1:22", true, ItemEnum.IpV4)]
    [InlineData("1.1.1.1:65535", true, ItemEnum.IpV4)]
        
    // Pure IPv6
    [InlineData("2001:4860:4860::8888", true, ItemEnum.IpV6)]   // Google DNS
    [InlineData("2606:4700:4700::1111", true, ItemEnum.IpV6)]   // Cloudflare
    [InlineData("fe80::1", true, ItemEnum.IpV6)]                // Link-local
    [InlineData("2001:db8::", true, ItemEnum.IpV6)]             // Documentation prefix
    [InlineData("2001:0db8:85a3:0000:0000:8a2e:0370:7334", true, ItemEnum.IpV6)]    // Full IpV6


    // IPv6 with brackets
    [InlineData("[2001:4860:4860::8888]", true, ItemEnum.IpV6)]
    [InlineData("[2606:4700:4700::1111]", true, ItemEnum.IpV6)]
    [InlineData("[fe80::1]", true, ItemEnum.IpV6)]
    [InlineData("[2001:db8::]", true, ItemEnum.IpV6)]

    // IPv6 with brackets and ports
    [InlineData("[2001:4860:4860::8888]:53", true, ItemEnum.IpV6)]
    [InlineData("[2606:4700:4700::1111]:443", true, ItemEnum.IpV6)]
    [InlineData("[fe80::1]:8080", true, ItemEnum.IpV6)]
    [InlineData("[::1]:12345", true, ItemEnum.IpV6)]
    [InlineData("[2001:db8::]:65535", true, ItemEnum.IpV6)]

    // Localhost variants
    [InlineData("127.0.0.1", true, ItemEnum.IpV4)]
    [InlineData("localhost", true, ItemEnum.IpV4)]
    [InlineData("localhost:80", true, ItemEnum.IpV4)]
    [InlineData("LOCALHOST:53861", true, ItemEnum.IpV4)]
    [InlineData("::1", true, ItemEnum.IpV6)]
    [InlineData("[::1]", true, ItemEnum.IpV6)]
    [InlineData("[localhost]", true, ItemEnum.IpV6)]
    [InlineData("[localhost]:443", true, ItemEnum.IpV6)]
    [InlineData("[LOCALHOST]:49500", true, ItemEnum.IpV6)]

    // IPv6 shortcuts and minimal valid addresses
    [InlineData("::", true, ItemEnum.IpV6)]                    // Unspecified   //TODO can match a delimiters in CSV file
    [InlineData("::ffff:192.168.1.1", true, ItemEnum.IpV6)]    // IPv4-mapped IPv6
    [InlineData("::2", true, ItemEnum.IpV6)]                   // Loopback-ish
    [InlineData("2001::1", true, ItemEnum.IpV6)]               // Simplified
    [InlineData("fd00::", true, ItemEnum.IpV6)]                // Unique local address

    // Negative test cases
    [InlineData("192.168.1", false, ItemEnum.Other)]            // IpV4 Missing octet
    [InlineData("192.168.1.1.1", false, ItemEnum.Other)]        // IpV4 Too many octets
    [InlineData("192.168.one.1", false, ItemEnum.Other)]        // IpV4 Mixed text
    [InlineData("300.1.1.1", false, ItemEnum.Other)]            // IpV4 Out-of-range
    [InlineData("256.256.256.256", false, ItemEnum.Other)]      // IpV4 Out-of-range
    [InlineData("999.999.999.999", false, ItemEnum.Other)]      // IpV4 Out-of-range
    [InlineData("1.1.1.1:99999", false, ItemEnum.Other)]        // IpV4 Invalid port
    [InlineData("1.1.1.1:port", false, ItemEnum.Other)]         // IpV4 Invalid port
        
    [InlineData(":::", false, ItemEnum.Other)]                  // IPv6 Invalid
    [InlineData("[::1]extra", false, ItemEnum.Other)]           // IPv6 Extra chars after bracket
    [InlineData("[2001:db8::1", false, ItemEnum.Other)]         // IPv6 Missing closing bracket
    [InlineData("[2001:db8::1]:abc", false, ItemEnum.Other)]    // IPv6 Invalid port
    [InlineData("2001:db8:85a3:8a2e:370:7334", false, ItemEnum.Other)]  // IPv6 Only 6 groups
    [InlineData("2001:db8:1:2:3:4:5:6:7", false, ItemEnum.Other)]   // IPv6 9 groups
    [InlineData("2001:rang:85a3:8a2e:370:7334", false, ItemEnum.Other)] // IPv6 Out-of-range
        
    [InlineData("Hakuna matata", false, ItemEnum.Other)]        // Not an IP
    [InlineData("1p@ddre$$", false, ItemEnum.Other)]            // Not an IP
    [InlineData("some-random-string", false, ItemEnum.Other)]   // String in address
    [InlineData("some.rand.string.addr", false, ItemEnum.Other)]// IpV4 String in address
    [InlineData("some:rand:string:addr", false, ItemEnum.Other)]// IpV6 String in address
    [InlineData("localhost:abc", false, ItemEnum.Other)]        // IpV4 Invalid port
    [InlineData("[localhost]:abc", false, ItemEnum.Other)]      // IpV6 Invalid port
    [InlineData("2614784", false, ItemEnum.Other)]              // Random number
    [InlineData("3232235521", false, ItemEnum.Other)]             // Decimal 192.168.0.1 without '.'

    [InlineData("", false, ItemEnum.Other)]                     // Empty string
    public void TryParse_ShouldDetectIpAddresses(string input, bool expected, ItemEnum expectedType)
    {
        // Act
        var result = IpAddressParser.TryParse(input, out ItemEnum actualType, out IPAddress ip);

        // Assert
        Assert.Equal(expected, result);
        Assert.Equal(expectedType, actualType);

        if (expected)
        {
            Assert.NotEqual(IPAddress.None, ip);
        }
        else
        {
            Assert.Equal(IPAddress.None, ip);
        }
    }
}
using System.Net;
using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Content.Detection.ItemParsing;

namespace LeakChecker.DataParser.Tests.Content.Detection.ItemParsing;

public class IpAddressParserTests
{
    [Theory]
    // Pure IPv4
    [InlineData("8.8.8.8", true, ItemType.Ipv4)]        // Google DNS
    [InlineData("1.1.1.1", true, ItemType.Ipv4)]        // Cloudflare DNS
    [InlineData("10.0.0.1", true, ItemType.Ipv4)]       // Private network address
    [InlineData("172.16.0.5", true, ItemType.Ipv4)]     // Private network address
    [InlineData("192.168.0.1", true, ItemType.Ipv4)]    // Common private network gateway
    [InlineData("0x7F.0x00.0x00.0x01", true, ItemType.Ipv4)]  // Hex format

    // IPv4 with ports
    [InlineData("10.0.0.1:22", true, ItemType.Ipv4)]
    [InlineData("8.8.8.8:53", true, ItemType.Ipv4)]
    [InlineData("1.1.1.1:65535", true, ItemType.Ipv4)]
    [InlineData("127.0.0.1:8080", true, ItemType.Ipv4)]
    [InlineData("192.168.1.10:443", true, ItemType.Ipv4)]
        
    // Pure IPv6
    [InlineData("fe80::1", true, ItemType.Ipv6)]                // Link-local
    [InlineData("2001:db8::", true, ItemType.Ipv6)]             // Documentation prefix
    [InlineData("2001:4860:4860::8888", true, ItemType.Ipv6)]   // Google DNS
    [InlineData("2606:4700:4700::1111", true, ItemType.Ipv6)]   // Cloudflare
    [InlineData("2001:0db8:85a3:0000:0000:8a2e:0370:7334", true, ItemType.Ipv6)]    // Full IpV6


    // IPv6 with brackets
    [InlineData("[fe80::1]", true, ItemType.Ipv6)]
    [InlineData("[2001:db8::]", true, ItemType.Ipv6)]
    [InlineData("[2001:4860:4860::8888]", true, ItemType.Ipv6)]
    [InlineData("[2606:4700:4700::1111]", true, ItemType.Ipv6)]
    [InlineData("[2001:0db8:85a3:0000:0000:8a2e:0370:7334]", true, ItemType.Ipv6)]


    // IPv6 with brackets and ports
    [InlineData("[::1]:12345", true, ItemType.Ipv6)]
    [InlineData("[fe80::1]:8080", true, ItemType.Ipv6)]
    [InlineData("[2001:db8::]:65535", true, ItemType.Ipv6)]
    [InlineData("[2001:4860:4860::8888]:53", true, ItemType.Ipv6)]
    [InlineData("[2606:4700:4700::1111]:443", true, ItemType.Ipv6)]

    // Localhost variants
    [InlineData("127.0.0.1", true, ItemType.Ipv4)]
    [InlineData("localhost", true, ItemType.Ipv4)]
    [InlineData("localhost:80", true, ItemType.Ipv4)]
    [InlineData("LOCALHOST:53861", true, ItemType.Ipv4)]
    [InlineData("::1", true, ItemType.Ipv6)]
    [InlineData("[::1]", true, ItemType.Ipv6)]
    [InlineData("[localhost]", true, ItemType.Ipv6)]
    [InlineData("[localhost]:443", true, ItemType.Ipv6)]
    [InlineData("[LOCALHOST]:49500", true, ItemType.Ipv6)]

    // IPv6 shortcuts and minimal valid addresses
    [InlineData("::", true, ItemType.Ipv6)]                    // Unspecified   //TODO can match a delimiters in CSV file
    [InlineData("::2", true, ItemType.Ipv6)]                   // Loopback-ish
    [InlineData("2001::1", true, ItemType.Ipv6)]               // Simplified
    [InlineData("fd00::", true, ItemType.Ipv6)]                // Unique local address
    [InlineData("::ffff:192.168.1.1", true, ItemType.Ipv6)]    // IPv4-mapped IPv6

    // Negative test cases
    [InlineData("192.168.1", false, ItemType.Other)]            // IpV4 Missing octet
    [InlineData("192.168.1.1.1", false, ItemType.Other)]        // IpV4 Too many octets
    [InlineData("192.168.one.1", false, ItemType.Other)]        // IpV4 Mixed text
    [InlineData("300.1.1.1", false, ItemType.Other)]            // IpV4 Out-of-range
    [InlineData("256.256.256.256", false, ItemType.Other)]      // IpV4 Out-of-range
    [InlineData("999.999.999.999", false, ItemType.Other)]      // IpV4 Out-of-range
    [InlineData("1.1.1.1:99999", false, ItemType.Other)]        // IpV4 Invalid port
    [InlineData("1.1.1.1:port", false, ItemType.Other)]         // IpV4 Invalid port
        
    [InlineData(":::", false, ItemType.Other)]                  // IPv6 Invalid
    [InlineData("[::1]extra", false, ItemType.Other)]           // IPv6 Extra chars after bracket
    [InlineData("[2001:db8::1", false, ItemType.Other)]         // IPv6 Missing closing bracket
    [InlineData("[2001:db8::1]:abc", false, ItemType.Other)]    // IPv6 Invalid port
    [InlineData("2001:db8:85a3:8a2e:370:7334", false, ItemType.Other)]  // IPv6 Only 6 groups
    [InlineData("2001:db8:1:2:3:4:5:6:7", false, ItemType.Other)]   // IPv6 9 groups
    [InlineData("2001:rang:85a3:8a2e:370:7334", false, ItemType.Other)] // IPv6 Out-of-range
        
    [InlineData("Hakuna matata", false, ItemType.Other)]        // Not an IP
    [InlineData("1p@ddre$$", false, ItemType.Other)]            // Not an IP
    [InlineData("some-random-string", false, ItemType.Other)]   // String in address
    [InlineData("some.rand.string.addr", false, ItemType.Other)]// IpV4 String in address
    [InlineData("some:rand:string:addr", false, ItemType.Other)]// IpV6 String in address
    [InlineData("localhost:abc", false, ItemType.Other)]        // IpV4 Invalid port
    [InlineData("[localhost]:abc", false, ItemType.Other)]      // IpV6 Invalid port
    [InlineData("2614784", false, ItemType.Other)]              // Random number
    [InlineData("3232235521", false, ItemType.Other)]           // Decimal 192.168.0.1 without '.'
    [InlineData("", false, ItemType.Other)]                     // Empty string
    public void TryParse_ShouldDetectIpAddresses(string input, bool expected, ItemType expectedType)
    {
        // Act
        var result = IpAddressParser.TryParse(input, out ItemType actualType, out IPAddress ip);

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
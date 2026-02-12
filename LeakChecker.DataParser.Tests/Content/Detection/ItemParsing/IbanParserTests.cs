using LeakChecker.Content.Detection.ItemParsing;

namespace LeakChecker.DataParser.Tests.Content.Detection.ItemParsing;

public class IbanParserTests
{
    [Theory]
    // Valid IBANs (plain, uppercase)
    [InlineData("GB82WEST12345698765432")]       // UK
    [InlineData("DE89370400440532013000")]       // Germany
    [InlineData("FR1420041010050500013M02606")]  // France
    [InlineData("GR1601101250000000012300695")]  // Greece
    [InlineData("NL91ABNA0417164300")]           // Netherlands
    [InlineData("ES9121000418450200051332")]     // Spain
    [InlineData("IT60X0542811101000000123456")]  // Italy
    [InlineData("NO9386011117947")]              // Norway (short)
        
    // Valid IBANs with common formatting variations
    [InlineData("GB82 WEST 1234 5698 7654 32")]       // spaces
    [InlineData("DE89 3704 0044 0532 0130 00")]       // spaced groups of 4
    [InlineData("fr14-2004-1010-0505-0001-3m02-606")] // lowercase with dashes
    [InlineData("GR16 0110 1250 0000 0001 2300 695")] // spaced, uppercase
    [InlineData("nl91-abna-0417-1643-00")]            // lowercase with dashes
    public void TryParse_ShouldReturnTrue_ForValidIbans(string input)
    {
        bool ok = IbanParser.TryParse(input);
        Assert.True(ok, $"Expected valid IBAN: {input}");
    }

    [Theory]
    // Invalid IBANs (structural issues)
    [InlineData("GB82WEST1234569876543")]        // too short
    [InlineData("GB82WEST123456987654321")]      // too long
    [InlineData("GB00WEST12345698765432")]       // invalid check digits
    [InlineData("DE8937040044053201300X")]       // contains invalid char
    [InlineData("12345678901234567890123456")]   // no country code
    [InlineData("")]                             // Empty
    [InlineData(" ")]                            // whitespace
    [InlineData("Hakuna matata")]                // Random string
    [InlineData("2641746484164")]                // Numbers
    [InlineData("GBkqmck645475")]                // Random string
    [InlineData("+44 7700 900123")]              // Phone number
    [InlineData("GB82 WEST 1234 5698 7654 3")]   // too short with spaces
    [InlineData("GR1601101250000000012300696")]  // wrong checksum
    [InlineData("DE00DE00DE00DE00DE00DE00DE00")] // nonsense pattern

    // Invalid IBANs with common formatting problems
    [InlineData("NL91 ABNA 0417 1643 0")]           // missing digit
    [InlineData("GB82-WEST-1234-5698-7654-3X")]     // invalid char at end
    [InlineData("GB82-WEST-1234-5698-7654-3#")]     // invalid char at end
    [InlineData("GB82/WEST/1234/5698/7654/32")]     // invalid separators (slashes)
    [InlineData("ES91-2100-0418-4502-0005-133Z")]   // invalid trailing character
    [InlineData("FR14 2004 1010 0505 0001 3M02 6066")]  // too long
    public void TryParse_ShouldReturnFalse_ForInvalidIbans(string input)
    {
        bool ok = IbanParser.TryParse(input);
        Assert.False(ok, $"Expected invalid IBAN: {input}");
    }
}
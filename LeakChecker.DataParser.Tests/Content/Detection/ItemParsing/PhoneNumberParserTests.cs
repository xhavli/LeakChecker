using LeakChecker.DataParser.Content.Detection.ItemParsing;

namespace LeakChecker.DataParser.Tests.Content.Detection.ItemParsing
{
    public class PhoneNumberParserTests
    {
        [Theory]
        // US
        [InlineData("+12125550123")]
        [InlineData("+1 212 555 0123")]
        [InlineData("+1 (212) 555 0123")]
        [InlineData("+1-212-555-0123")]
        [InlineData("+1 (212)-555-0123")]

        // Czech Republic
        [InlineData("+420602987654")]
        [InlineData("+420 602 987 654")]
        [InlineData("+420 (602) 987 654")]
        [InlineData("+420-602-987-654")]
        [InlineData("+420 (602)-987-654")]

        // Russia
        [InlineData("+74951234567")]
        [InlineData("+7 495 123 4567")]
        [InlineData("+7 (495) 123 4567")]
        [InlineData("+7-495-123-4567")]
        [InlineData("+7 (495)-123-4567")]

        // India
        [InlineData("+919812345678")]
        [InlineData("+91 98123 45678")]
        [InlineData("+91 (98123) 45678")]
        [InlineData("+91-98123-45678")]
        [InlineData("+91 (98123)-45678")]

        // Egypt
        [InlineData("+201001234567")]
        [InlineData("+20 100 123 4567")]
        [InlineData("+20 (100) 123 4567")]
        [InlineData("+20-100-123-4567")]
        [InlineData("+20 (100)-123-4567")]
        public void TryParse_ShouldParseInternationalNumbers(string input)
        {
            var ok = PhoneNumberParser.TryParse(input, out _);

            Assert.True(ok, $"Should parse valid international number: {input}");
        }

        [Theory]
        // International without '+'
        [InlineData("12025551212")]     // US
        [InlineData("420602123456")]    // Czech
        [InlineData("74951234567")]     // Russia
        [InlineData("919876543210")]    // India
        [InlineData("201001234567")]    // Egypt
        public void TryParse_ShouldParseWithoutPlus(string input)
        {
            var ok = PhoneNumberParser.TryParse(input, out _);

            Assert.True(ok, $"Should parse number without +: {input}");
        }

        [Theory]
        // Negative test cases
        [InlineData("12345")]                // too short
        [InlineData("abcdefg")]              // not numeric
        [InlineData("++420602123456")]       // double plus
        [InlineData("#420602123456")]        // contains '#'
        [InlineData("+420602+123456")]       // plus in the middle
        [InlineData("+1 (212) ABC-DEFG")]    // old Telecom format
        [InlineData("+999999999999999999")]  // invalid country code
        [InlineData("0700-ABC-DEF")]         // non-digit with dashes
        [InlineData("0000000000")]           // invalid prefix
        [InlineData("112")]                  // EU emergency number
        [InlineData("911")]                  // US emergency number
        [InlineData("+42")]                  // incomplete
        [InlineData("")]                     // empty
        public void TryParse_ShouldRejectInvalidNumbers(string input)
        {
            var ok = PhoneNumberParser.TryParse(input, out string result);

            Assert.False(ok, $"Should NOT parse invalid number: {input}");
            Assert.True(string.IsNullOrEmpty(result));
        }
    }
}

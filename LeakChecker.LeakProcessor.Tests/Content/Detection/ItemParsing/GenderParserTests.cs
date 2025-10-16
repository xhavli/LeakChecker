using LeakChecker.Content.Detection.ItemParsing;

namespace LeakProcessor.Tests.Content.Detection.ItemParsing;

public class GenderParserTests
{
    [Theory]
    // Male values
    [InlineData("he", true, "Male")]
    [InlineData("man", true, "Male")]
    [InlineData("male", true, "Male")]
    [InlineData("masculino", true, "Male")]
    [InlineData("masc", true, "Male")]
    [InlineData("boy", true, "Male")]
        
    // Female values
    [InlineData("she", true, "Female")]
    [InlineData("woman", true, "Female")]
    [InlineData("female", true, "Female")]
    [InlineData("feminino", true, "Female")]
    [InlineData("fem", true, "Female")]
    [InlineData("girl", true, "Female")]
        
    // Other values
    [InlineData("trans", true, "Other")]
    [InlineData("transgender", true, "Other")]
    [InlineData("nonbinary", true, "Other")]
    [InlineData("non-binary", true, "Other")]
    [InlineData("shemale", true, "Other")]
        
    // Negative test cases
    [InlineData("Helikoptera", false, "")]
    [InlineData("$nake", false, "")]
    [InlineData("56789", false, "")]
    [InlineData("", false, "")]
    public void TryParse_ShouldReturnExpectedResult(string input, bool expectedResult, string expectedGender)
    {
        // Act
        var result = GenderParser.TryParse(input, out var gender);

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedGender, gender);
    }
}
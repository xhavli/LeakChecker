using LeakChecker.Content.Detection.ItemParsing;

namespace LeakProcessor.Tests.Content.Detection.ItemParsing;

public class GenderParserTests
{
    [Theory]
    // Male values
    [InlineData("he", true, "he")]
    [InlineData("man", true, "man")]
    [InlineData("male", true, "male")]
    [InlineData("masculino", true, "masculino")]
    [InlineData("masc", true, "masc")]
    [InlineData("boy", true, "boy")]
        
    // Female values
    [InlineData("she", true, "she")]
    [InlineData("woman", true, "woman")]
    [InlineData("female", true, "female")]
    [InlineData("feminino", true, "feminino")]
    [InlineData("fem", true, "fem")]
    [InlineData("girl", true, "girl")]
        
    // Other values
    [InlineData("trans", true, "trans")]
    [InlineData("transgender", true, "transgender")]
    [InlineData("nonbinary", true, "nonbinary")]
    [InlineData("non-binary", true, "non-binary")]
    [InlineData("shemale", true, "shemale")]
        
    // Negative test cases
    [InlineData("Helikoptera", false, "")]
    [InlineData("$nake", false, "")]
    [InlineData("56789", false, "")]
    [InlineData("", false, "")]
    public void TryParse_ShouldReturnExpectedResult(string input, bool expectedResult, string expectedGender)
    {
        // Act
        var result = GenderParser.TryParse(input, out string gender);

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedGender, gender);
    }
}
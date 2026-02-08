using LeakChecker.Content.Detection.ItemParsing;

namespace LeakProcessor.Tests.Content.Detection.ItemParsing;

public class MaritalStatusParserTests
{
    [Theory]
    // Positive test cases
    [InlineData("Single", true)]
    [InlineData("Dating", true)]
    [InlineData("Taken", true)]
    [InlineData("Partnered", true)]
    [InlineData("In a domestic partnership", true)]
    [InlineData("In a relationship", true)]
    [InlineData("Engaged", true)]
    [InlineData("Married", true)]
    [InlineData("Separated", true)]
    [InlineData("Divorced", true)]
    [InlineData("Widowed", true)]
    [InlineData("It's complicated", true)]
    
    // Negative test cases
    [InlineData("Hakuna matata", false)]
    [InlineData("#arital$tatu$", false)]
    [InlineData("12345", false)]
    [InlineData("", false)]
    public void TryParse_ReturnsExpectedResults(string input, bool expected)
    {
        // Act
        var result = MaritalStatusParser.TryParse(input, out var parsed);

        // Assert
        Assert.Equal(expected, result);
        Assert.Equal(expected ? input : string.Empty, parsed);
    }
}
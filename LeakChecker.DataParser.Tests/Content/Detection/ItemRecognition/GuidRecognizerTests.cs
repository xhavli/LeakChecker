using LeakChecker.Content.Detection.ItemRecognition;

namespace LeakChecker.DataParser.Tests.Content.Detection.ItemRecognition;

public class GuidRecognizerTests
{
    [Theory]
    // Positive cases
    [InlineData("UserID: 123e4567-e89b-12d3-a456-426614174000", "123e4567-e89b-12d3-a456-426614174000")]    // Normal lowercase
    [InlineData("Token: ABCDEF12-3456-7890-abcd-ef1234567890", "ABCDEF12-3456-7890-abcd-ef1234567890")]     // Uppercase mixed
    [InlineData("HiddenGUID: A1B2C3D4E5F67890A1B2C3D4E5F67890", "A1B2C3D4E5F67890A1B2C3D4E5F67890")]        // No dashes
    [InlineData("Value in brackets [123E4567-E89B-12D3-A456-426614174000]", "123E4567-E89B-12D3-A456-426614174000")] // Bracketed
    [InlineData("{ abcdef12-3456-7890-abcd-ef1234567890 }", "abcdef12-3456-7890-abcd-ef1234567890")]        // Curly braces
    [InlineData("MixCase: 12AB3456-cD78-9Ef0-AaBb-112233445566", "12AB3456-cD78-9Ef0-AaBb-112233445566")]   // Mixed case
    [InlineData("LeadingGUID:123e4567-e89b-12d3-a456-426614174000", "123e4567-e89b-12d3-a456-426614174000")]// GUID at end
    [InlineData("123e4567-e89b-12d3-a456-426614174000 trailing", "123e4567-e89b-12d3-a456-426614174000")]   // GUID at start
    [InlineData("Embedded###123e4567-e89b-12d3-a456-426614174000###data", "123e4567-e89b-12d3-a456-426614174000")]  // Surrounded by junk
    public void TryRecognize_ShouldFindValidGuids(string input, string expectedGuid)
    {
        // Act
        var ok = GuidRecognizer.TryRecognize(input, out List<string> stringGuids, out List<Guid> guids);

        Assert.True(ok, $"Recognizer should find a GUID in: {input}");
        Assert.Contains(expectedGuid, stringGuids, StringComparer.OrdinalIgnoreCase);
        Assert.NotEmpty(guids);
        Assert.Equal(Guid.Parse(expectedGuid), guids[0]);
    }
    
    [Theory]
    // 2 GUIDs  - with and without text
    [InlineData("UserIDs: 123e4567-e89b-12d3-a456-426614174000 and abcdef12-3456-7890-abcd-ef1234567890",
                "123e4567-e89b-12d3-a456-426614174000,abcdef12-3456-7890-abcd-ef1234567890")]  // Text between
    [InlineData("List: [123e4567-e89b-12d3-a456-426614174000] [abcdef12-3456-7890-abcd-ef1234567890]",
                "123e4567-e89b-12d3-a456-426614174000,abcdef12-3456-7890-abcd-ef1234567890")]  // Bracketed style
    [InlineData("123e4567-e89b-12d3-a456-426614174000,abcdef12-3456-7890-abcd-ef1234567890",
                "123e4567-e89b-12d3-a456-426614174000,abcdef12-3456-7890-abcd-ef1234567890")]  // Only GUIDs
    [InlineData("IDs: {123e4567-e89b-12d3-a456-426614174000}; next={abcdef12-3456-7890-abcd-ef1234567890}",
                "123e4567-e89b-12d3-a456-426614174000,abcdef12-3456-7890-abcd-ef1234567890")]  // Curly braces

    // 3 GUIDs - with and without text
    [InlineData("Batch: 123e4567-e89b-12d3-a456-426614174000, abcdef12-3456-7890-abcd-ef1234567890, 12ab3456-cd78-9ef0-aabb-112233445566",
                "123e4567-e89b-12d3-a456-426614174000,abcdef12-3456-7890-abcd-ef1234567890,12ab3456-cd78-9ef0-aabb-112233445566")]  // Comma-separated
    [InlineData("Multiple GUIDs: [123e4567-e89b-12d3-a456-426614174000] and [abcdef12-3456-7890-abcd-ef1234567890] and [12ab3456-cd78-9ef0-aabb-112233445566]",
                "123e4567-e89b-12d3-a456-426614174000,abcdef12-3456-7890-abcd-ef1234567890,12ab3456-cd78-9ef0-aabb-112233445566")]  // Realistic sentence
    [InlineData("{123e4567-e89b-12d3-a456-426614174000}|{abcdef12-3456-7890-abcd-ef1234567890}|{12ab3456-cd78-9ef0-aabb-112233445566}",
                "123e4567-e89b-12d3-a456-426614174000,abcdef12-3456-7890-abcd-ef1234567890,12ab3456-cd78-9ef0-aabb-112233445566")]  // Pipe-separated
    [InlineData("123e4567-e89b-12d3-a456-426614174000 abcdef12-3456-7890-abcd-ef1234567890 12ab3456-cd78-9ef0-aabb-112233445566",
                "123e4567-e89b-12d3-a456-426614174000,abcdef12-3456-7890-abcd-ef1234567890,12ab3456-cd78-9ef0-aabb-112233445566")]  // Space-separated
    public void TryRecognize_ShouldFindMultipleGuids(string input, string expectedGuidsCsv)
    {
        // Arrange
        var expectedGuids = expectedGuidsCsv.Split(',').Select(Guid.Parse).ToList();

        // Act
        var ok = GuidRecognizer.TryRecognize(input, out List<string> stringGuids, out List<Guid> guids);

        Assert.True(ok, $"Recognizer should find GUIDs in: {input}");
        Assert.Equal(expectedGuids.Count, guids.Count);
        Assert.Equal(expectedGuids.Count, stringGuids.Count);
        
        foreach (var expected in expectedGuids)
            Assert.Contains(expected, guids);
    }

    [Theory]
    // Negative cases
    [InlineData("InvalidGUID: 1234-5678-90AB-CDEF")]                    // Too short
    [InlineData("GUID? not-a-guid-value")]                              // Random text
    [InlineData("This text has numbers 1111222233334444555566667777")]  // Wrong length
    [InlineData("GUID-like 123e4567e89b12d3a45642661417400000")]        // Too long
    [InlineData("GUID_with_wrong_characters 123e4567-e89b-12d3-a456-42661417400G")] // Invalid hex char 'G'
    [InlineData("Wrong separators: 123e4567_e89b_12d3_a456_426614174000")]  // Underscores instead of dashes
    [InlineData("Wrong slash: 123e4567/e89b/12d3/a456/426614174000")]   // Slashes
    [InlineData("Broken grouping: 123e4567-e89b-12d3-a4567-426614174000")]  // 5th group too long
    [InlineData("randomtextwithoutguid")]                               // No pattern at all
    [InlineData("")]                                                    // Empty
    public void TryRecognize_ShouldRejectInvalidGuids(string input)
    {
        // Act
        var ok = GuidRecognizer.TryRecognize(input, out List<string> stringGuids, out List<Guid> guids);

        // Assert
        Assert.False(ok, $"Recognizer should NOT detect a valid GUID in: {input}");
        Assert.True(stringGuids == null || stringGuids.Count == 0, "No GUID strings should be returned.");
        Assert.True(guids == null || guids.Count == 0, "No GUID objects should be returned.");
    }
}
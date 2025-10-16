using LeakChecker.Content.Detection.ItemRecognition;

namespace LeakProcessor.Tests.Content.Detection.ItemRecognition;

public class WebRecognizerTests
{
    [Theory]
    // .com URLs
    [InlineData("Check this: https://www.google.com", "https://www.google.com")]
    [InlineData("Visit site: http://example.com/", "http://example.com/")]
    [InlineData("data:https://openai.com/about", "https://openai.com/about")]
    [InlineData("Try: https://github.com/openai/chatgpt", "https://github.com/openai/chatgpt")]
    [InlineData("url=https://www.microsoft.com/en-us/download", "https://www.microsoft.com/en-us/download")]
    [InlineData("site=https://twitter.com/home", "https://twitter.com/home")]
    [InlineData("goto: https://facebook.com/profile?id=123", "https://facebook.com/profile?id=123")]
    [InlineData("target=https://amazon.com/products/item1", "https://amazon.com/products/item1")]
    [InlineData("secure link: https://cloudflare.com/docs#section", "https://cloudflare.com/docs#section")]
    [InlineData("random: https://www.google.com/search?udm=2&q=usa#vhid=qNntVDgAadgVVM&vssid=mosaic", 
               "https://www.google.com/search?udm=2&q=usa#vhid=qNntVDgAadgVVM&vssid=mosaic")]
    public void TryRecognize_ShouldFindComUrls(string input, string expectedStringUri)
    {
        // Act
        var ok = WebRecognizer.TryRecognize(input, out List<string> stringUris, out List<Uri> uris);

        Assert.True(ok, $"Recognizer should detect a .com URL in: {input}");
        Assert.Contains(expectedStringUri, stringUris);
        Assert.NotEmpty(uris);
    }

    [Theory]
    // .cz URLs
    [InlineData("Check https://www.seznam.cz", "https://www.seznam.cz")]
    [InlineData("Try: http://centrum.cz", "http://centrum.cz")]
    [InlineData("data=https://www.idnes.cz/", "https://www.idnes.cz/")]
    [InlineData("url: https://novinky.cz/", "https://novinky.cz/")]
    [InlineData(">https://firma.cz/kontakt<", "https://firma.cz/kontakt")]
    public void TryRecognize_ShouldFindCzUrls(string input, string expectedStringUri)
    {
        // Act
        var ok = WebRecognizer.TryRecognize(input, out List<string> stringUris, out List<Uri> uris);

        Assert.True(ok, $"Recognizer should detect a .cz URL in: {input}");
        Assert.Contains(expectedStringUri, stringUris);
        Assert.NotEmpty(uris);
    }

    [Theory]
    // .it URLs
    [InlineData("Go to https://www.repubblica.it", "https://www.repubblica.it")]
    [InlineData("Check out: http://corriere.it for sports", "http://corriere.it")]
    [InlineData("source=https://www.gazzetta.it/", "https://www.gazzetta.it/")]
    [InlineData("info: https://libero.it/", "https://libero.it/")]
    [InlineData("open https://ansa.it/news", "https://ansa.it/news")]
    public void TryRecognize_ShouldFindItUrls(string input, string expectedStringUri)
    {
        // Act
        var ok = WebRecognizer.TryRecognize(input, out List<string> stringUris, out List<Uri> uris);

        Assert.True(ok, $"Recognizer should detect a .it URL in: {input}");
        Assert.Contains(expectedStringUri, stringUris);
        Assert.NotEmpty(uris);
    }
    
    [Theory]
    // 2 uris - with and without text
    [InlineData("Compare https://google.com/search and https://bing.com/images",
        new[] { "https://google.com/search", "https://bing.com/images" })]
    [InlineData("Sources: https://novinky.cz/article123 and https://cnn.com/news/world",
        new[] { "https://novinky.cz/article123", "https://cnn.com/news/world" })]
    [InlineData("Hey, check this out: first go to https://google.com/search for info, then maybe browse https://bing.com/images to compare pictures!",
        new[] { "https://google.com/search", "https://bing.com/images" })]
    // 3 uris - with and without text
    [InlineData("https://github.com/openai https://stackoverflow.com/questions https://microsoft.cz/products",
        new[] { "https://github.com/openai", "https://stackoverflow.com/questions", "https://microsoft.cz/products" })]
    [InlineData("Useful links: https://github.com/openai, https://stackoverflow.com/questions, and https://microsoft.cz/products",
        new[] { "https://github.com/openai", "https://stackoverflow.com/questions", "https://microsoft.cz/products" })]

    public void TryRecognize_ShouldFindMultipleUrls(string input, string[] expectedUris)
    {
        var ok = WebRecognizer.TryRecognize(input, out List<string> stringUris, out List<Uri> uris);

        Assert.True(ok);
        Assert.Equal(expectedUris.Length, uris.Count);
        Assert.Equal(expectedUris.Length, stringUris.Count);
        
        foreach (var expected in expectedUris)
            Assert.Contains(expected, stringUris);
    }
        
    [Theory]
    // Negative test cases
    [InlineData("25414")]
    [InlineData("W#b pa@e")]
    [InlineData("Hakuna matata")]
    [InlineData("http:/example.com")]                  // Missing second slash
    [InlineData("htp://wrongprotocol.com")]            // Invalid protocol
    [InlineData("https://")]                           // Missing domain
    [InlineData("https://.com")]                       // Missing domain name
    [InlineData("www..com")]                           // Double dots
    [InlineData("example dot com")]                    // Text instead of dot
    [InlineData("randomtextwithouturl")]               // No URL at all
    // [InlineData("http://domain_with_underscore.com")]  // Invalid character //TODO underscore is not allowed in domain name
    [InlineData("://missingprotocol.com")]             // Missing scheme
    public void TryRecognize_ShouldRejectInvalidUrls(string input)
    {
        // Act
        var ok = WebRecognizer.TryRecognize(input, out List<string> stringUris, out List<Uri> uris);

        // Assert
        Assert.False(ok, $"Recognizer should NOT detect a valid URL in: {input}");
        Assert.True(stringUris == null || stringUris.Count == 0, "No URLs should be returned.");
        Assert.True(uris == null || uris.Count == 0, "No Uri objects should be parsed.");
    }
}
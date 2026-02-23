using LeakChecker.DataParser.Utilities.Configuration;

namespace LeakChecker.DataParser.Tests.Services;

public class ServiceTests
{
    [Fact]
    public async Task IsReachable_HashesDotCom()
    {
        // Arrange
        const string url = "https://hashes.com/en/tools/hash_identifier";
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(10);

        // Act
        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));

        // Assert
        Assert.True(response.IsSuccessStatusCode);
    }
    
    [Fact]
    public async Task IsReachable_PythonNerService()
    {
        // Arrange
        // *\LeakChecker\LeakChecker.DataParser.Tests\bin\Release\net0.0
        DirectoryInfo? dir = new DirectoryInfo(Environment.CurrentDirectory);
        // *\LeakChecker
        dir = dir.Parent?.Parent?.Parent?.Parent;
        string parserDir = Path.Combine(dir!.FullName, "LeakChecker.DataParser");
        string configJson = Path.Combine(parserDir, "appsettings.json");
        AppConfig config = AppConfigParser.LoadFromFile(configJson);
        string url = $"http://localhost:{config.PythonPort}/status";
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(10);
        
        // Act
        string status = await client.GetStringAsync(url);

        // Assert
        Assert.Equal("ready", status.Trim(), ignoreCase: true);
    }
}
using LeakChecker.DataParser.Utilities.Settings;

namespace LeakChecker.DataParser.Tests.Services;

public class ServicesTests
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
        //TODO fix when will be DI done properly
        // string projectDir = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.Parent?.FullName!;
        // string jsonPath = Path.Combine(projectDir, "LeakChecker.DataParser/appsettings.json");
        // AppConfig config = AppConfigParser.LoadFromFile(jsonPath);
        string url = $"http://localhost:{8000}/status";
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(10);
        
        // Act
        string status = await client.GetStringAsync(url);

        // Assert
        Assert.Equal("ready", status.Trim(), ignoreCase: true);
    }
}
using System.Text.Json;

namespace LeakChecker.Content.Detection.RecognitionService;

public static class PythonNerServiceRecognizer
{
    private static readonly HttpClient Client = new();
    private const string BaseUrl = "http://localhost:8000/analyze?text=";
    private static readonly JsonSerializerOptions? Options = new() { PropertyNameCaseInsensitive = true };

    public static async Task<List<PresidioEntity>> TryRecognize(string line)
    {
        string requestUrl = BaseUrl + Uri.EscapeDataString(line);
        
        var analyzeResponse = await Client.GetAsync(requestUrl);
        analyzeResponse.EnsureSuccessStatusCode();

        var json = await analyzeResponse.Content.ReadAsStringAsync();
        List<PresidioEntity>? analyzeResult = JsonSerializer.Deserialize<List<PresidioEntity>>(json, Options);

        return analyzeResult ?? new List<PresidioEntity>();
    }
}
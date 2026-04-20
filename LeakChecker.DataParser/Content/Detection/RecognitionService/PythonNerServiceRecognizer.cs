using System.Text.Json;

namespace LeakChecker.DataParser.Content.Detection.RecognitionService;

public static class PythonNerServiceRecognizer
{
    private const string Person = "PERSON";
    private const string Location = "LOCATION";
    private const string Organization = "ORGANIZATION";
    private static readonly HttpClient Client = new();
    private const string BaseUrl = "http://localhost:8000/analyze?text=";
    private static readonly JsonSerializerOptions? Options = new() { PropertyNameCaseInsensitive = true };

    public static async Task<List<PresidioEntity>> TryRecognize(string line)
    {
        string requestUrl = BaseUrl + Uri.EscapeDataString(line);
        
        var response = await Client.GetAsync(requestUrl);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        List<PresidioEntity>? result = JsonSerializer.Deserialize<List<PresidioEntity>>(json, Options);

        return result ?? new List<PresidioEntity>();
    }

    public static async Task<ItemEnum?> TryRecognizeToken(string token)
    {
        string requestUrl = BaseUrl + Uri.EscapeDataString(token);
        
        var response = await Client.GetAsync(requestUrl);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        List<PresidioEntity> result = JsonSerializer.Deserialize<List<PresidioEntity>>(json, Options) ?? new();
        
        return result.Count == 0 ? null : MapEntityType(result.First().Type);
    }

    public static ItemEnum MapEntityType(string entityType) => entityType switch
    {
        Person => ItemEnum.Name,
        Location => ItemEnum.Location,
        Organization => ItemEnum.Organization,
        _ => throw new Exception($"Unknown entity type: '{entityType}' returned from PythonNerService")
    };
}
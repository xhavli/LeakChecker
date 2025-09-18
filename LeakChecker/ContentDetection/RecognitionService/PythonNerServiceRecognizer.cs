using System.Text.Json;

namespace LeakChecker.ContentDetection.RecognitionService;

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

        return MergeEntities(analyzeResult);
    }
    
    private static List<PresidioEntity> MergeEntities(List<PresidioEntity>? entities, int maxGap = 2)
    {
        if (entities == null || !entities.Any()) return new List<PresidioEntity>();

        // Ensure entities are sorted by start index
        entities = entities.OrderBy(e => e.Start).ToList();

        var current = entities[0];
        List<PresidioEntity> result = new();

        for (int i = 1; i < entities.Count; i++)
        {
            var next = entities[i];

            // Check if same type AND close enough
            if (next.Type == current.Type && next.Start - current.End <= maxGap)
            {
                // Merge: extend the end position
                current.End = next.End;
            }
            else
            {
                result.Add(current);
                current = next;
            }
        }

        result.Add(current);
        return result;
    }
}
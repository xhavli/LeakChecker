using System.Text.Json;
using System.Text.Json.Serialization;

namespace LeakChecker.ContentDetection;

public class PresidioEntity
{
    [JsonPropertyName("entity_type")]
    public required string Type { get; set; }

    [JsonPropertyName("start")]
    public int Start { get; set; }

    [JsonPropertyName("end")]
    public int End { get; set; }
}
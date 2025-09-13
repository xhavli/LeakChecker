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

    [JsonPropertyName("score")]
    [JsonConverter(typeof(FlexibleDoubleConverter))]
    public double Score { get; set; }

    public override string ToString()
    {
        return $"{Type} [{Start}-{End}], score={Score}";
    }
}

public class FlexibleDoubleConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.String => double.TryParse(reader.GetString(), out double result) ? result : 0.0,
            _ => throw new JsonException("Unexpected token type for double")
        };
    }

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}
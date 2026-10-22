namespace LeakChecker.DataParser.Helpers.Settings;

public record SamplingSettings
{
    public int CsvSamples { get; init; }
    public int SqlSamples { get; init; }
    public int ExcelSamples { get; init; }
    public int SchemaThreshold { get; init; }
}
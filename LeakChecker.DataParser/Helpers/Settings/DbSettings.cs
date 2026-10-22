namespace LeakChecker.DataParser.Helpers.Settings;

public record DbSettings
{
    public string DbProvider { get; init; } = string.Empty;
    public string ConnectionString { get; init; }  = string.Empty;
}
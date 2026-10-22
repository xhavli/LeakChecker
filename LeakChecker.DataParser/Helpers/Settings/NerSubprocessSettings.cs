namespace LeakChecker.DataParser.Helpers.Settings;

public record NerSubprocessSettings
{
    public string PythonScriptName { get; init; }  = string.Empty;
    public ushort CsharpPort { get; init; }
    public ushort PythonPort { get; init; }
    public uint StartupTimeoutSeconds { get; init; }
}
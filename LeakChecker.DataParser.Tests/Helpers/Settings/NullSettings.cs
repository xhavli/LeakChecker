using System.Text;
using LeakChecker.DataParser.Database;
using LeakChecker.DataParser.Helpers.Settings;

namespace LeakChecker.DataParser.Tests.Helpers.Settings;

public class NullSettings : ISettings
{
    public string InputDirectory { get; init; } = string.Empty;
    public string LogDirectory { get; init; } = string.Empty;
    public string TmpDirectory { get; init; } = string.Empty;
    public string ProjectDirectory { get; init; } = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.Parent?.FullName!;
    public double? ParseSizeLimitGb { get; init; } = null;
    public string? ResumeFromPath { get; init; } = null;
    public string PythonVenvPath { get; init; } = string.Empty;
    public string PythonScriptName { get; init; } = string.Empty;
    public int CsharpPort { get; init; } = 6666;
    public int PythonPort { get; init; } = 8000;
    public int StartupTimeoutSeconds { get; init; } = 300;
    public int ThreadsCapacity { get; init; } = 10;
    public int ChannelCapacity { get; init; } = 10;
    public int SchemaThreshold { get; init; } = 50;
    public int CsvSamples { get; init; } = 103;
    public int SqlSamples { get; init; } = 31;
    public int ExcelSamples { get; init; } = 31;
    public Encoding DefaultUtf8 { get; init; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    public string Environment { get; init; } = string.Empty;
    public bool Verbose { get; init; } = false;
    public IDatabase Database { get; init; } = new NullDatabase();

    public void ApplyGlobalSettings()
    {
        ApplyEncodingSettings();

        System.Environment.SetEnvironmentVariable("LeakCheckerRunningTest", Environment == "Test" ? "true" : null);
    }

    private void ApplyEncodingSettings()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        Console.InputEncoding = DefaultUtf8;
        Console.OutputEncoding = DefaultUtf8;
    }
}
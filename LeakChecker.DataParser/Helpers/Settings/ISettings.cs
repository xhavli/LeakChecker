using System.Text;
using LeakChecker.DataParser.Database;
using LeakChecker.DataParser.Helpers.Enums;

namespace LeakChecker.DataParser.Helpers.Settings;

public interface ISettings
{
    public string InputDirectory { get; init; }
    public string LogDirectory { get; init; }
    public string TmpDirectory { get; init; }
    public string ProjectDirectory { get; init; }
    public double? ParseSizeLimitGb { get; init; }
    public long? ParseSizeLimitBytes => ParseSizeLimitGb is null 
        ? null 
        : (long)(ParseSizeLimitGb.Value * SizeEnum.GigaByte);
    public string? ResumeFromPath { get; init; }
    public string PythonVenvPath { get; init; }
    public string PythonScriptName { get; init; }
    public int CsharpPort { get; init; }
    public int PythonPort { get; init; }
    public int StartupTimeoutSeconds { get; init; }
    public int ThreadsCapacity { get; init; }
    public int ChannelCapacity { get; init; }
    public int SchemaThreshold { get; init; }
    public int CsvSamples { get; init; }
    public int SqlSamples { get; init; }
    public int ExcelSamples { get; init; }
    public Encoding DefaultUtf8 { get; init; }
    public string Environment { get; init; }
    public bool Verbose { get; init; }
    public IDatabase Database { get; init; }
    
    public void ApplyGlobalSettings()
    {
        ApplyEncodingSettings();
        
        if (Environment == "Test")
            System.Environment.SetEnvironmentVariable("LeakCheckerRunningTest", "true");
        else
            System.Environment.SetEnvironmentVariable("LeakCheckerRunningTest", null);
            
    }

    private void ApplyEncodingSettings()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        Console.InputEncoding = DefaultUtf8;
        Console.OutputEncoding = DefaultUtf8;
    }
}
using System.Text;
using LeakChecker.DataParser.Database;

namespace LeakChecker.DataParser.Helpers.Settings;

public class Settings : ISettings
{
    public required string InputDirectory { get; init; }
    public required string LogDirectory { get; init; }
    public required string TmpDirectory { get; init; }
    public required string ProjectDirectory { get; init; }
    public required double? ParseSizeLimitGb { get; init; }
    public required string? ResumeFromPath { get; init; }
    public required string PythonVenvPath { get; init; }
    public required string PythonScriptName { get; init; }
    public required int CsharpPort { get; init; }
    public required int PythonPort { get; init; }
    public required int StartupTimeoutSeconds { get; init; }
    public required int ThreadsCapacity { get; init; }
    public required int ChannelCapacity { get; init; }
    public required int SchemaThreshold { get; init; }
    public required int CsvSamples { get; init; }
    public required int SqlSamples { get; init; }
    public required int ExcelSamples { get; init; }
    public required Encoding DefaultUtf8 { get; init; }
    public required string Environment { get; init; }
    public required bool Verbose { get; init; }
    public required IDatabase Database { get; init; }

    public static Settings FromJson(JsonSettings jsonSettings)
    {
        ArgumentNullException.ThrowIfNull(jsonSettings);

        jsonSettings.Validate();
        
        return new Settings
        {
            InputDirectory = Path.GetFullPath(jsonSettings.InputDirectory!),
            LogDirectory = Path.GetFullPath(jsonSettings.LogDirectory!),
            TmpDirectory = Path.GetFullPath(jsonSettings.TmpDirectory!),
            ProjectDirectory = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.Parent?.FullName!,
            ParseSizeLimitGb = jsonSettings.ParseSizeLimitGb,
            ResumeFromPath = jsonSettings.ResumeFromPath is null 
                ? null 
                : Path.GetFullPath(jsonSettings.ResumeFromPath),
            PythonVenvPath = ResolvePythonVenvPath(jsonSettings),
            PythonScriptName = jsonSettings.PythonScriptName!,
            CsharpPort = jsonSettings.CsharpPort,
            PythonPort = jsonSettings.PythonPort,
            StartupTimeoutSeconds = jsonSettings.StartupTimeoutSeconds,
            ThreadsCapacity = jsonSettings.ThreadsCapacity,
            ChannelCapacity = jsonSettings.ChannelCapacity,
            SchemaThreshold = jsonSettings.SchemaThreshold,
            CsvSamples = jsonSettings.CsvSamples,
            SqlSamples = jsonSettings.SqlSamples,
            ExcelSamples = jsonSettings.ExcelSamples,
            DefaultUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Environment = jsonSettings.Environment!,
            Verbose = jsonSettings.Verbose,
            Database = ResolveDatabase(jsonSettings.DbProvider)
        };
    }

    private static IDatabase ResolveDatabase(string? dbProvider)
    {
        if (string.IsNullOrWhiteSpace(dbProvider))
            return new NullDatabase();

        return dbProvider.Trim().ToLowerInvariant() switch
        {
            "mongodb" => new MongoDbFacade(),
            _ => throw new NotSupportedException($"Unsupported database provider '{dbProvider}'.")
        };
    }
    
    private static string ResolvePythonVenvPath(JsonSettings jsonSettings)
    {
        if (OperatingSystem.IsWindows())
            return jsonSettings.PythonVenvWindowsPath!;
    
        if (OperatingSystem.IsLinux())
            return jsonSettings.PythonVenvLinuxPath!;
    
        throw new PlatformNotSupportedException("Unsupported OS");
    }
}
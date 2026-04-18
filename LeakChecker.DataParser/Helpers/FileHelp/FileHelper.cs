using ExcelDataReader;
using LeakChecker.DataParser.Helpers.ArchiveExtraction;
using LeakChecker.DataParser.Helpers.Settings;
using LeakChecker.DataParser.Logging;
using LeakChecker.DataParser.Logging.Execution;
using MimeDetective;
using MimeDetective.Definitions;
using MimeDetective.Definitions.Licensing;

namespace LeakChecker.DataParser.Helpers.FileHelp;

public class FileHelper(ISettings settings, ArchiveExtractor archiveExtractor, ExecutionLogger logger)
{
    private static string? ApplicationName { get; } = AppDomain.CurrentDomain.FriendlyName;

    private static readonly IContentInspector MimeInspector = new ContentInspectorBuilder
    {
        Definitions = new ExhaustiveBuilder { UsageType = UsageType.PersonalNonCommercial }.Build()
    }.Build();
    
    private static readonly HashSet<string> TextualMimes = new(StringComparer.InvariantCultureIgnoreCase)
    {
        // --- CSV ---
        "application/csv",
        "application/tab-separated-values",

        // --- SQL ---
        "application/sql",
        "application/x-sql",
        
        // --- XML ---
        "application/xml",
        "application/xhtml+xml",
        "application/rss+xml",
        "application/atom+xml",
        "application/soap+xml",
        "application/mathml+xml",
        "application/svg+xml",
        
        // --- JSON ---
        "application/json",
        "application/ld+json",
        "application/schema+json",
        "application/vnd.api+json",
        
        // --- YAML ---
        "application/yaml",
        "application/x-yaml",
        
        // --- Misc structured text ---
        "application/graphql",
        "application/x-ndjson", // newline-delimited JSON
        "application/json-seq",
        
        // --- Log ---
        "application/x-log",
    };
    
    public bool IsAccessible(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            return true;
        }
        catch (FileNotFoundException)
        {
            logger.Log($"File in path: '{filePath}' does not exist.", LogLevel.Warning);
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            logger.Log($"{ApplicationName} or current user does not have permission to read the file " +
                       $"in path: '{filePath}'. Raised UnauthorizedAccessException.", LogLevel.Warning);
            return false;
        }
        catch (IOException)
        {
            logger.Log($"File in path: '{filePath}' is locked, in use or its not file. Raised IOException.",
                LogLevel.Warning);
            return false;
        }
        catch (Exception)
        {
            logger.Log($"File in path: '{filePath}' throw general exception", LogLevel.Warning);
            return false;
        }
    }

    private List<string> SelectAccessiblePaths(IEnumerable<string> filePaths)
    {
        List<string> parsePaths = new();
        
        foreach (var filePath in filePaths)
        {
            if (IsAccessible(filePath))
                parsePaths.Add(filePath);
        }

        return parsePaths;
    }
    
    public bool IsTextual(string filePath, int reliableThreshold = 4000)
    {
        using var stream = File.OpenRead(filePath);
        
        var results = MimeInspector.Inspect(stream);
        var best = results.ByMimeType().FirstOrDefault();
        
        if (best is not null && best.Points > reliableThreshold)
        {
            if (!(TextualMimes.Contains(best.MimeType) || 
                  best.MimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)))
            {
                logger.Log($"File in path: '{filePath}' have MIME Extension [{best.MimeType.ToLower()}] with [{best.Points}] " +
                           $"success points which can't be analysed via {ApplicationName}.", LogLevel.Warning);
                return false;
            }
        }
        
        return true;
    }
    
    public static bool IsExcel(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    public async Task<bool> IsReadable(string filePath, int sampleLimit = 1000, int threshold = 75)
    {
            await using var stream = File.OpenRead(filePath);
            using var reader = new StreamReader(stream);

            int malformed = 0;
            int sampled = 0;
            
            while (!reader.EndOfStream && sampled < sampleLimit)
            {
                string? line = await reader.ReadLineAsync();
                if (line == null || line.Contains('�'))
                    malformed++;

                sampled++;
            }
            
            double successRate = sampled == 0 ? 0 : Math.Max(0, (double)(sampled - malformed) / sampled  * 100);
            if (successRate < threshold)
            {
                logger.Log($"File in path: '{filePath}' readability success rate is {successRate:N2} " +
                           $"which can't be analysed via {ApplicationName}.", LogLevel.Warning);
                return false;
            }

            return true;
    }
    
    public async Task<IEnumerable<string>> GetPathsFromInputDirectory()
    {
        IEnumerable<string> inputPaths = Directory.EnumerateFiles(settings.InputDirectory, "*", SearchOption.AllDirectories);
        IEnumerable<string> allPaths = await archiveExtractor.ExtractArchives(inputPaths);
        return SelectAccessiblePaths(allPaths);
    }
    
    public void RemoveEmptyDirectories()
    {
        // Get all directories, deepest first
        var directories = Directory
            .GetDirectories(settings.TmpDirectory, "*", SearchOption.AllDirectories)
            .OrderByDescending(d => d.Length);

        foreach (var dir in directories)
        {
            try
            {
                // Check if directory is empty
                if (!Directory.EnumerateFileSystemEntries(dir).Any())
                    Directory.Delete(dir);
            }
            catch (Exception ex)
            {
                // Access issues, locked folders, etc.
                logger.Log($"Could not delete directory '{dir}': {ex.Message}", LogLevel.Warning);
            }
        }
    }
    
    public static void EnsureDirectoryExists(string directory)
    {
        string? directoryPath = Path.GetDirectoryName(directory);

        if (!string.IsNullOrWhiteSpace(directoryPath))
            Directory.CreateDirectory(directoryPath);
    }
}

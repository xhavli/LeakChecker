using ExcelDataReader;
using LeakChecker.DataParser.Logging;
using LeakChecker.DataParser.Logging.Execution;
using MimeDetective;
using MimeDetective.Definitions;
using MimeDetective.Definitions.Licensing;

namespace LeakChecker.DataParser.Utilities;

public class FileHandler(ExecutionLogger logger)
{
    private static string? ApplicationName { get; } = AppDomain.CurrentDomain.FriendlyName;

    private static readonly IContentInspector MimeInspector = new ContentInspectorBuilder
    {
        Definitions = new ExhaustiveBuilder { UsageType = UsageType.PersonalNonCommercial }.Build()
    }.Build();
    
    private static readonly HashSet<string> SupportedMime = new(StringComparer.InvariantCultureIgnoreCase)
    {
        "application/xml", 
        "application/json", 
        "application/vnd.ms-excel", 
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    };
    
    public async Task<bool> IsAccessible(string filePath)
    {
        try
        {
            await using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            return true;
        }
        catch (FileNotFoundException)
        {
            await logger.Log($"File in path: '{filePath}' does not exist.", LogLevel.Warning);
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            await logger.Log($"{ApplicationName} or current user does not have permission to read the file " +
                             $"in path: '{filePath}'. Raised UnauthorizedAccessException.", LogLevel.Warning);
            return false;
        }
        catch (IOException)
        {
            await logger.Log($"File in path: '{filePath}' is locked, in use or its not file. Raised IOException.", LogLevel.Warning);
            return false;
        }
    }
    
    public async Task<bool> IsSupported(string filePath, int reliableThreshold = 4000)
    {
        await using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
        
        var results = MimeInspector.Inspect(stream);
        var best = results.ByMimeType().FirstOrDefault();
        
        if (best is not null && best.Points > reliableThreshold)
        {
            if (!(SupportedMime.Contains(best.MimeType) || 
                  best.MimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)))
            {
                await logger.Log($"File in path: '{filePath}' have MIME Extension [{best.MimeType.ToLower()}] with [{best.Points}] " +
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
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    public async Task<bool> IsReadable(string filePath, int sampleLimit = 1000)
    {
            await using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var reader = new StreamReader(stream);

            int malformed = 0;
            int sampled = 0;
            
            while (!reader.EndOfStream && sampled < sampleLimit)
            {
                string? line = await reader.ReadLineAsync();
                if (line == null || line.Contains('�')) { malformed++; }

                sampled++;
            }
            
            double successRate = sampled == 0 ? 0 : Math.Max(0, (double)(sampled - malformed) / sampled  * 100);
            if (successRate < 75)
            {
                await logger.Log($"File in path: '{filePath}' readability success rate is {successRate:N2} which " +
                                 $"can't be analysed via {ApplicationName}.", LogLevel.Warning);
                return false;
            }

            return true;
    }
}

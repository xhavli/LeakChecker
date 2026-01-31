using ExcelDataReader;
using LeakChecker.Logging;
using LeakChecker.Logging.ExecutionLogging;
using MimeDetective;
using MimeDetective.Definitions;
using MimeDetective.Definitions.Licensing;

namespace LeakChecker.Utilities;

public class FileHandler(ExecutionLogger logger)
{
    private static readonly IContentInspector MimeInspector = new ContentInspectorBuilder
    {
        Definitions = new ExhaustiveBuilder { UsageType = UsageType.PersonalNonCommercial }.Build()
    }.Build();
    
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
            // throw new FileNotFoundException($"File in path: '{filePath}' does not exist.", filePath);
        }
        catch (UnauthorizedAccessException)
        {
            await logger.Log($"{Program.ApplicationName} or current user does not have permission to read the file " +
                             $"in path: '{filePath}'. Raised UnauthorizedAccessException.", LogLevel.Warning);
            return false;
            // throw new UnauthorizedAccessException($"{Program.ApplicationName} or current user does not have permission to read the file in path: '{filePath}'. Raised UnauthorizedAccessException.");
        }
        catch (IOException)
        {
            await logger.Log($"File in path: '{filePath}' is locked or in use. Raised IOException.", LogLevel.Warning);
            return false;
            // throw new IOException($"File in path: '{filePath}' is locked or in use. Raised IOException.");
        }
    }
    
    public async Task<bool> IsSupported(string filePath)
    {
        await using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
        var results = MimeInspector.Inspect(stream);

        var resultsByMime = results.ByMimeType();
        var best = resultsByMime.FirstOrDefault();
        const int reliableThreshold = 4000;
        if (best is not null && best.Points > reliableThreshold)
        {
            bool supportedMime = best.MimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
                                 best.MimeType.Equals("application/xml", StringComparison.OrdinalIgnoreCase) ||
                                 best.MimeType.Equals("application/json", StringComparison.OrdinalIgnoreCase) ||
                                 best.MimeType.Equals("application/vnd.ms-excel", StringComparison.OrdinalIgnoreCase) ||
                                 best.MimeType.Equals("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                     StringComparison.OrdinalIgnoreCase);
            if (!supportedMime)
            {
                await logger.Log($"File in path: '{filePath}' have MIME Extension [{best.MimeType.ToLower()}] with [{best.Points}] " +
                                 $"success points which can't be analysed via {Program.ApplicationName}.", LogLevel.Warning);
                return false;
                // throw new Exception($"File in path: '{filePath}' have MIME Extension [{best.MimeType.ToLower()}] with [{best.Points}] success points and its not supported to read in {Program.ApplicationName}.");
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
    
    public async Task<bool> IsReadable(string filePath)
    {
            await using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var reader = new StreamReader(stream);

            int malformed = 0;
            int sampled = 0;
            const int sampleLimit = 1000;
            
            while (!reader.EndOfStream && sampled < sampleLimit)
            {
                string? line = await reader.ReadLineAsync();
                if (line == null || line.Contains('�'))
                {
                    malformed++;
                }

                sampled++;
            }
            
            double successRate = sampled == 0 ? 0 : Math.Max(0, (double)(sampled - malformed) / sampled  * 100);
            if (successRate < 0.99)
            {
                await logger.Log($"File in path: '{filePath}' readability success rate is {successRate:N2} which is not readable.", LogLevel.Warning);
                return false;
                // throw new Exception($"File in path: '{filePath}' readable success rate is {successRate:N2} which is not readable.");
            }

            return true;
    }
}

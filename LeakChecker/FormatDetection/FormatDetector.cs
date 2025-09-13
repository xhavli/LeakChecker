using LeakChecker.Logging;
using LeakChecker.Logging.FileLogging;

namespace LeakChecker.FormatDetection;

public static class FormatDetector
{

    public static async Task<string> DetectDelimiterFromFile(FileLogger file)
    {
        await file.LogFormatProcessingStart();
        
        string? delimiter;
        using HttpClient client = new HttpClient();
        string encodedFilePath = Uri.EscapeDataString(file.FilePath);
        string url = $"http://localhost:8000/delimiter?filepath={encodedFilePath}";

        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            delimiter = await client.GetStringAsync(url);

            if (string.IsNullOrEmpty(delimiter) || delimiter.Length != 1)
            {
                await file.Log("Delimiter detection failure. Falling back to default delimiter ':'.", LogLevel.Warning, LogContext.Delimiter);
                delimiter = ":";
            }
            else
            {
                await file.Log($"Delimiter detected [{delimiter}]", LogLevel.Success, LogContext.Delimiter);
            }
        }
        catch (Exception e)
        {
            await file.Log($"Delimiter detection communication. {e.Message} Falling back to default delimiter ':'.", LogLevel.Exception, LogContext.Delimiter);
            delimiter = ":";
        }
        
        await file.Log("Delimiter detection finished successfully.", LogLevel.Info, LogContext.Delimiter);
        
        return delimiter;
    }
}
using LeakChecker.Logging;
using LeakChecker.Logging.FileLogging;

namespace LeakChecker.Format;

public static class FormatDetector
{
    public static async Task<char> DetectDelimiterFromFile(FileLogger file)
    {
        await file.LogFormatHeader();
        
        char delimiter;
        using HttpClient client = new HttpClient();
        string encodedFilePath = Uri.EscapeDataString(file.SubjectFilePath);
        string url = $"http://localhost:8000/delimiter?filepath={encodedFilePath}";

        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string stringDelimiter = await client.GetStringAsync(url);
            delimiter = stringDelimiter.ToCharArray()[0];

            if (string.IsNullOrEmpty(stringDelimiter) || stringDelimiter.Length != 1)
            {
                await file.Log("Format detection failure. Falling back to default delimiter ':'.", LogLevel.Warning, LogContext.Format);
                delimiter = ':';
            }
            else
            {
                await file.Log($"Format detected [{delimiter}]", LogLevel.Success, LogContext.Format);
            }
        }
        catch (Exception e)
        {
            await file.Log($"Format detection communication. {e.Message} Falling back to default delimiter ':'.", LogLevel.Exception, LogContext.Format);
            delimiter = ':';
        }
        
        await file.Log("Format detection finished successfully.");
        
        return delimiter;
    }
}

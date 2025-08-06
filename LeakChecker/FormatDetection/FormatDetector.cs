using System.Text.RegularExpressions;
using LeakChecker.FileTracking;

namespace LeakChecker.FormatDetection;

public class FormatDetector
{
    private readonly HttpClient _client = new HttpClient();

    public async Task<string> DetectDelimiter(FileContext file)
    {
        string encodedFilePath = Uri.EscapeDataString(file.Path);
        string url = $"http://localhost:8000/delimiter?filepath={encodedFilePath}";
        string? delimiter;

        try
        {
            HttpResponseMessage response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            delimiter = await _client.GetStringAsync(url);

            if (string.IsNullOrEmpty(delimiter) || delimiter.Length != 1)
            {
                await file.Log($"Wrong delimiter found. Falling back to default delimiter ':'.", LogLevel.Warning, LogContext.Delimiter);
                delimiter = ":";
            }
            else
            {
                await file.Log($"Detected delimiter is: '{delimiter}'", LogLevel.Info, LogContext.Delimiter);
            }
        }
        catch (Exception e)
        {
            await file.Log($"Delimiter not found. {e.Message} Falling back to default delimiter ':'.", LogLevel.Exception, LogContext.Delimiter);
            delimiter = ":";
        }

        return delimiter;
    }
}
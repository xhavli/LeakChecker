using System.Buffers.Text;
using System.Text.Json;

namespace LeakChecker.ContentDetection.ItemParsing;

public static class HashParser
{
    private const string BaseUrl = $"https://hashes.com/en/api/identifier?hash=";
    private static readonly HttpClient Client = new();

    public static async Task<(bool isHash, bool isSalted, string hashType)> TryParse(string token)
    {
        string hashType = string.Empty;
        bool isSalted = false;
        bool isHash = false;
        
        string requestUrl = BaseUrl + Uri.EscapeDataString(token);
        string requestUrlExtended = requestUrl + "&extended=true";
        try
        {
            var response = await Client.GetStringAsync(requestUrlExtended);

            var jsonDoc = JsonDocument.Parse(response);
            var root = jsonDoc.RootElement;

            isHash = root.GetProperty("success").GetBoolean();
            if (isHash)
            {
                var algorithms = root.GetProperty("algorithms").EnumerateArray();
                Console.WriteLine($"Possible hash algorithms for '{token}' :");
                foreach (var algor in algorithms)
                {
                    Console.WriteLine($"- {algor.GetString()}");
                }
                
                string algorithm = algorithms.First().ToString();

                hashType = algorithm;
                isSalted = algorithm.ToLower().Contains("salt");
                
                if (algorithm.ToUpperInvariant().Contains("BASE64"))
                {
                    if (!Base64.IsValid(token, out _))
                    {
                        isHash = false;
                        isSalted = false;
                        algorithm = string.Empty;
                    }
                }
            }
            else
            {
                // string? message = root.GetProperty("message").GetString();
                // Console.WriteLine($"Token is not hash, token: '{token}'");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"[EXCEPTION]: {e.Message}"); //TODO log properly
            isHash = false;
        }
        
        return (isHash, isSalted, hashType);
    }
}
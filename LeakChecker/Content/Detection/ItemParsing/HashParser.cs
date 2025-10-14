using System.Buffers.Text;
using System.Text.Json;

namespace LeakChecker.Content.Detection.ItemParsing;

public static class HashParser
{
    private static readonly HttpClient Client = new();
    private const string BaseUrl = "https://hashes.com/en/api/identifier?hash=";

    public static async Task<(bool isHash, bool isSalted, string hashType)> TryParse(string token)
    {
        bool isSalted = false;
        string hashType = string.Empty;
        
        if (Base64.IsValid(token)) {}   //TODO mark Base64 and decode
        
        string requestUrl = BaseUrl + Uri.EscapeDataString(token);  //Ready for simple use
        string requestUrlExtended = requestUrl + "&extended=true";  //Offers more possible algorithms sorted by its popularity

        var response = await Client.GetStringAsync(requestUrlExtended);

        var jsonDoc = JsonDocument.Parse(response);
        var root = jsonDoc.RootElement;

        bool isHash = root.GetProperty("success").GetBoolean();
        if (isHash)
        {
            var algorithms = root.GetProperty("algorithms").EnumerateArray();
            
            hashType = algorithms.First().ToString();
            isSalted = hashType.Contains("salt", StringComparison.InvariantCultureIgnoreCase);
            
            if (hashType.Replace(" ", "").Contains("Base64", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!Base64.IsValid(token, out _))
                {
                    return (false, false, string.Empty);
                }
            }
            
            Console.WriteLine($"Possible hash algorithms for '{token}':");
            foreach (var algorithm in algorithms)
            {
                Console.WriteLine($"- {algorithm.GetString()}");
            }
        }
        else
        {
            // string? message = root.GetProperty("message").GetString();
            // Console.WriteLine($"Token is not hash, token: '{token}'");
        }
        
        return (isHash, isSalted, hashType);
    }
}
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using LeakChecker.FileTracking;
using LeakChecker.Utilities;
using PhoneNumbers;

namespace LeakChecker.ContentDetector;

public class ContentDetector
{
    private static readonly object ConsoleLock = new();
    private readonly HttpClient _client = new HttpClient();
    private static readonly PhoneNumberUtil PhoneUtil = PhoneNumberUtil.GetInstance();
    
    private const string ContentPatternDeprecated = @"(?<=^|\s)(['""])(?<content>[^'""]+?)\1(?=\s|$)";
    private const string ContentPattern = @"^\s*(['""""`])(.*?)\1\s*$";
    private const RegexOptions RegexOptions = System.Text.RegularExpressions.RegexOptions.Compiled;
    private static readonly Regex ContentRegex = new(ContentPattern, RegexOptions);

    public async Task ProcessFile(FileContext file, string delimiter, Encoding encoding)
    {
        await using var fileStream = new FileStream(file.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(fileStream, encoding, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        Dictionary<RecordAttribute, int> diversityDictionary = new();
        foreach (RecordAttribute attr in Enum.GetValues(typeof(RecordAttribute))) diversityDictionary[attr] = 0;
        await file.LogContentProcessingStart();
        Stopwatch sw = Stopwatch.StartNew();

        int count = 0;
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (count == 60) break;
            count++;
            Console.WriteLine();
            Console.WriteLine($"'{file.Path}' -> {line}");
            // await ProcessFileDepr(line);
            // continue;
                    
            string[] tokens = line.Split(delimiter);
            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i].Trim();
                if (string.IsNullOrWhiteSpace(token) || string.IsNullOrEmpty(token)) continue;
                Match match = ContentRegex.Match(token);
                if (match.Success)
                {
                    token = match.Groups[2].Value;
                    Console.WriteLine($"[{i}] {token} <- {tokens[0]}");
                }

                // TODO maybe use Microsoft.Recognizers.Text.DateTime or https://github.com/EudyContreras/Chronox.NetCore
                if (DateTime.TryParse(token, out _))
                {
                    Console.WriteLine($"[{i}] {token} -> {RecordAttribute.Timestamp}");
                    
                    diversityDictionary[RecordAttribute.Timestamp]++;
                    continue;
                }

                if (IPAddress.TryParse(token, out IPAddress? address))
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork &&
                        token.Count(ch => ch == '.') == 3)
                    {
                        Console.WriteLine($"[{i}] {token} -> {RecordAttribute.IpV4Address}");
                        diversityDictionary[RecordAttribute.IpV4Address]++;
                    }
                    else if (address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        Console.WriteLine($"[{i}] {token} -> {RecordAttribute.IpV6Address}");
                        diversityDictionary[RecordAttribute.IpV6Address]++;
                    }
                    
                    continue;
                }

                if (MailAddress.TryCreate(token, out _))
                {
                    Console.WriteLine($"[{i}] {token} -> {RecordAttribute.Email}");
                    
                    diversityDictionary[RecordAttribute.Email]++;
                    continue;
                }

                if (token.Equals("male", StringComparison.CurrentCultureIgnoreCase) ||
                    token.Equals("female", StringComparison.CurrentCultureIgnoreCase))
                {
                    Console.WriteLine($"[{i}] {token} -> {RecordAttribute.Gender}");
                    
                    diversityDictionary[RecordAttribute.Gender]++;
                    continue;
                }

                string phoneNum = token.Replace(" ", "");
                if (phoneNum.All(char.IsDigit) || phoneNum.StartsWith("+"))
                {
                    string phoneNumInternational = phoneNum;
                    if (!phoneNum.StartsWith("+"))
                    {
                        phoneNumInternational = "+" + phoneNum;
                    }
                    
                    try
                    {
                        var number = PhoneUtil.Parse(phoneNumInternational, null);
                        if (PhoneUtil.IsValidNumber(number))
                        {
                            Console.WriteLine($"[{i}] {token} -> {RecordAttribute.PhoneNumber}");
                            
                            diversityDictionary[RecordAttribute.PhoneNumber]++;
                            continue;
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
                
                var urlHashes = $"https://hashes.com/en/api/identifier?hash={Uri.EscapeDataString(token)}";
                var urlHashesExtended = $"https://hashes.com/en/api/identifier?hash={Uri.EscapeDataString(token)}&extended=true";
                try
                {
                    var response = await _client.GetStringAsync(urlHashes);

                    var jsonDoc = JsonDocument.Parse(response);
                    var root = jsonDoc.RootElement;

                    bool success = root.GetProperty("success").GetBoolean();
                    if (success)
                    {
                        Console.WriteLine($"[{i}] {token} -> {RecordAttribute.Hash}");
                        
                        var algorithms = root.GetProperty("algorithms").EnumerateArray();
                        Console.WriteLine("Possible hash algorithms:");
                        foreach (var algo in algorithms)
                        {
                            Console.WriteLine($"- {algo.GetString()}");
                        }
                        
                        string algorithm = algorithms.First().ToString();
                        if (algorithm.ToLower().Contains("pass") || algorithm.ToLower().Contains("salt"))
                        {
                            diversityDictionary[RecordAttribute.SaltedHash]++;    
                        }
                        else
                        {
                            diversityDictionary[RecordAttribute.Hash]++;
                        }
                        continue;
                    }
                    else
                    {
                        string? message = root.GetProperty("message").GetString();
                        // Console.WriteLine($"hashes.com Success: {success}, Message: {message}");
                    }
                }
                catch (Exception e)
                {
                    await file.Log($"Something went wrong during communication with www.hashes.com. {e.Message}",
                        LogLevel.Exception, LogContext.Content);
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{i}] [UNRECOGNIZED] token: {token}");
                Console.ResetColor();

                diversityDictionary[RecordAttribute.Other]++;
                continue;

                string encoded = HttpUtility.UrlEncode(token);
                if (string.IsNullOrEmpty(encoded)) continue;

                // Your FastAPI endpoint
                string url = $"http://localhost:8000/category?text={encoded}";
                string category = string.Empty;

                try
                {
                    category = await _client.GetStringAsync(url);
                    Console.WriteLine($"Predicted label for token [{i}] {token} is: " + category.Trim('"'));
                }
                catch (HttpRequestException ex)
                {
                    Logger.LogError($"Request from {file.Path} for {token} failed: " + ex.Message);
                }

                if (category.Contains("mail"))
                {
                    diversityDictionary[RecordAttribute.Email]++;

                    continue;
                }

                if (category.Contains("phone") || category.Contains("number"))
                {
                    diversityDictionary[RecordAttribute.PhoneNumber]++;

                    continue;
                }

                if (category.Contains("date") || category.Contains("time"))
                {
                    diversityDictionary[RecordAttribute.Timestamp]++;

                    continue;
                }

                if (category.Contains("web") || category.ToLower().StartsWith("ur"))
                {
                    diversityDictionary[RecordAttribute.Domain] += 1;

                    continue;
                }

                // if (!diversityDictionary.TryAdd(category, 1))
                // {
                //     diversityDictionary[category] += 1;
                // }
            }
        }

        sw.Stop();
        await file.Log($"Content detection finished successfully. Time taken: {sw.Elapsed:g}, " +
                       $"current DateTime: {DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}", 
                       LogLevel.Success, LogContext.Content);
        Console.Write($"{file.Path} ");
        await file.LogContentStats(diversityDictionary);
    }

    public async Task ProcessFileDeprecated(string line)
    {
        Dictionary<string, int> diversityDictionary = new();
        string result = string.Empty;

        try
        {
            var url = $"http://localhost:8000/?text={Uri.EscapeDataString(line)}";
            using HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            result = await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError($"[EXCEPTION] response error from FastAPI: " + ex.Message);
        }

        List<PresidioEntity>? entities = JsonSerializer.Deserialize<List<PresidioEntity>>(result);
        // Sort by score DESC, then start ASC, then end ASC
        var sorted = entities
            .OrderByDescending(e => e.Score)
            .ThenBy(e => e.Start)
            .ThenBy(e => e.End)
            .ToList();

        var filtered = new List<PresidioEntity>();

        lock (ConsoleLock)
        {
            // Print with text fragment from firstLine
            foreach (var current in sorted)
            {
                bool overlaps = filtered.Any(existing =>
                    current.Start < existing.End && existing.Start < current.End);

                if (!overlaps)
                {
                    filtered.Add(current);
                }
            }

            // Print filtered results
            foreach (var entity in sorted)
            {
                string fragment = line.Substring(entity.Start, entity.End - entity.Start);
                if (diversityDictionary.ContainsKey(entity.EntityType))
                {
                    diversityDictionary[entity.EntityType] += 1;
                }
                else
                {
                    diversityDictionary[entity.EntityType] = 1;
                }
                Console.WriteLine(
                    $"{entity.EntityType} [{entity.Start}-{entity.End}], {fragment}, score={entity.Score:F2}");
            }
        }
    }
}
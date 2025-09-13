using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using LeakChecker.ContentDetection.ItemParsing;
using LeakChecker.ContentDetection.ItemRecognition;
using LeakChecker.Logging;
using LeakChecker.Logging.FileLogging;

namespace LeakChecker.ContentDetection;

public static class ContentDetector
{
    private static readonly HttpClient Client = new();
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };
    
    private const string ContentPatternDeprecated = @"(?<=^|\s)(['""])(?<content>[^'""]+?)\1(?=\s|$)";
    private const string ContentPattern = """^\s*(['""`])(.*?)\1\s*$""";
    private const RegexOptions RegexOptions = System.Text.RegularExpressions.RegexOptions.Compiled;
    private static readonly Regex ContentRegex = new(ContentPattern, RegexOptions);

    public static async Task ProcessFile(FileLogger file, Encoding encoding, string delimiter)
    {
        await file.LogContentProcessingStart();
        Stopwatch sw = Stopwatch.StartNew();
        
        await using var fileStream = new FileStream(file.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(fileStream, encoding, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        
        Dictionary<RecordAttributeEnum, int> diversityDictionary = new();
        foreach (RecordAttributeEnum attr in Enum.GetValues(typeof(RecordAttributeEnum))) diversityDictionary[attr] = 0;
        
        int linesCount = 0;
        string? line;
        
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (linesCount == 60) break;
            linesCount++;
            Console.WriteLine();
            Console.WriteLine($"'{file.FilePath}' -> {line}");

            if (TimeStampRecognizer.TryRecognize(line, out List<string> stringTimeStamps, out List<DateTime> timeStamps))
            {
                if (stringTimeStamps.Count == timeStamps.Count)
                {
                    for (int i = 0; i < stringTimeStamps.Count; i++)
                    {
                        Console.WriteLine($"{RecordAttributeEnum.TimeStamp} = {stringTimeStamps[i]}");
                        diversityDictionary[RecordAttributeEnum.TimeStamp]++;
                        
                        line = line.Replace(stringTimeStamps[i], "");           //TODO described below
                        line = line.Replace(stringTimeStamps[i].ToUpper(), ""); //TODO described below
                        
                        //TODO issue when removing content from a line
                        // 'D:Bc\Czech Republic.txt' -> 420601006009:100027196779720:Petr:Tiffanys:male:::::1/1/0001 12:00:00 AM::01/01/1987
                        // TimeStamp = 1/1
                        // TimeStamp = 12:00:00 am
                        // TimeStamp = 01/01/1987
                        // PERSON = Petr
                        // ORGANIZATION = Tiffanys
                        // [0] PhoneNumber = +420601006009
                        // [1] [UNRECOGNIZED] token: 100027196779720
                        // [4] Gender = male
                        // [9] [UNRECOGNIZED] token: /0001      <-- not removed
                        // [11] [UNRECOGNIZED] token: 01/0987   <-- not removed
                    }
                }
                else
                {
                    await file.Log("stringTimeStamps.Count != timeStamps.Count", LogLevel.Warning, LogContext.Content);
                }
            }

            if (EmailRecognizer.TryRecognize(line, out List<string> stringEmails, out List<MailAddress> emails))
            {
                if (stringEmails.Count == emails.Count)
                {
                    for (int i = 0; i < stringEmails.Count; i++)
                    {
                        Console.WriteLine($"{RecordAttributeEnum.Email} = {stringEmails[i]}");
                        diversityDictionary[RecordAttributeEnum.Email]++;
                        
                        line = line.Replace(stringEmails[i], "");
                    }
                }
                else
                {
                    await file.Log("stringEmails.Count != emails.Count", LogLevel.Warning, LogContext.Content);
                }
            }
            
            if (WebRecognizer.TryRecognize(line, out List<string> stringUris, out List<Uri> uris))
            {
                if (stringUris.Count == uris.Count)
                {
                    for (int i = 0; i < stringUris.Count; i++)
                    {
                        Console.WriteLine($"{RecordAttributeEnum.Web} = {stringUris[i]}");
                        diversityDictionary[RecordAttributeEnum.Web]++;
                        
                        line = line.Replace(stringUris[i], "");
                    }
                }
                else
                {
                    await file.Log("stringUris.Count != uris.Count", LogLevel.Warning, LogContext.Content);
                }
            }
            
            if (GuidRecognizer.TryRecognize(line, out List<string> stringGuids, out List<Guid> guids))
            {
                if (stringGuids.Count == guids.Count)
                {
                    for (int i = 0; i < stringGuids.Count; i++)
                    {
                        Console.WriteLine($"{RecordAttributeEnum.Id} = {stringGuids[i]}");
                        diversityDictionary[RecordAttributeEnum.Id]++;
                     
                        line = line.Replace(stringGuids[i], "");
                    }
                }
                else
                {
                    await file.Log("stringGuids.Count != guids.Count", LogLevel.Warning, LogContext.Content);
                }
            }

            List<PresidioEntity>? analyzeResult;
            try
            {
                var url = $"http://localhost:8000/analyze?text={Uri.EscapeDataString(line)}";
                
                var analyzeResponse = await Client.GetAsync(url);
                analyzeResponse.EnsureSuccessStatusCode();

                var json = await analyzeResponse.Content.ReadAsStringAsync();
                analyzeResult =  JsonSerializer.Deserialize<List<PresidioEntity>>(json, Options);
            }
            catch (Exception e)
            {
                await file.Log($"Flair communication failed. {e.Message}", LogLevel.Exception, LogContext.Content);
                throw;
            }
            
            // Print filtered results
            try
            {
                foreach (var entity in MergeEntities(analyzeResult))
                {
                    string fragment = line.Substring(entity.Start, entity.End - entity.Start);
                    if (entity.Type.Equals("person", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // fragment = fragment.Replace(delimiter, " ");
                        diversityDictionary[RecordAttributeEnum.Name]++;
                    }
                    else if (entity.Type.Equals("location", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // fragment = fragment.Replace(delimiter, ", ");
                        diversityDictionary[RecordAttributeEnum.Location]++;
                    }
                    else if (entity.Type.Equals("organization", StringComparison.InvariantCultureIgnoreCase))
                    {
                        diversityDictionary[RecordAttributeEnum.Organization]++;
                    }
                    
                    Console.WriteLine($"{entity.Type} = {fragment}");
                }
                
                foreach (var entity in MergeEntities(analyzeResult).OrderByDescending(e => e.Start))
                {
                    line = line.Remove(entity.Start, entity.End - entity.Start);
                }
            }
            catch (Exception e)
            {
                await file.Log($"Fragment handling failed. {e.Message}", LogLevel.Exception, LogContext.Content);
                throw;
            }


            string[] tokens = line.Split(delimiter);
            
            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i].Trim();
                Match match = ContentRegex.Match(token);
                if (match.Success)
                {
                    token = match.Groups[2].Value;
                    Console.WriteLine($"[{i}] {token} <- {tokens[i]}");
                }
                if (string.IsNullOrWhiteSpace(token) || string.IsNullOrEmpty(token)) continue;

                if (IPAddress.TryParse(token, out IPAddress? ipAddress))
                {
                    if (ipAddress.AddressFamily == AddressFamily.InterNetwork &&
                        token.Count(ch => ch == '.') == 3)
                    {
                        Console.WriteLine($"[{i}] {token} -> {RecordAttributeEnum.IpV4Address}");
                        diversityDictionary[RecordAttributeEnum.IpV4Address]++;
                    }
                    else if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        Console.WriteLine($"[{i}] {token} -> {RecordAttributeEnum.IpV6Address}");
                        diversityDictionary[RecordAttributeEnum.IpV6Address]++;
                    }
                    
                    continue;
                }

                if (GenderParser.TryParse(token, out string gender))
                {
                    Console.WriteLine($"[{i}] {RecordAttributeEnum.Gender} = {token}");
                    
                    diversityDictionary[RecordAttributeEnum.Gender]++;
                    continue;
                }

                if (MaritalStatusParser.TryParse(token, out string? maritalStatus))
                {
                    Console.WriteLine($"[{i}] {RecordAttributeEnum.MaritalStatus} = {maritalStatus}");
                    
                    diversityDictionary[RecordAttributeEnum.MaritalStatus]++;
                    continue;
                }
                
                if (PhoneNumberParser.TryParse(token, out string phoneNumber))
                {
                    Console.WriteLine($"[{i}] {RecordAttributeEnum.PhoneNumber} = {phoneNumber}");
                            
                    diversityDictionary[RecordAttributeEnum.PhoneNumber]++;
                    continue;
                }
                
                var (isHash, withSalt, algorythm) = await HashParser.TryParse(token);
                if (isHash && !withSalt)
                {
                    diversityDictionary[RecordAttributeEnum.Hash]++;
                    continue;
                }
                if (isHash && withSalt)
                {
                    diversityDictionary[RecordAttributeEnum.SaltedHash]++;
                    continue;
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{i}] [UNRECOGNIZED] token: {token}");
                Console.ResetColor();

                diversityDictionary[RecordAttributeEnum.Other]++;
                continue;

                string encoded = HttpUtility.UrlEncode(token);
                if (string.IsNullOrEmpty(encoded)) continue;

                // Your FastAPI endpoint
                var url = $"http://localhost:8000/classify?token={encoded}";
                string category = string.Empty;

                try
                {
                    category = await Client.GetStringAsync(url);
                    Console.WriteLine($"Predicted label for token [{i}] {token} is: " + category.Trim('"'));
                }
                catch (HttpRequestException ex)
                {
                    await file.Log($"Request from {file.FilePath} for {token} failed: " + ex.Message);
                }

                if (category.Contains("mail"))
                {
                    diversityDictionary[RecordAttributeEnum.Email]++;

                    continue;
                }

                if (category.Contains("phone") || category.Contains("number"))
                {
                    diversityDictionary[RecordAttributeEnum.PhoneNumber]++;

                    continue;
                }

                if (category.Contains("date") || category.Contains("time"))
                {
                    diversityDictionary[RecordAttributeEnum.TimeStamp]++;

                    continue;
                }

                if (category.Contains("web") || category.ToLower().StartsWith("ur"))
                {
                    diversityDictionary[RecordAttributeEnum.Web] += 1;

                    continue;
                }

                // if (!diversityDictionary.TryAdd(category, 1))
                // {
                //     diversityDictionary[category] += 1;
                // }
            }
        }

        await file.Log($"Content processing finished successfully. Time taken: {sw.Elapsed}, current DateTime: " +
                       $"{DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}", LogLevel.Success, LogContext.Content);
        await file.Log($"Lines processed count: {linesCount}");
        await file.LogContentStats(diversityDictionary);
    }

    private static List<PresidioEntity> MergeEntities(List<PresidioEntity>? entities, int maxGap = 2)
    {
        if (entities == null || !entities.Any()) return new List<PresidioEntity>();

        // Ensure entities are sorted by start index
        entities = entities.OrderBy(e => e.Start).ToList();

        var current = entities[0];
        List<PresidioEntity> result = new();

        for (int i = 1; i < entities.Count; i++)
        {
            var next = entities[i];

            // Check if same type AND close enough
            if (next.Type == current.Type && next.Start - current.End <= maxGap)
            {
                // Merge: extend the end position
                current.End = next.End;
                // Keep the max score (or average if you want)
                current.Score = Math.Min(current.Score, next.Score);
            }
            else
            {
                result.Add(current);
                current = next;
            }
        }

        result.Add(current);
        return result;
    }
}
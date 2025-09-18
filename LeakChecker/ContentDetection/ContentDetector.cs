using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using LeakChecker.ContentDetection.ItemParsing;
using LeakChecker.ContentDetection.ItemRecognition;
using LeakChecker.ContentDetection.RecognitionService;
using LeakChecker.Logging;
using LeakChecker.Logging.FileLogging;

namespace LeakChecker.ContentDetection;

public static class ContentDetector
{
    // private const string ContentPatternDeprecated = @"(?<=^|\s)(['""])(?<content>[^'""]+?)\1(?=\s|$)";
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

        while (await reader.ReadLineAsync() is { } line)
        {
            if (linesCount == 60) break;
            // if (linesCount == 10000) break;
            
            linesCount++;
            Console.WriteLine();
            Console.WriteLine($"'{file.FilePath}' [{linesCount}] [{sw.Elapsed:g}] ===> {line}");

            if (TimeStampRecognizer.TryRecognize(line, out List<string> stringTimeStamps, out List<DateTime> timeStamps))
            {
                if (stringTimeStamps.Count == timeStamps.Count)
                {
                    foreach (var timeStamp in stringTimeStamps)
                    {
                        Console.WriteLine($"{RecordAttributeEnum.TimeStamp} = {timeStamp}");
                        diversityDictionary[RecordAttributeEnum.TimeStamp]++;

                        line = line.Replace(timeStamp, "", StringComparison.OrdinalIgnoreCase);
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
                    foreach (var email in stringEmails)
                    {
                        Console.WriteLine($"{RecordAttributeEnum.Email} = {email}");
                        diversityDictionary[RecordAttributeEnum.Email]++;
                        
                        line = line.Replace(email, "", StringComparison.InvariantCultureIgnoreCase);
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
                    foreach (var uri in stringUris.OrderByDescending(s => s.Length))
                    {
                        Console.WriteLine($"{RecordAttributeEnum.Web} = {uri}");
                        diversityDictionary[RecordAttributeEnum.Web]++;
                        
                        line = line.Replace(uri, "");
                    }
                }
                else
                {
                    await file.Log("stringUris.Count != uris.Count", LogLevel.Warning, LogContext.Content);
                }
            }
            
            try
            {
                List<PresidioEntity> analyzeResults = await PythonNerServiceRecognizer.TryRecognize(line);
                foreach (var entity in analyzeResults)
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
                
                foreach (var entity in analyzeResults.OrderByDescending(e => e.Start))
                {
                    line = line.Remove(entity.Start, entity.End - entity.Start);
                }
            }
            catch (Exception e)
            {
                await file.Log($"Communication with PythonNerService failed. {e.Message}", LogLevel.Exception, LogContext.Content);
            }
            
            if (GuidRecognizer.TryRecognize(line, out List<string> stringGuids, out List<Guid> guids))
            {
                if (stringGuids.Count == guids.Count)
                {
                    foreach (var guid in stringGuids)
                    {
                        Console.WriteLine($"{RecordAttributeEnum.Id} = {guid}");
                        diversityDictionary[RecordAttributeEnum.Id]++;
                     
                        line = line.Replace(guid, "");
                    }
                }
                else
                {
                    await file.Log("stringGuids.Count != guids.Count", LogLevel.Warning, LogContext.Content);
                }
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
                        Console.WriteLine($"[{i}] {token} = {RecordAttributeEnum.IpV4Address}");
                        diversityDictionary[RecordAttributeEnum.IpV4Address]++;
                    }
                    else if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        Console.WriteLine($"[{i}] {token} = {RecordAttributeEnum.IpV6Address}");
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

                if (MaritalStatusParser.TryParse(token, out string maritalStatus))
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

                try
                {
                    var (isHash, withSalt, algorithm) = await HashParser.TryParse(token);
                    if (isHash && !withSalt)
                    {
                        Console.WriteLine($"[{i}] {RecordAttributeEnum.Hash} = algorithm: {algorithm}, token: {token}");
                        
                        diversityDictionary[RecordAttributeEnum.Hash]++;
                        continue;
                    }
                    if (isHash && withSalt)
                    {
                        Console.WriteLine($"[{i}] {RecordAttributeEnum.SaltedHash} = algorithm: {algorithm}, token: {token}");
                        
                        diversityDictionary[RecordAttributeEnum.SaltedHash]++;
                        continue;
                    }
                }
                catch (Exception e)
                {
                    await file.Log($"Communication with www.hashes.com failed. {e.Message}", LogLevel.Exception, LogContext.Content);
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{i}] [UNRECOGNIZED TOKEN]: {token}");
                Console.ResetColor();

                diversityDictionary[RecordAttributeEnum.Other]++;
            }
        }

        await file.Log($"Content processing finished successfully. Time taken: {sw.Elapsed}, current DateTime: " +
                       $"{DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}", LogLevel.Success, LogContext.Content);
        await file.Log($"Lines processed count: {linesCount}");
        await file.LogContentStats(diversityDictionary);
    }
}
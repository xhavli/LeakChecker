using System.Net.Mail;
using System.Net.NetworkInformation;
using LeakChecker.Content.Detection.ItemParsing;
using LeakChecker.Content.Detection.ItemRecognition;
using LeakChecker.Content.Detection.RecognitionService;
using LeakChecker.Format;
using LeakChecker.Logging;
using LeakChecker.Logging.FileLogging;
using LeakChecker.Utilities.Extensions;

namespace LeakChecker.Content.Detection;

public static class ContentDetector
{
    public static async Task<List<SchemaHeuristicRecord>> DetectLine(string line, char delimiter, FileLogger logger)
    {
        List<SchemaHeuristicRecord> linePatterns = new();
        string originLine = line;
        
        //TODO
        // line = line.Trim().TrimOuterParenthesesAndComma();
        // if (string.IsNullOrEmpty(line)) return linePatterns;
        // Console.WriteLine(line);
        
        if (TimeStampRecognizer.TryRecognize(line, out List<string> stringTimeStamps, out List<DateTime> timeStamps))
        {
            if (stringTimeStamps.Count == timeStamps.Count)
            {
                foreach (var timeStamp in stringTimeStamps.OrderByDescending(ts => ts.Length))
                {
                    int position = CountDelimitersBefore(originLine, timeStamp, delimiter);
                    Console.WriteLine($"[{position}] {ItemEnum.TimeStamp} = {timeStamp}");

                    linePatterns.Add(new SchemaHeuristicRecord
                    {
                        Attribute = ItemEnum.TimeStamp,
                        Position = position,
                        DelimitersInside = timeStamp.Count(ch => ch == delimiter)
                    });
                    line = line.Replace(timeStamp, "", StringComparison.InvariantCultureIgnoreCase);
                }
            }
            else
            {
                await logger.Log("stringTimeStamps.Count != timeStamps.Count", LogLevel.Warning, LogContext.Content);
            }
        }

        if (EmailRecognizer.TryRecognize(line, out List<string> stringEmails, out List<MailAddress> emails))
        {
            if (stringEmails.Count == emails.Count)
            {
                foreach (var email in stringEmails)
                {
                    int position = CountDelimitersBefore(originLine, email, delimiter);
                    Console.WriteLine($"[{position}] {ItemEnum.Email} = {email}");

                    linePatterns.Add(new SchemaHeuristicRecord
                    {
                        Attribute = ItemEnum.Email,
                        Position = position,
                        DelimitersInside = email.Count(ch => ch == delimiter)
                    });
                    line = line.Replace(email, "", StringComparison.InvariantCultureIgnoreCase);
                }
            }
            else
            {
                await logger.Log("stringEmails.Count != emails.Count", LogLevel.Warning, LogContext.Content);
            }
        }

        if (WebRecognizer.TryRecognize(line, out List<string> stringUris, out List<Uri> uris))
        {
            if (stringUris.Count == uris.Count)
            {
                foreach (var uri in stringUris.OrderByDescending(s => s.Length))
                {
                    int position = CountDelimitersBefore(originLine, uri, delimiter);
                    Console.WriteLine($"[{position}] {ItemEnum.Web} = {uri}");

                    linePatterns.Add(new SchemaHeuristicRecord
                    {
                        Attribute = ItemEnum.Web,
                        Position = position,
                        DelimitersInside = uri.Count(ch => ch == delimiter)
                    });
                    line = line.Replace(uri, "");
                }
            }
            else
            {
                await logger.Log("stringUris.Count != uris.Count", LogLevel.Warning, LogContext.Content);
            }
        }

        try
        {
            List<PresidioEntity> analyzeResults = await PythonNerServiceRecognizer.TryRecognize(line);
            foreach (var entity in analyzeResults.OrderBy(e => e.Start))
            {
                string item = line.Substring(entity.Start, entity.End - entity.Start);
                int position = CountDelimitersBefore(originLine, item, delimiter);
                if (entity.Type.Equals("PERSON"))
                {
                    linePatterns.Add(new SchemaHeuristicRecord
                    {
                        Attribute = ItemEnum.Name,
                        Position = position,
                        DelimitersInside = item.Count(ch => ch == delimiter)
                    });
                }
                else if (entity.Type.Equals("LOCATION"))
                {
                    linePatterns.Add(new SchemaHeuristicRecord
                    {
                        Attribute = ItemEnum.Location,
                        Position = position,
                        DelimitersInside = item.Count(ch => ch == delimiter)
                    });
                }
                else if (entity.Type.Equals("ORGANIZATION"))
                {
                    linePatterns.Add(new SchemaHeuristicRecord
                    {
                        Attribute = ItemEnum.Organization,
                        Position = position,
                        DelimitersInside = item.Count(ch => ch == delimiter)
                    });
                }

                Console.WriteLine($"[{position}] {entity.Type} = {item}");
            }

            foreach (var entity in analyzeResults.OrderByDescending(e => e.Start))
            {
                line = line.Remove(entity.Start, entity.End - entity.Start);
            }
        }
        catch (Exception e)
        {
            await logger.Log($"Communication with PythonNerService failed. {e.Message}", LogLevel.Exception,LogContext.Content);
        }

        if (GuidRecognizer.TryRecognize(line, out List<string> stringGuids, out List<Guid> guids))
        {
            if (stringGuids.Count == guids.Count)
            {
                foreach (var guid in stringGuids)
                {
                    int position = CountDelimitersBefore(originLine, guid, delimiter);
                    Console.WriteLine($"[{position}] {ItemEnum.Id} = {guid}");

                    linePatterns.Add(new SchemaHeuristicRecord
                    {
                        Attribute = ItemEnum.Id,
                        Position = position,
                        DelimitersInside = guid.Count(ch => ch == delimiter)
                    });
                    line = line.Replace(guid, "");
                }
            }
            else
            {
                await logger.Log("stringGuids.Count != guids.Count", LogLevel.Warning, LogContext.Content);
            }
        }

        string[] tokens = line.Split(delimiter);
        for (int i = 0; i < tokens.Length; i++)
        {
            bool skip = false;
            foreach (var linePattern in linePatterns)
            {
                if (linePattern.Position == i) skip = true;
            }
            if (skip) continue;

            string token = tokens[i].Trim().TrimOuterQuotes();
            if (string.IsNullOrEmpty(token)) continue;

            ItemEnum itemType = await DetectToken(token, logger);

            if (itemType != ItemEnum.Other)
            {
                int position = CountDelimitersBefore(originLine, token, delimiter);
                Console.WriteLine($"[{position}] {itemType} = {token}");
                
                linePatterns.Add(new SchemaHeuristicRecord
                {
                    Attribute = itemType,
                    Position = position,
                    DelimitersInside = token.Count(ch => ch == delimiter)
                });
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{i}] [UNRECOGNIZED TOKEN]: {token}");
                Console.ResetColor();
                linePatterns.Add(new SchemaHeuristicRecord
                {
                    Attribute = itemType,
                    Position = i,
                    DelimitersInside = token.Count(ch => ch == delimiter)
                });
            }
        }
        
        return linePatterns;
    }
    
    public static async Task<ItemEnum> DetectToken(string token, FileLogger logger)
    {
        //TODO how to handle empty im heuristic analysis if it will be None, Empty or Other
        if (string.IsNullOrEmpty(token) || string.IsNullOrWhiteSpace(token)) return ItemEnum.Other;
        
        if (TimeStampRecognizer.TryRecognize(token, out _, out _)) { return ItemEnum.TimeStamp; }

        if (EmailRecognizer.TryRecognize(token, out _, out _)) { return ItemEnum.Email; }
        
        if (WebRecognizer.TryRecognize(token, out _, out _)) { return ItemEnum.Web; }
        
        try
        {
            List<PresidioEntity> analyzeResults = await PythonNerServiceRecognizer.TryRecognize(token);
            if (analyzeResults.Any())
            {
                var entity = analyzeResults.First();
                
                if (entity.Type.Equals("PERSON")) { return ItemEnum.Name; }

                if (entity.Type.Equals("LOCATION")) { return ItemEnum.Location; }

                if (entity.Type.Equals("ORGANIZATION")) { return ItemEnum.Organization; }
            }
        }
        catch (Exception e)
        {
            await logger.Log($"Communication with PythonNerService failed. {e.Message}", LogLevel.Exception, LogContext.Content);
        }
        
        if (GuidRecognizer.TryRecognize(token, out _, out _)) { return ItemEnum.Id; }

        if (GenderParser.TryParse(token, out _)) { return ItemEnum.Gender; }

        if (MaritalStatusParser.TryParse(token, out _)) { return ItemEnum.MaritalStatus;}
        
        if (IpAddressParser.TryParse(token, out ItemEnum itemType, out _)) { return itemType; }
        
        if (PhoneNumberParser.TryParse(token, out _)) { return ItemEnum.PhoneNumber; }

        if (PhysicalAddress.TryParse(token, out PhysicalAddress? mac) && mac.GetAddressBytes().Length == 6)
        { return ItemEnum.Mac; }

        //TODO bypas for faster detection in development because www.hashes.com responds take a while
        if (TimeStampParser.TryParse(token, out _)) { return ItemEnum.TimeStamp; }
        // Console.ForegroundColor = ConsoleColor.Red;
        // Console.WriteLine($"[UNRECOGNIZED TOKEN]: {token}");
        // Console.ResetColor();
        // return ItemEnum.Other;
        
        try
        {
            var (isHash, withSalt, _) = await HashParser.TryParse(token);
            
            if (isHash && !withSalt) { return ItemEnum.Hash; }

            if (isHash && withSalt) { return ItemEnum.SaltedHash; }
        }
        catch (Exception e)
        {
            
            await logger.Log($"Communication with www.hashes.com failed. {e.Message}", LogLevel.Exception, LogContext.Content);
        }
        
        if (TimeStampParser.TryParse(token, out _)) { return ItemEnum.TimeStamp; }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[UNRECOGNIZED TOKEN]: {token}");
        Console.ResetColor();
        return ItemEnum.Other;
    }
    
    private static async Task<ItemEnum> ParseToken(string token, FileLogger logger)
    {
        if (IpAddressParser.TryParse(token, out ItemEnum itemType, out _)) { return itemType; }

        if (GenderParser.TryParse(token, out _)) { return ItemEnum.Gender; }

        if (MaritalStatusParser.TryParse(token, out _)) { return ItemEnum.MaritalStatus;}
        
        if (PhoneNumberParser.TryParse(token, out _)) { return ItemEnum.PhoneNumber; }

        if (PhysicalAddress.TryParse(token, out PhysicalAddress? mac) && mac.GetAddressBytes().Length == 6)
        { return ItemEnum.Mac; }

        //TODO bypas for faster detection in development because www.hashes.com responds take a while
        if (TimeStampParser.TryParse(token, out _)) { return ItemEnum.TimeStamp; }
        return ItemEnum.Other;
        
        try
        {
            var (isHash, withSalt, algorithm) = await HashParser.TryParse(token);
            
            if (isHash && !withSalt) { return ItemEnum.Hash; }

            if (isHash && withSalt) { return ItemEnum.SaltedHash; }
        }
        catch (Exception e)
        {
            
            await logger.Log($"Communication with www.hashes.com failed. {e.Message}", LogLevel.Exception, LogContext.Content);
        }
        
        if (TimeStampParser.TryParse(token, out _)) { return ItemEnum.TimeStamp; }

        return ItemEnum.Other;
    }

    private static int CountDelimitersBefore(string line, string word, char delimiter)
    {
        int index = line.IndexOf(word, StringComparison.InvariantCultureIgnoreCase);
        if (index <= 0) return 0;

        ReadOnlySpan<char> span = line.AsSpan(0, index);
        int count = 0;
        foreach (char c in span)
        {
            if (c == delimiter)
                count++;
        }
        return count;
    }
}
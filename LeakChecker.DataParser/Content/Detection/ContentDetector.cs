using System.Net.Mail;
using LeakChecker.DataParser.Content.Detection.ItemParsing;
using LeakChecker.DataParser.Content.Detection.ItemRecognition;
using LeakChecker.DataParser.Content.Detection.RecognitionService;
using LeakChecker.DataParser.Format.Schema;
using LeakChecker.DataParser.Helpers.Extensions;
using LeakChecker.DataParser.Logging;
using LeakChecker.DataParser.Logging.Parse;

namespace LeakChecker.DataParser.Content.Detection;

public static class ContentDetector
{
    public static async Task<List<SchemaHeuristicRecord>> DetectLine(string line, char delimiter, IParseLogger logger)
    {
        line = line.Trim();
        string originalLine = line;
        List<SchemaHeuristicRecord> linePatterns = new();
        
        if (WebRecognizer.TryRecognize(line, out List<string> stringUris, out List<Uri> uris))
        {
            if (stringUris.Count == uris.Count)
            {
                foreach (var uri in stringUris.OrderByDescending(s => s.Length))
                {
                    int position = CountDelimitersBefore(originalLine, uri, delimiter);
                    // Console.WriteLine($"[{position}] {ItemEnum.Web} = {uri}");

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
        
        if (EmailRecognizer.TryRecognize(line, out List<string> stringEmails, out List<MailAddress> emails))
        {
            if (stringEmails.Count == emails.Count)
            {
                foreach (var email in stringEmails)
                {
                    int position = CountDelimitersBefore(originalLine, email, delimiter);
                    // Console.WriteLine($"[{position}] {ItemEnum.Email} = {email}");

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
        
        if (TimestampRecognizer.TryRecognize(line, out List<string> stringTimeStamps, out List<DateTime> timeStamps))
        {
            if (stringTimeStamps.Count == timeStamps.Count)
            {
                foreach (var timeStamp in stringTimeStamps.OrderByDescending(ts => ts.Length))
                {
                    int position = CountDelimitersBefore(originalLine, timeStamp, delimiter);
                    // Console.WriteLine($"[{position}] {ItemEnum.TimeStamp} = {timeStamp}");

                    linePatterns.Add(new SchemaHeuristicRecord
                    {
                        Attribute = ItemEnum.Timestamp,
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
        
        try
        {
            List<PresidioEntity> analyzeResults = await PythonNerServiceRecognizer.TryRecognize(line);
            foreach (var entity in analyzeResults.OrderBy(e => e.Start))
            {
                ItemEnum itemType = PythonNerServiceRecognizer.MapEntityType(entity.Type);
                string item = line.Substring(entity.Start, entity.End - entity.Start);
                int position = CountDelimitersBefore(originalLine, item, delimiter);
                
                linePatterns.Add(new SchemaHeuristicRecord
                {
                    Attribute = itemType,
                    Position = position,
                    DelimitersInside = item.Count(ch => ch == delimiter)
                });

                // Console.WriteLine($"[{position}] {itemType.Value} = {item}");
            }

            foreach (var entity in analyzeResults.OrderByDescending(e => e.Start))
            {
                line = line.Remove(entity.Start, entity.End - entity.Start);
            }
        }
        catch (Exception e)
        {
            // throw new NotImplementedException($"Communication with PythonNerService failed. {e.Message}");
            await logger.Log($"Communication with PythonNerService failed. {e.Message}", LogLevel.Failure,LogContext.Content);
        }
        
        if (GuidRecognizer.TryRecognize(line, out List<string> stringGuids, out List<Guid> guids))
        {
            if (stringGuids.Count == guids.Count)
            {
                foreach (var guid in stringGuids)
                {
                    int position = CountDelimitersBefore(originalLine, guid, delimiter);
                    // Console.WriteLine($"[{position}] {ItemEnum.Id} = {guid}");

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
            if (linePatterns.Any(lp => lp.Position == i))
                continue;

            string token = tokens[i].Trim().TrimEnclosingChars();
            
            if (string.IsNullOrWhiteSpace(token))
                continue;

            ItemEnum itemType = await DetectToken(token, logger);
            
            //TODO test hashes with salt and ip with ports
            if (false)
            {
                // for hash:salt
                //if (itemType > ItemEnum.Other); //then try to add salt
                if (i + 1 < tokens.Length)
                {
                    ItemEnum nextType = await DetectToken(tokens[i].Trim().TrimEnclosingChars(), logger);
                    if (nextType != ItemEnum.Other) continue;
                    
                    string concatenated = token + delimiter + tokens[i].Trim().TrimEnclosingChars();
                    itemType = await DetectToken(concatenated, logger);
                }
                
                // for hashType:iterations:hashValue:salt or IPv6:port
                for(int j = 0; j < 2 || i + j < tokens.Length; j++)
                {
                        string concatenated = token + delimiter + token;
                        itemType = await DetectToken(concatenated, logger);
                }
            }
            
            linePatterns.Add(new SchemaHeuristicRecord
            {
                Position = i,
                Attribute = itemType,
                DelimitersInside = token.Count(ch => ch == delimiter)
            });
            // Console.WriteLine($"[{i}] {itemType} = {token}");
        }
        
        return linePatterns;
    }
    
    public static async Task<ItemEnum> DetectToken(string token, IParseLogger logger)
    {
        if (WebRecognizer.TryRecognize(token, out _, out _))
            return ItemEnum.Web;
        
        if (EmailRecognizer.TryRecognize(token, out _, out _))
            return ItemEnum.Email;
        
        if (TimestampRecognizer.TryRecognize(token, out _, out _))
            return ItemEnum.Timestamp;
        
        try
        {
            var itemType = await PythonNerServiceRecognizer.TryRecognizeToken(token);
            if (itemType != null)
                return itemType.Value;
        }
        catch (Exception e)
        {
            // throw new NotImplementedException($"Communication with PythonNerService failed. {e.Message}");
            await logger.Log($"Communication with PythonNerService failed. {e.Message}", LogLevel.Failure, LogContext.PythonNerService);
        }
        
        if (GuidRecognizer.TryRecognize(token, out _, out _))
            return ItemEnum.Id;
        
        if (GenderParser.TryParse(token, out _))
            return ItemEnum.Gender;
        
        if (MaritalStatusParser.TryParse(token, out _))
            return ItemEnum.MaritalStatus;
        
        if (IpAddressParser.TryParse(token, out ItemEnum ipType, out _))
            return ipType;
        
        if (PhoneNumberParser.TryParse(token, out _))
            return ItemEnum.PhoneNumber;
        
        if (MacAddressParser.TryParse(token, out _))
            return ItemEnum.Mac;
        
        if (IbanParser.TryParse(token))
            return ItemEnum.Iban;
        
        if (TimestampParser.TryParse(token, out ItemEnum timeType, out _))
            return timeType;
        
        try
        {
            var hashType = await HashParser.TryParse(token);
            if (hashType != null)
                return hashType.Value;
        }
        catch (Exception e)
        {
            // throw new NotImplementedException($"Communication with www.hashes.com failed. {e.Message}");
            await logger.Log($"Communication with www.hashes.com failed. {e.Message}", LogLevel.Failure, LogContext.ExternalService);
        }
        
        return ItemEnum.Other;
    }

    private static int CountDelimitersBefore(string line, string word, char delimiter)
    {
        int index = line.IndexOf(word, StringComparison.InvariantCultureIgnoreCase);
        if (index <= 0)
            return 0;

        ReadOnlySpan<char> span = line.AsSpan(0, index);
        
        int count = 0;
        foreach (char ch in span)
        {
            if (ch == delimiter)
                count++;
        }
        return count;
    }
}
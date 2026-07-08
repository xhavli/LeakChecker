using LeakChecker.Common.Enums;
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
        
        if (WebRecognizer.TryRecognize(line, out List<string> uris, out _))
        {
            foreach (var uri in uris.OrderByDescending(s => s.Length))
            {
                int position = CountDelimitersBefore(originalLine, uri, delimiter);
                AddLinePatterns(linePatterns, position, ItemType.Web, uri, delimiter);
                line = line.Replace(uri, "");
            }
        }
        
        if (EmailRecognizer.TryRecognize(line, out List<string> emails, out _))
        {
            foreach (var email in emails)
            {
                int position = CountDelimitersBefore(originalLine, email, delimiter);
                AddLinePatterns(linePatterns, position, ItemType.Email, email, delimiter);
                line = line.Replace(email, "", StringComparison.InvariantCultureIgnoreCase);
            }
        }
        
        if (TimestampRecognizer.TryRecognize(line, out List<string> timeStamps, out _))
        {
            foreach (var timeStamp in timeStamps.OrderByDescending(ts => ts.Length))
            {
                int position = CountDelimitersBefore(originalLine, timeStamp, delimiter);
                AddLinePatterns(linePatterns, position, ItemType.Timestamp, timeStamp, delimiter);
                line = line.Replace(timeStamp, "", StringComparison.InvariantCultureIgnoreCase);
            }
        }
        
        try
        {
            List<PresidioEntity> analyzeResults = await PythonNerServiceRecognizer.TryRecognize(line);
            foreach (var entity in analyzeResults.OrderBy(e => e.Start))
            {
                ItemType itemType = PythonNerServiceRecognizer.MapEntityType(entity.Type);
                string item = line.Substring(entity.Start, entity.End - entity.Start);
                int position = CountDelimitersBefore(originalLine, item, delimiter);
                AddLinePatterns(linePatterns, position, itemType, item, delimiter);
            }

            foreach (var entity in analyzeResults.OrderByDescending(e => e.Start))
            {
                line = line.Remove(entity.Start, entity.End - entity.Start);
            }
        }
        catch (Exception e)
        {
            // throw new NotImplementedException($"Communication with PythonNerService failed. {e.Message}");
            logger.Log($"Communication with PythonNerService failed. {e.Message}", LogLevel.Failure,LogContext.Content);
        }
        
        if (GuidRecognizer.TryRecognize(line, out List<string> stringGuids, out _))
        {
            foreach (var guid in stringGuids)
            {
                int position = CountDelimitersBefore(originalLine, guid, delimiter);
                AddLinePatterns(linePatterns, position, ItemType.Id, guid, delimiter);
                line = line.Replace(guid, "");
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

            ItemType itemType = await DetectToken(token, logger);
            
            //TODO test hashes with salt and ip with ports
            if (false)
            {
                // for hash:salt
                //if (itemType > ItemEnum.Other); //then try to add salt
                if (i + 1 < tokens.Length)
                {
                    ItemType nextType = await DetectToken(tokens[i].Trim().TrimEnclosingChars(), logger);
                    if (nextType != ItemType.Other) continue;
                    
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
            
            AddLinePatterns(linePatterns, i, itemType, token, delimiter);
        }
        
        return linePatterns;
    }
    
    public static async Task<ItemType> DetectToken(string token, IParseLogger logger)
    {
        if (WebRecognizer.TryRecognize(token, out _, out _))
            return ItemType.Web;
        
        if (EmailRecognizer.TryRecognize(token, out _, out _))
            return ItemType.Email;
        
        if (TimestampRecognizer.TryRecognize(token, out _, out _))
            return ItemType.Timestamp;
        
        try
        {
            var itemType = await PythonNerServiceRecognizer.TryRecognizeToken(token);
            if (itemType != null)
                return itemType.Value;
        }
        catch (Exception e)
        {
            // throw new NotImplementedException($"Communication with PythonNerService failed. {e.Message}");
            logger.Log($"Communication with PythonNerService failed. {e.Message}", LogLevel.Failure, LogContext.PythonNerService);
        }
        
        if (GuidRecognizer.TryRecognize(token, out _, out _))
            return ItemType.Id;
        
        if (GenderParser.TryParse(token, out _))
            return ItemType.Gender;
        
        if (MaritalStatusParser.TryParse(token, out _))
            return ItemType.MaritalStatus;
        
        if (IpAddressParser.TryParse(token, out ItemType ipType, out _))
            return ipType;
        
        if (PhoneNumberParser.TryParse(token, out _))
            return ItemType.PhoneNumber;
        
        if (MacAddressParser.TryParse(token, out _))
            return ItemType.MacAddress;
        
        if (IbanParser.TryParse(token))
            return ItemType.Iban;
        
        if (TimestampParser.TryParse(token, out ItemType timeType, out _))
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
            logger.Log($"Communication with www.hashes.com failed. {e.Message}", LogLevel.Failure, LogContext.ExternalService);
        }
        
        return ItemType.Other;
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
    
    private static void AddLinePatterns(List<SchemaHeuristicRecord> linePatterns, int position, ItemType itemType, string itemValue, char delimiter)
    {
        linePatterns.Add(new SchemaHeuristicRecord
        {
            Attribute = itemType,
            Position = position,
            DelimitersInside = itemValue.Count(ch => ch == delimiter)
        });
    }
}
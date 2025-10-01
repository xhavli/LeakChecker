using System.Diagnostics;
using System.Globalization;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Text;
using LeakChecker.ContentDetection.ItemParsing;
using LeakChecker.ContentDetection.ItemRecognition;
using LeakChecker.ContentDetection.RecognitionService;
using LeakChecker.ContentProcessing;
using LeakChecker.FormatDetection;
using LeakChecker.Logging;
using LeakChecker.Logging.FileLogging;
using LeakChecker.Utilities.Extensions;

namespace LeakChecker.ContentDetection;

public class ContentDetector(FileLogger logger, FileStats fileStats)
{
    public async Task ProcessFile(Encoding encoding, char delimiter)
    {
        await logger.LogContentProcessingStart();
        Stopwatch sw = Stopwatch.StartNew();

        await using var fileStream = new FileStream(fileStats.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(fileStream, encoding, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        long readerPosition = 0;
        
        long recordsCount = 0;
        var analyzer = new HeuristicAnalyzer();

        while (await reader.ReadLineWithEndingAsync() is { } line)
        {
            int bytesRead = encoding.GetByteCount(line);
            line = line.ReplaceLineEndings();
            List<HeuristicRecord> linePatterns = new();
            string originalLine = line;
            
            // if (linesCount == 100) break;
            if (recordsCount == 10000) break;
            
            recordsCount++;
            Console.WriteLine();
            Console.WriteLine($"'{logger.SubjectFilePath}' [{recordsCount}] [{sw.Elapsed:g}] ===> {line}");

            line = line.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            
            if (line.StartsWith("INSERT INTO", StringComparison.OrdinalIgnoreCase))
            {
                long sqlInsertStart = readerPosition;
                
                // Reset to before INSERT (checkpoint)
                await AdjustReader(reader, sqlInsertStart);

                // Detect schema & get end position
                Stopwatch sqlInsertSw = Stopwatch.StartNew();
                var sqlInsertSchema = await SqlInsertDetector.DetectFormat(reader, logger);
                
                Console.WriteLine();
                Console.WriteLine($"-- SCHEMA CREATED IN {sqlInsertSw.Elapsed} --");
                Console.WriteLine();
                
                // Process INSERT block fully (this will advance reader internally)
                SqlInsertProcessor processor = new(sqlInsertSchema);
                await AdjustReader(reader, sqlInsertStart);
                var (recordsProcessed, sqlBytesRead) = await processor.ProcessInsert(reader, logger);
                
                recordsCount += recordsProcessed;
                // Reader is now at end of INSERT block -> realign
                readerPosition += sqlBytesRead;
                await AdjustReader(reader, readerPosition);
                fileStats.Formats.Add(FormatEnum.SqlInsert); //TODO this throwing an exception

                continue;
            }

            // Update checkpoint AFTER processing a normal line
            readerPosition += bytesRead;
            
            line = line.TrimOuterParenthesesAndComma();

            if (TimeStampRecognizer.TryRecognize(line, out List<string> stringTimeStamps, out List<DateTime> timeStamps))
            {
                if (stringTimeStamps.Count == timeStamps.Count)
                {
                    foreach (var timeStamp in stringTimeStamps.OrderByDescending(ts=>ts.Length))
                    {
                        Console.WriteLine($"[{CountDelimitersBefore(originalLine,timeStamp, delimiter)}] {ItemEnum.TimeStamp} = {timeStamp}");

                        linePatterns.Add(new HeuristicRecord
                        {
                            Attribute = ItemEnum.TimeStamp,
                            TokenStart = CountDelimitersBefore(originalLine, timeStamp, delimiter),
                            DelimiterCountInside = timeStamp.Count(ch => ch == delimiter)
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
                        Console.WriteLine($"[{CountDelimitersBefore(originalLine,email, delimiter)}]{ItemEnum.Email} = {email}");
                        
                        linePatterns.Add(new HeuristicRecord
                        {
                            Attribute = ItemEnum.Email,
                            TokenStart = CountDelimitersBefore(originalLine, email, delimiter),
                            DelimiterCountInside = email.Count(ch => ch == delimiter)
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
                        Console.WriteLine($"[{CountDelimitersBefore(originalLine,uri, delimiter)}] {ItemEnum.Web} = {uri}");
                        
                        linePatterns.Add(new HeuristicRecord
                        {
                            Attribute = ItemEnum.Web,
                            TokenStart = CountDelimitersBefore(originalLine, uri, delimiter),
                            DelimiterCountInside = uri.Count(ch => ch == delimiter)
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
                    if (entity.Type.Equals("PERSON"))
                    {
                        linePatterns.Add(new HeuristicRecord
                        {
                            Attribute = ItemEnum.Name,
                            TokenStart = CountDelimitersBefore(originalLine, item, delimiter),
                            DelimiterCountInside = item.Count(ch => ch == delimiter)
                        });
                    }
                    else if (entity.Type.Equals("LOCATION"))
                    {
                        linePatterns.Add(new HeuristicRecord
                        {
                            Attribute = ItemEnum.Location,
                            TokenStart = CountDelimitersBefore(originalLine, item, delimiter),
                            DelimiterCountInside = item.Count(ch => ch == delimiter)
                        });
                    }
                    else if (entity.Type.Equals("ORGANIZATION"))
                    {
                        linePatterns.Add(new HeuristicRecord
                        {
                            Attribute = ItemEnum.Organization,
                            TokenStart = CountDelimitersBefore(originalLine, item, delimiter),
                            DelimiterCountInside = item.Count(ch => ch == delimiter)
                        });
                    }
                    
                    Console.WriteLine($"[{CountDelimitersBefore(originalLine,item, delimiter)}] {entity.Type} = {item}");
                }
                
                foreach (var entity in analyzeResults.OrderByDescending(e => e.Start))
                {
                    line = line.Remove(entity.Start, entity.End - entity.Start);
                }
            }
            catch (Exception e)
            {
                await logger.Log($"Communication with PythonNerService failed. {e.Message}", LogLevel.Exception, LogContext.Content);
            }
            
            if (GuidRecognizer.TryRecognize(line, out List<string> stringGuids, out List<Guid> guids))
            {
                if (stringGuids.Count == guids.Count)
                {
                    foreach (var guid in stringGuids)
                    {
                        Console.WriteLine($"[{CountDelimitersBefore(originalLine,guid, delimiter)}] {ItemEnum.Id} = {guid}");
                     
                        linePatterns.Add(new HeuristicRecord
                        {
                            Attribute = ItemEnum.Web,
                            TokenStart = CountDelimitersBefore(originalLine, guid, delimiter),
                            DelimiterCountInside = guid.Count(ch => ch == delimiter)
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
                    if (linePattern.TokenStart == i) skip = true; 
                }
                if (skip) continue;
                
                string token = tokens[i].Trim().TrimOuterQuotes();

                if (string.IsNullOrEmpty(token)) continue;

                if (GenderParser.TryParse(token, out string gender))
                {
                    Console.WriteLine($"[{i}] {ItemEnum.Gender} = {gender}");
                    
                    linePatterns.Add(new HeuristicRecord
                    {
                        Attribute = ItemEnum.Gender,
                        TokenStart = CountDelimitersBefore(originalLine, token, delimiter),
                        DelimiterCountInside = token.Count(ch => ch == delimiter)
                    });
                    continue;
                }

                if (MaritalStatusParser.TryParse(token, out string maritalStatus))
                {
                    Console.WriteLine($"[{i}] {ItemEnum.MaritalStatus} = {maritalStatus}");
                    
                    linePatterns.Add(new HeuristicRecord
                    {
                        Attribute = ItemEnum.MaritalStatus,
                        TokenStart = CountDelimitersBefore(originalLine, token, delimiter),
                        DelimiterCountInside = token.Count(ch => ch == delimiter)
                    });
                    continue;
                }
                
                if (IpAddressParser.TryParse(token, out ItemEnum ipAddressType, out _))
                {
                    Console.WriteLine($"[{i}] {token} = {ipAddressType}");
                        
                    linePatterns.Add(new HeuristicRecord
                    {
                        Attribute = ipAddressType,
                        TokenStart = CountDelimitersBefore(originalLine, token, delimiter),
                        DelimiterCountInside = token.Count(ch => ch == delimiter)
                    });
                        
                    continue;
                }
                
                if (PhoneNumberParser.TryParse(token, out string phoneNumber))
                {
                    Console.WriteLine($"[{i}] {ItemEnum.PhoneNumber} = {phoneNumber}");
                            
                    linePatterns.Add(new HeuristicRecord
                    {
                        Attribute = ItemEnum.PhoneNumber,
                        TokenStart = CountDelimitersBefore(originalLine, token, delimiter),
                        DelimiterCountInside = token.Count(ch => ch == delimiter)
                    });
                    continue;
                }

                if (PhysicalAddress.TryParse(token, out PhysicalAddress? mac) && mac.GetAddressBytes().Length == 6)
                {
                    Console.WriteLine($"[{i}] {ItemEnum.Mac} = {token}");
                            
                    linePatterns.Add(new HeuristicRecord
                    {
                        Attribute = ItemEnum.Mac,
                        TokenStart = CountDelimitersBefore(originalLine, token, delimiter),
                        DelimiterCountInside = token.Count(ch => ch == delimiter)
                    });
                    continue;
                }
                
                try
                {
                    var (isHash, withSalt, algorithm) = await HashParser.TryParse(token);
                    if (isHash && !withSalt)
                    {
                        Console.WriteLine($"[{i}] {ItemEnum.Hash} = algorithm: {algorithm}, token: {token}");
                        
                        linePatterns.Add(new HeuristicRecord
                        {
                            Attribute = ItemEnum.Hash,
                            TokenStart = CountDelimitersBefore(originalLine, token, delimiter),
                            DelimiterCountInside = token.Count(ch => ch == delimiter)
                        });
                        continue;
                    }
                    if (isHash && withSalt)
                    {
                        Console.WriteLine($"[{i}] {ItemEnum.SaltedHash} = algorithm: {algorithm}, token: {token}");
                        
                        linePatterns.Add(new HeuristicRecord
                        {
                            Attribute = ItemEnum.SaltedHash,
                            TokenStart = CountDelimitersBefore(originalLine, token, delimiter),
                            DelimiterCountInside = token.Count(ch => ch == delimiter)
                        });
                        continue;
                    }
                }
                catch (Exception e)
                {
                    await logger.Log($"Communication with www.hashes.com failed. {e.Message}", LogLevel.Exception, LogContext.Content);
                }
                
                if (TimeStampParser.TryParse(token, out DateTime timeStamp))
                {
                    Console.WriteLine($"[{i}] {ItemEnum.TimeStamp} = {timeStamp}");
                            
                    linePatterns.Add(new HeuristicRecord
                    {
                        Attribute = ItemEnum.TimeStamp,
                        TokenStart = CountDelimitersBefore(originalLine, token, delimiter),
                        DelimiterCountInside = token.Count(ch => ch == delimiter)
                    });
                    continue;
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{i}] [UNRECOGNIZED TOKEN]: {token}");
                Console.ResetColor();

                linePatterns.Add(new HeuristicRecord
                {
                    Attribute = ItemEnum.Other,
                    TokenStart = CountDelimitersBefore(originalLine, token, delimiter),
                    DelimiterCountInside = token.Count(ch => ch == delimiter)
                });
            }
            
            analyzer.AddLinePatterns(linePatterns);
        }
        
        // Console.WriteLine();
        // analyzer.PrintHeuristicData();
        // analyzer.PrintDominantSchema();
        // var schemaX = analyzer.GetDominantSchema(50);
        // // var schemaY = analyzer.GetSchemaWithSpans();
        // // ContentProcessing.ContentProcessor contentProcessor = new(delimiter, schemaY);
        // var ts = sw.Elapsed;
        // Console.WriteLine();
        // Console.WriteLine($"Schema created {ts}");
        // Console.WriteLine();    

        // while (await reader.ReadLineAsync() is { } line)
        // {
        //     linesCount++;
        //     if (linesCount >= 200) break;
        //     contentProcessor.ProcessLine(line);
        // }
        
        // Console.WriteLine();
        // Console.WriteLine($"Another 100 lines processed {sw.Elapsed - ts}");
        // Console.WriteLine();

        fileStats.RecordsCount = recordsCount;
        Console.WriteLine();
        await logger.Log($"Content processing finished successfully. Time taken: {sw.Elapsed}, current DateTime: " +
                       $"{DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}", LogLevel.Success, LogContext.Content);
        await logger.Log($"Lines processed count: {recordsCount}");
        //await logger.LogContentStats(diversityDictionary);
    }

    public static async Task<ItemEnum> DetectToken(string token, FileLogger logger)
    {
        //TODO how to handle empty im heuristic analysis
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

        //TODO bypas for faster detection in development hashes.com responds take a while
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

    private async Task<ItemEnum> ParseToken(string token)
    {
        if (IpAddressParser.TryParse(token, out ItemEnum itemType, out _)) { return itemType; }

        if (GenderParser.TryParse(token, out _)) { return ItemEnum.Gender; }

        if (MaritalStatusParser.TryParse(token, out _)) { return ItemEnum.MaritalStatus;}
        
        if (PhoneNumberParser.TryParse(token, out _)) { return ItemEnum.PhoneNumber; }

        if (PhysicalAddress.TryParse(token, out PhysicalAddress? mac) && mac.GetAddressBytes().Length == 6)
        { return ItemEnum.Mac; }

        //TODO tmp while www.hashes.com is down
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
        
        //TODO
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
    
    private async Task AdjustReader(StreamReader reader, long offset)
    {
        if (offset < 0)
        {
            await logger.Log("Offset out of range in AdjustReader. Provided value is negative. Offset set to 0.", LogLevel.Warning, LogContext.Processing);
            offset = 0;
        }

        if (offset > reader.BaseStream.Length)
        {
            await logger.Log("Offset out of range in AdjustReader. Provided value is greater than stream.Lenght. Offset set to stream.Length", LogLevel.Warning, LogContext.Processing);
            offset = reader.BaseStream.Length;
        }
        
        reader.BaseStream.Seek(offset, SeekOrigin.Begin);
        reader.DiscardBufferedData();
    }
}
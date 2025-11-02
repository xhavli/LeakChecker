using System.Diagnostics;
using System.Globalization;
using System.Text;
using LeakChecker.Format;
using LeakChecker.Format.Detection;
using LeakChecker.Logging;
using LeakChecker.Logging.FileLogging;
using LeakChecker.Utilities.Extensions;

namespace LeakChecker.Content.Processing;

public class ContentProcessor
{
    private long _recordsCount;
    private long _readerPosition;
    private readonly Stopwatch _sw = new();
    private readonly char _delimiter;
    private readonly Encoding _encoding;
    private readonly StreamReader _reader;
    private readonly IFileLogger _logger;
    private readonly FileStats _fileStats;

    private ContentProcessor(char delimiter, Encoding encoding, StreamReader reader, IFileLogger logger, FileStats fileStats)
    {
        _delimiter = delimiter;
        _encoding = encoding;
        _reader = reader;
        _logger = logger;
        _fileStats = fileStats;
    }

    public static async Task<ContentProcessor> CreateAsync(
        char delimiter, IFileLogger logger, FileStats stats, Encoding? encoding = null)
    {
        if (encoding == null)
        {
            await logger.Log("No encoding specified to ContentDetector. Set UTF-8 without BOM as default.", 
                LogLevel.Warning, LogContext.Content);
            encoding ??= new UTF8Encoding(false);   // false = BOM
        }
        
        string filePath = logger.SubjectFilePath;
        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        
        return new ContentProcessor(delimiter, encoding, reader, logger, stats);
    }
    
    public async Task ProcessFile()
    {
        _sw.Start();
        await _logger.LogContentHeader();

        while (await _reader.ReadLineWithEndingAsync() is { } line)
        {
            if (_recordsCount >= 150) break;
            
            int bytesRead = _encoding.GetByteCount(line);
            line = line.ReplaceLineEndings("").Trim();
            
            // Console.WriteLine($"'{filePath}' [{recordsCount}] [{_sw.Elapsed:g}] ===> {line}");

            line = line.Trim();
            if (string.IsNullOrWhiteSpace(line) || string.IsNullOrEmpty(line)) continue;
            
            if (line.StartsWith("INSERT INTO", StringComparison.OrdinalIgnoreCase))
            {
                await ProcessSqlInsert();
                continue;
            }
            
            // if looks like a JSON    //TODO
            if (line.StartsWith('{') || line.StartsWith('[')) {}    // test if it really is a json and parse it
            // if looks like a HTML    //TODO
            if (line.Contains("<html", StringComparison.OrdinalIgnoreCase) || 
                line.Contains("<body", StringComparison.OrdinalIgnoreCase)) {}  // // test if it really is a html and parse it

            await ProcessCsvFile();
            
            if (_recordsCount >= 150) break;
            
            // line = line.TrimOuterParenthesesAndComma();  //TODO

            // if (TimeStampRecognizer.TryRecognize(line, out List<string> stringTimeStamps, out List<DateTime> timeStamps))
            // {
            //     if (stringTimeStamps.Count == timeStamps.Count)
            //     {
            //         foreach (var timeStamp in stringTimeStamps.OrderByDescending(ts=>ts.Length))
            //         {
            //             Console.WriteLine($"[{CountDelimitersBefore(originalLine,timeStamp, delimiter)}] {ItemEnum.TimeStamp} = {timeStamp}");
            //
            //             linePatterns.Add(new ContentHeuristicRecord
            //             {
            //                 Attribute = ItemEnum.TimeStamp,
            //                 TokenStart = CountDelimitersBefore(originalLine, timeStamp, delimiter),
            //                 DelimitersInside = timeStamp.Count(ch => ch == delimiter)
            //             });
            //             line = line.Replace(timeStamp, "", StringComparison.InvariantCultureIgnoreCase);
            //         }
            //     }
            //     else
            //     {
            //         await logger.Log("stringTimeStamps.Count != timeStamps.Count", LogLevel.Warning, LogContext.Content);
            //     }
            // }
            //
            // if (EmailRecognizer.TryRecognize(line, out List<string> stringEmails, out List<MailAddress> emails))
            // {
            //     if (stringEmails.Count == emails.Count)
            //     {
            //         foreach (var email in stringEmails)
            //         {
            //             Console.WriteLine($"[{CountDelimitersBefore(originalLine,email, delimiter)}]{ItemEnum.Email} = {email}");
            //             
            //             linePatterns.Add(new ContentHeuristicRecord
            //             {
            //                 Attribute = ItemEnum.Email,
            //                 TokenStart = CountDelimitersBefore(originalLine, email, delimiter),
            //                 DelimitersInside = email.Count(ch => ch == delimiter)
            //             });
            //             line = line.Replace(email, "", StringComparison.InvariantCultureIgnoreCase);
            //         }
            //     }
            //     else
            //     {
            //         await logger.Log("stringEmails.Count != emails.Count", LogLevel.Warning, LogContext.Content);
            //     }
            // }
            //
            // if (WebRecognizer.TryRecognize(line, out List<string> stringUris, out List<Uri> uris))
            // {
            //     if (stringUris.Count == uris.Count)
            //     {
            //         foreach (var uri in stringUris.OrderByDescending(s => s.Length))
            //         {
            //             Console.WriteLine($"[{CountDelimitersBefore(originalLine,uri, delimiter)}] {ItemEnum.Web} = {uri}");
            //             
            //             linePatterns.Add(new ContentHeuristicRecord
            //             {
            //                 Attribute = ItemEnum.Web,
            //                 TokenStart = CountDelimitersBefore(originalLine, uri, delimiter),
            //                 DelimitersInside = uri.Count(ch => ch == delimiter)
            //             });
            //             line = line.Replace(uri, "");
            //         }
            //     }
            //     else
            //     {
            //         await logger.Log("stringUris.Count != uris.Count", LogLevel.Warning, LogContext.Content);
            //     }
            // }
            //
            // try
            // {
            //     List<PresidioEntity> analyzeResults = await PythonNerServiceRecognizer.TryRecognize(line);
            //     foreach (var entity in analyzeResults.OrderBy(e => e.Start))
            //     {
            //         string item = line.Substring(entity.Start, entity.End - entity.Start);
            //         if (entity.Type.Equals("PERSON"))
            //         {
            //             linePatterns.Add(new ContentHeuristicRecord
            //             {
            //                 Attribute = ItemEnum.Name,
            //                 TokenStart = CountDelimitersBefore(originalLine, item, delimiter),
            //                 DelimitersInside = item.Count(ch => ch == delimiter)
            //             });
            //         }
            //         else if (entity.Type.Equals("LOCATION"))
            //         {
            //             linePatterns.Add(new ContentHeuristicRecord
            //             {
            //                 Attribute = ItemEnum.Location,
            //                 TokenStart = CountDelimitersBefore(originalLine, item, delimiter),
            //                 DelimitersInside = item.Count(ch => ch == delimiter)
            //             });
            //         }
            //         else if (entity.Type.Equals("ORGANIZATION"))
            //         {
            //             linePatterns.Add(new ContentHeuristicRecord
            //             {
            //                 Attribute = ItemEnum.Organization,
            //                 TokenStart = CountDelimitersBefore(originalLine, item, delimiter),
            //                 DelimitersInside = item.Count(ch => ch == delimiter)
            //             });
            //         }
            //         
            //         Console.WriteLine($"[{CountDelimitersBefore(originalLine,item, delimiter)}] {entity.Type} = {item}");
            //     }
            //     
            //     foreach (var entity in analyzeResults.OrderByDescending(e => e.Start))
            //     {
            //         line = line.Remove(entity.Start, entity.End - entity.Start);
            //     }
            // }
            // catch (Exception e)
            // {
            //     await logger.Log($"Communication with PythonNerService failed. {e.Message}", LogLevel.Exception, LogContext.Content);
            // }
            //
            // if (GuidRecognizer.TryRecognize(line, out List<string> stringGuids, out List<Guid> guids))
            // {
            //     if (stringGuids.Count == guids.Count)
            //     {
            //         foreach (var guid in stringGuids)
            //         {
            //             Console.WriteLine($"[{CountDelimitersBefore(originalLine,guid, delimiter)}] {ItemEnum.Id} = {guid}");
            //          
            //             linePatterns.Add(new ContentHeuristicRecord
            //             {
            //                 Attribute = ItemEnum.Web,
            //                 TokenStart = CountDelimitersBefore(originalLine, guid, delimiter),
            //                 DelimitersInside = guid.Count(ch => ch == delimiter)
            //             });
            //             line = line.Replace(guid, "");
            //         }
            //     }
            //     else
            //     {
            //         await logger.Log("stringGuids.Count != guids.Count", LogLevel.Warning, LogContext.Content);
            //     }
            // }

            // string[] tokens = line.Split(delimiter);
            // for (int i = 0; i < tokens.Length; i++)
            // {
            //     bool skip = false;
            //     foreach (var linePattern in linePatterns)
            //     {
            //         if (linePattern.TokenStart == i) skip = true; 
            //     }
            //     if (skip) continue;
            //
            
            // string token = tokens[i].Trim().TrimOuterQuotes();  //TODO
            
            //
            //     if (string.IsNullOrEmpty(token)) continue;
            //
            //     if (GenderParser.TryParse(token, out string gender))
            //     {
            //         Console.WriteLine($"[{i}] {ItemEnum.Gender} = {gender}");
            //         
            //         linePatterns.Add(new ContentHeuristicRecord
            //         {
            //             Attribute = ItemEnum.Gender,
            //             TokenStart = CountDelimitersBefore(originalLine, token, delimiter),
            //             DelimitersInside = token.Count(ch => ch == delimiter)
            //         });
            //         continue;
            //     }
            //
            //     if (MaritalStatusParser.TryParse(token, out string maritalStatus))
            //     {
            //         Console.WriteLine($"[{i}] {ItemEnum.MaritalStatus} = {maritalStatus}");
            //         
            //         linePatterns.Add(new ContentHeuristicRecord
            //         {
            //             Attribute = ItemEnum.MaritalStatus,
            //             TokenStart = CountDelimitersBefore(originalLine, token, delimiter),
            //             DelimitersInside = token.Count(ch => ch == delimiter)
            //         });
            //         continue;
            //     }
            //     
            //     if (IpAddressParser.TryParse(token, out ItemEnum ipAddressType, out _))
            //     {
            //         Console.WriteLine($"[{i}] {token} = {ipAddressType}");
            //             
            //         linePatterns.Add(new ContentHeuristicRecord
            //         {
            //             Attribute = ipAddressType,
            //             TokenStart = CountDelimitersBefore(originalLine, token, delimiter),
            //             DelimitersInside = token.Count(ch => ch == delimiter)
            //         });
            //             
            //         continue;
            //     }
            //     
            //     if (PhoneNumberParser.TryParse(token, out string phoneNumber))
            //     {
            //         Console.WriteLine($"[{i}] {ItemEnum.PhoneNumber} = {phoneNumber}");
            //                 
            //         linePatterns.Add(new ContentHeuristicRecord
            //         {
            //             Attribute = ItemEnum.PhoneNumber,
            //             TokenStart = CountDelimitersBefore(originalLine, token, delimiter),
            //             DelimitersInside = token.Count(ch => ch == delimiter)
            //         });
            //         continue;
            //     }
            //
            //     if (PhysicalAddress.TryParse(token, out PhysicalAddress? mac) && mac.GetAddressBytes().Length == 6)
            //     {
            //         Console.WriteLine($"[{i}] {ItemEnum.Mac} = {token}");
            //                 
            //         linePatterns.Add(new ContentHeuristicRecord
            //         {
            //             Attribute = ItemEnum.Mac,
            //             TokenStart = CountDelimitersBefore(originalLine, token, delimiter),
            //             DelimitersInside = token.Count(ch => ch == delimiter)
            //         });
            //         continue;
            //     }
            //     
            //     try
            //     {
            //         var (isHash, withSalt, algorithm) = await HashParser.TryParse(token);
            //         if (isHash && !withSalt)
            //         {
            //             Console.WriteLine($"[{i}] {ItemEnum.Hash} = algorithm: {algorithm}, token: {token}");
            //             
            //             linePatterns.Add(new ContentHeuristicRecord
            //             {
            //                 Attribute = ItemEnum.Hash,
            //                 TokenStart = CountDelimitersBefore(originalLine, token, delimiter),
            //                 DelimitersInside = token.Count(ch => ch == delimiter)
            //             });
            //             continue;
            //         }
            //         if (isHash && withSalt)
            //         {
            //             Console.WriteLine($"[{i}] {ItemEnum.SaltedHash} = algorithm: {algorithm}, token: {token}");
            //             
            //             linePatterns.Add(new ContentHeuristicRecord
            //             {
            //                 Attribute = ItemEnum.SaltedHash,
            //                 TokenStart = CountDelimitersBefore(originalLine, token, delimiter),
            //                 DelimitersInside = token.Count(ch => ch == delimiter)
            //             });
            //             continue;
            //         }
            //     }
            //     catch (Exception e)
            //     {
            //         await logger.Log($"Communication with www.hashes.com failed. {e.Message}", LogLevel.Exception, LogContext.Content);
            //     }
            //     
            //     if (TimeStampParser.TryParse(token, out DateTime timeStamp))
            //     {
            //         Console.WriteLine($"[{i}] {ItemEnum.TimeStamp} = {timeStamp}");
            //                 
            //         linePatterns.Add(new ContentHeuristicRecord
            //         {
            //             Attribute = ItemEnum.TimeStamp,
            //             TokenStart = CountDelimitersBefore(originalLine, token, delimiter),
            //             DelimitersInside = token.Count(ch => ch == delimiter)
            //         });
            //         continue;
            //     }
            //
            //     Console.ForegroundColor = ConsoleColor.Red;
            //     Console.WriteLine($"[{i}] [UNRECOGNIZED TOKEN]: {token}");
            //     Console.ResetColor();
            //
            //     linePatterns.Add(new ContentHeuristicRecord
            //     {
            //         Attribute = ItemEnum.Other,
            //         TokenStart = CountDelimitersBefore(originalLine, token, delimiter),
            //         DelimitersInside = token.Count(ch => ch == delimiter)
            //     });
            // }
            
            // analyzer.AddLinePatterns(linePatterns);
        }

        _fileStats.RecordsCount = _recordsCount;
        Console.WriteLine();
        await _logger.Log($"Content processing finished successfully. Time taken: {_sw.Elapsed}, current DateTime: " +
                       $"{DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}", LogLevel.Success, LogContext.Content);
        await _logger.Log($"Lines processed count: {_recordsCount}");
    }

    private async Task AdjustReader(long offset)
    {
        //TODO remove validations and make it as a reader extension
        if (offset < 0)
        {
            await _logger.Log("Offset out of range in AdjustReader. Provided value is negative. Offset set to 0.", LogLevel.Warning, LogContext.Processing);
            offset = 0;
        }

        if (offset > _reader.BaseStream.Length)
        {
            await _logger.Log("Offset out of range in AdjustReader. Provided value is greater than stream.Lenght. Offset set to stream.Length", LogLevel.Warning, LogContext.Processing);
            offset = _reader.BaseStream.Length;
        }
        
        _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
        _reader.DiscardBufferedData();
    }

    private async Task ProcessSqlInsert()
    {
        long sqlInsertStart = _readerPosition;
        
        // Reset to before INSERT INTO
        await AdjustReader(sqlInsertStart);
        
        // Detect schema & get end position
        Stopwatch sw = Stopwatch.StartNew();
        var schema = await SqlInsertDetector.DetectFormat(_reader, _logger);
        Console.WriteLine();
        await _logger.Log($"SQL INSERT schema created in {sw.Elapsed}");
        Console.WriteLine();
        
        // Process INSERT block fully (this will advance reader internally)
        SqlInsertProcessor processor = new(schema, _reader, _logger);
        await AdjustReader(sqlInsertStart);
        var result = await processor.ProcessSqlInsert();
        
        // Reader is now at end of INSERT block -> realign
        _readerPosition += result.bytesRead;
        _recordsCount += result.recordProcessed;
        await AdjustReader(_readerPosition);
        
        _fileStats.Formats.Add(FormatEnum.SqlInsert);
    }

    private async Task ProcessCsvFile()
    {
        long csvFormatStarted = _readerPosition;
            
        await AdjustReader(csvFormatStarted);
        
        // Detect schema & get end position
        Stopwatch csvFormatSw = Stopwatch.StartNew();
        var csvSchema = await CsvFileDetector.DetectFormat(_delimiter, _reader, _logger);
        Console.WriteLine();
        await _logger.Log($"-- CSV file schema created in {csvFormatSw.Elapsed} --");
        Console.WriteLine();
            
        await AdjustReader(csvFormatStarted);
        CsvFileProcessor csvFileProcessor = new(csvSchema);
        var csvResult = await csvFileProcessor.ProcessCsvFile(_reader, _logger);
        
        // Reader is now at end of INSERT block -> realign
        _readerPosition += csvResult.bytesRead;
        _recordsCount += csvResult.recordProcessed;
        await AdjustReader(_readerPosition);
        
        _fileStats.Formats.Add(FormatEnum.Csv);
    }
}
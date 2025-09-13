using System.Diagnostics;
using System.Globalization;
using System.Text;
using LeakChecker.Logging;
using LeakChecker.Logging.FileLogging;
using LeakChecker.Utilities;
using UtfUnknown;

namespace LeakChecker.EncodingDetection;

public static class EncodingDetector
{
    // Read & process each segment with correct encoding, improve and use
    public static async Task ReadAndProcessWithDynamicEncodings(string filePath, List<EncodingSegment> segments)
    {
        await using var stream = File.OpenRead(filePath);

        Console.WriteLine(filePath);
        Console.WriteLine("seg count: " + segments.Count);
        foreach (var segment in segments)
        {
            Console.WriteLine("enc name:" + segment.EncodingName);
            Console.WriteLine("seg start: " + segment.StartOffset);
            Console.WriteLine("seg len: " + segment.Length);
            Console.WriteLine("seg end: " + (segment.StartOffset + segment.Length));
        }

        foreach (var segment in segments)
        {
            var encoding = Encoding.GetEncoding(segment.EncodingName);

            stream.Seek(segment.StartOffset, SeekOrigin.Begin);
            byte[] buffer = new byte[segment.Length];
            await stream.ReadAsync(buffer, 0, buffer.Length);

            string decoded = encoding.GetString(buffer);
            using var reader = new StringReader(decoded);

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                // 🧪 Replace with your own processing
                Console.WriteLine($"[{segment.EncodingName}] {line}");
                return;
            }
        }
    }
    
    private static async Task<List<EncodingSegment>> MergeSequenceOfSameEncodingSegments(FileLogger logger, List<EncodingSegment> segments, Stopwatch sw)
    {
        if (segments.Count == 0) return segments;
        var merged = new List<EncodingSegment>();

        var current = segments[0];

        for (int i = 1; i < segments.Count; i++)
        {
            var next = segments[i];

            if (next.EncodingName == current.EncodingName)
            {
                // Extend current
                current.Length += next.Length;
            }
            else
            {
                merged.Add(current);
                current = next;
            }
        }
        
        merged.Add(current); // Add last segment
        
        await logger.Log($"Encoding processing finished successfully. Time taken: {sw.Elapsed}, current DateTime: " +
                       $"{DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}", LogLevel.Success, LogContext.Encoding);
        await logger.LogEncodingStats(merged);
        await logger.LogEncodingDetails(merged);
        
        return merged;
    }
    
    private static async Task<List<EncodingSegment>> RecursiveDetectEncodingBoundaries(string filePath, long startOffset, long length, int minBlockSize = 1)
    {
        var segments = new List<EncodingSegment>();

        async Task ProcessRange(FileStream stream, long start, long end)
        {
            if (end <= start || (end - start) < minBlockSize)
                return;

            long size = end - start;
            byte[] buffer = new byte[size];

            stream.Seek(start, SeekOrigin.Begin);
            int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, (int)size));
            if (bytesRead <= 0)
                return;

            var result = CharsetDetector.DetectFromBytes(buffer, 0, bytesRead);
            string encoding = result.Detected?.Encoding?.WebName ?? "[unknown]";
            float confidence = result.Detected?.Confidence ?? 0f;

            if (confidence >= 0.99f || size == minBlockSize)
            {
                segments.Add(new EncodingSegment
                {
                    StartOffset = start,
                    Length = bytesRead,
                    EncodingName = encoding,
                    Confidence = confidence
                });
            }
            else
            {
                long mid = start + size / 2;

                // Recurse into two halves
                await ProcessRange(stream, start, mid);
                await ProcessRange(stream, mid, end);
            }
        }

        await using var stream = File.OpenRead(filePath);
        await ProcessRange(stream, startOffset, startOffset + length);

        return segments;
    }

    private static async Task<List<EncodingSegment>> ProcessFileAsConcatenatedEncoding(FileLogger logger, int sampleSize = 4 * 1024 )
    {
        int bytesRead;
        long offset = 0;
        var buffer = new byte[sampleSize];
        var segments = new List<EncodingSegment>();

        await using var stream = File.OpenRead(logger.FilePath);

        while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
        {
            var result = CharsetDetector.DetectFromBytes(buffer, 0, bytesRead);
            float confidence = result.Detected?.Confidence ?? 0f;
            string encodingName = result.Detected?.Encoding?.WebName ?? "[unknown]";

            if (confidence >= 0.99f)
            {
                segments.Add(new EncodingSegment
                {
                    StartOffset = offset,
                    Length = bytesRead,
                    EncodingName = encodingName,
                    Confidence = confidence
                });
            }
            else
            {
                // Precise fallback detection
                var preciseSegments = await RecursiveDetectEncodingBoundaries(logger.FilePath, offset, bytesRead);
                segments.AddRange(preciseSegments);
            }

            offset += bytesRead;
        }

        return segments;
    }
    
    private static async Task<List<EncodingSegment>> ProcessFileAsConsistentEncoding(FileLogger logger)
    {
        var segments = new List<EncodingSegment>();
        await using var stream = File.OpenRead(logger.FilePath);

        // Ensure detector will read all logger
        var result = CharsetDetector.DetectFromStream(stream, stream.Length);
        if (result is { Detected.Confidence: > 0.99f })
        {
            segments.Add(new EncodingSegment
            {
                StartOffset = 0,
                Length = stream.Length,
                EncodingName = result.Detected.EncodingName,
                Confidence = result.Detected.Confidence
            });
            
            return segments;
        }

        if (result == null)
            await logger.Log("Consistent encoding detection failed.", LogLevel.Warning, LogContext.Encoding);
        
        if (result != null)
            await logger.Log($"Consistent encoding detection not satisfied [{result.Detected?.EncodingName}] " +
                           $"with low confidence [{result.Detected?.Confidence:F2}]", LogLevel.Warning, LogContext.Encoding);
        
        return segments;
    }

    public static async Task<List<EncodingSegment>> DetectEncodingFromFile(FileLogger logger)
    {
        Stopwatch sw = Stopwatch.StartNew();
        await logger.LogEncodingProcessingStart();

        List<EncodingSegment> result = await ProcessFileAsConsistentEncoding(logger);
        if (result.Any()) return await MergeSequenceOfSameEncodingSegments(logger, result, sw);

        result = await ProcessFileAsConcatenatedEncoding(logger);
        return await MergeSequenceOfSameEncodingSegments(logger, result, sw);
    }
    
    
    //TODO tmp bypass to avoid encoding detection with segments
    public static async Task<Encoding> DetectEncodingFromStream(FileLogger logger)
    {
        Stopwatch sw = Stopwatch.StartNew();
        await logger.LogEncodingProcessingStart();
        
        await using var stream = File.OpenRead(logger.FilePath);
        var result = CharsetDetector.DetectFromStream(stream);
        {
            try
            {
                string encoding = result.Detected?.Encoding?.WebName ?? string.Empty;
                float confidence = result.Detected?.Confidence ?? 0f;
                if (!string.IsNullOrEmpty(encoding) || confidence > 0f)
                {
                    await logger.Log($"Encoding detected [{encoding}] with confidence [{confidence}]", LogLevel.Success, LogContext.Encoding);
                    await logger.Log($"Encoding processing finished successfully. Time taken: {sw.Elapsed}, current DateTime: " +
                                   $"{DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}");
                }
                return Encoding.GetEncoding(encoding);
            }
            catch (Exception e)
            {
                
                await logger.Log($"Encoding detection failed. {e.Message} Fallback to [{Encoding.UTF8.WebName}]", LogLevel.Exception);
                await logger.Log($"Encoding processing failed. Time taken: {sw.Elapsed}, current DateTime: " +
                               $"{DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}", LogLevel.Warning, LogContext.Encoding);
                return Encoding.UTF8;
            }
        }
    }
}
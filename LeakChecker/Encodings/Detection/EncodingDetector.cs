using System.Diagnostics;
using System.Globalization;
using System.Text;
using LeakChecker.Logging;
using LeakChecker.Logging.FileLogging;
using UtfUnknown;

namespace LeakChecker.Encodings.Detection;

public class EncodingDetector(FileLogger logger, FileStats stats)
{
    private readonly Stopwatch _stopWatch = new();

    private static List<EncodingSegment> MergeSameEncodingSegments(List<EncodingSegment> segments)
    {
        if (segments.Count == 0) return segments;
        var merged = new List<EncodingSegment>();

        var current = segments[0];

        for (int i = 1; i < segments.Count; i++)
        {
            var next = segments[i];

            if (Equals(next.Encoding, current.Encoding))
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
        
        return merged;
    }
    
    private static async Task<List<EncodingSegment>> DetectEncodingBoundaries(string filePath, long startOffset, long length, int minBlockSize = 1)
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
            float confidence = result?.Detected?.Confidence ?? 0f;
            Encoding? encoding = result?.Detected?.Encoding;

            if (confidence >= 0.99f || size == minBlockSize)
            {
                segments.Add(new EncodingSegment
                {
                    StartOffset = start,
                    Length = bytesRead,
                    Encoding = encoding,
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

    // sampleSize predefined to 4KB
    private async Task<List<EncodingSegment>> DetectConcatenatedEncoding(int sampleSize = 4 * 1024)
    {
        int bytesRead;
        long offset = 0;
        var buffer = new byte[sampleSize];
        var segments = new List<EncodingSegment>();

        await using var stream = File.OpenRead(logger.SubjectFilePath);

        while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
        {
            var result = CharsetDetector.DetectFromBytes(buffer, 0, bytesRead);
            float confidence = result?.Detected?.Confidence ?? 0f;
            Encoding? encoding = result?.Detected?.Encoding;

            if (confidence >= 0.99f)
            {
                segments.Add(new EncodingSegment
                {
                    StartOffset = offset,
                    Length = bytesRead,
                    Encoding = encoding,
                    Confidence = confidence
                });
            }
            else
            {
                // Precise fallback detection
                var preciseSegments = await DetectEncodingBoundaries(logger.SubjectFilePath, offset, bytesRead);
                segments.AddRange(preciseSegments);
            }

            offset += bytesRead;
        }

        return segments;
    }
    
    private async Task<List<EncodingSegment>> DetectConsistentEncoding()
    {
        var segments = new List<EncodingSegment>();
        await using var stream = File.OpenRead(logger.SubjectFilePath);

        // Ensure detector will read all logger
        var result = CharsetDetector.DetectFromStream(stream, stream.Length);
        
        stats.Encoding = result?.Detected?.Encoding;
        if (result is { Detected.Confidence: > 0.99f })
        {
            segments.Add(new EncodingSegment
            {
                StartOffset = 0,
                Length = stream.Length,
                Encoding = result.Detected.Encoding,
                Confidence = result.Detected.Confidence
            });
            
            return segments;
        }

        if (result == null)
            await logger.Log("Consistent encoding detection failed.", LogLevel.Warning, LogContext.Encoding);
        
        if (result != null)
            await logger.Log($"Consistent encoding detection with low confidence [{result.Detected?.Confidence:F2}]" +
                             $"detected [{result.Detected?.EncodingName}]", LogLevel.Warning, LogContext.Encoding);
        
        return segments;
    }

    public async Task<List<EncodingSegment>> DetectFileEncodings()
    {
        _stopWatch.Start();
        await logger.LogEncodingHeader();

        List<EncodingSegment> encSegments = await DetectConsistentEncoding();
        if (encSegments.Count != 1)
        {
            encSegments = await DetectConcatenatedEncoding();
        }
        
        List<EncodingSegment> result = MergeSameEncodingSegments(encSegments);
        
        await logger.Log($"Encoding detection finished successfully. Time taken: {_stopWatch.Elapsed}, current DateTime: " +
                         $"{DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}", LogLevel.Success, LogContext.Encoding);
        await logger.LogEncodingStats(result);
        await logger.LogEncodingDetails(result);
        
        return result;
    }
}
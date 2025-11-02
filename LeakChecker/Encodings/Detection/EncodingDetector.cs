using System.Diagnostics;
using System.Globalization;
using System.Text;
using LeakChecker.Logging;
using LeakChecker.Logging.FileLogging;
using UtfUnknown;

namespace LeakChecker.Encodings.Detection;

public class EncodingDetector(IFileLogger logger, FileStats stats)
{
    private readonly Stopwatch _stopWatch = new();

    private static List<EncodingSegment> MergeSameEncodingSegments(List<EncodingSegment> segments)
    {
        if (segments.Count <= 1) return segments;
        segments.Sort(static (a, b) => a.StartOffset.CompareTo(b.StartOffset));
        
        var merged = new List<EncodingSegment>(segments.Count);
        var current = segments[0];

        for (int i = 1; i < segments.Count; i++)
        {
            var next = segments[i];

            if (Equals(current.Encoding, next.Encoding) &&
                current.StartOffset + current.Length == next.StartOffset)
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
    
    private static async Task<List<EncodingSegment>> DetectEncodingBoundaries(
        string filePath,
        long startOffset,
        long length,
        float confidenceThreshold = 0.99f)
    {
        var segments = new List<EncodingSegment>();
        byte[] buffer = new byte[64 * 1024]; // Reused buffer for efficiency

        // Use an explicit stack to avoid deep recursion
        var stack = new Stack<(long start, long end)>();
        stack.Push((startOffset, startOffset + length));

        await using var stream = File.OpenRead(filePath);

        while (stack.Count > 0)
        {
            var (start, end) = stack.Pop();
            long size = end - start;
            if (size <= 0) continue;

            // Read range
            stream.Seek(start, SeekOrigin.Begin);
            int readSize = (int)Math.Min(size, buffer.Length);
            int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, readSize));
            if (bytesRead <= 0) continue;

            var result = CharsetDetector.DetectFromBytes(buffer, 0, bytesRead);
            float confidence = result?.Detected?.Confidence ?? 0f;
            var encoding = result?.Detected?.Encoding;

            if (confidence >= confidenceThreshold || size == 1)
            {
                // Accept this range as one consistent encoding
                segments.Add(new EncodingSegment
                {
                    StartOffset = start,
                    Length = size,
                    Encoding = encoding,
                    Confidence = confidence
                });
            }
            else
            {
                // Split in half for finer resolution
                long mid = start + size / 2;
                stack.Push((mid, end));
                stack.Push((start, mid));
            }
        }

        return segments;
    }

    // sampleSize predefined to 4KB
    private async Task<List<EncodingSegment>> DetectConcatenatedEncoding(int sampleSize = 4 * 1024)
    {
        await logger.LogConcatEncDetection();
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
        await logger.LogConsistEncDetection();
        
        string message;
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

        if (result?.Detected == null)
        {
            message = "Consistent encoding detection failed: returned null.";
        }
        else
        {
            message = $"Consistent encoding detection failed: detected [{result.Detected?.Encoding?.WebName}] " +
                      $"with low confidence [{result.Detected?.Confidence:F2}].";
        }

        await logger.Log(message);
        
        return segments;
    }

    public async Task<List<EncodingSegment>> DetectFileEncodings()
    {
        string message;
        _stopWatch.Start();
        await logger.LogEncodingHeader();

        List<EncodingSegment> encSegments = await DetectConsistentEncoding();
        if (encSegments.Count != 1)
        {
            encSegments = await DetectConcatenatedEncoding();
            encSegments = MergeSameEncodingSegments(encSegments);
        }

        if (encSegments.Count == 1)
        {
            message = $"Encoding finished successfully. Detected consistent [{encSegments[0].Encoding!.WebName}] " +
                      $"with [{encSegments[0].Confidence:F2}] confidence. Time taken: {_stopWatch.Elapsed}, " +
                      $"current DateTime: {DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}";
            
            await logger.Log(message, LogLevel.Success, LogContext.Encoding);
        }
        else if (encSegments.Count > 1)
        {
            // Avoiding LINQ
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var segment in encSegments)
                if (segment.Encoding?.WebName is { } name)
                    set.Add(name);
            int differentEncodings = set.Count;

            message = $"Encoding finished successfully. Detected concatenated encoding with " +
                      $"{differentEncodings} different encodings. Time taken: {_stopWatch.Elapsed}, " +
                      $"current DateTime: {DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}";
            
            await logger.Log(message, LogLevel.Success, LogContext.Encoding);
        }
        else
        {
            message = $"Encoding detection failed. Detector did not detect any encoding. Time taken: {_stopWatch.Elapsed}, " +
                      $"current DateTime: {DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}";
            
            await logger.Log(message, LogLevel.Warning, LogContext.Encoding);
        }
        
        await logger.LogEncodingStats(encSegments);
        await logger.LogEncodingDetails(encSegments);
        
        return encSegments;
    }
}
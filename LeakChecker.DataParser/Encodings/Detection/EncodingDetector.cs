using System.Diagnostics;
using System.Text;
using LeakChecker.DataParser.Logging;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Stats.Parse;
using LeakChecker.DataParser.Utilities;
using LeakChecker.DataParser.Utilities.Extensions;
using UtfUnknown;

namespace LeakChecker.DataParser.Encodings.Detection;

public class EncodingDetector(string filePath, IParseLogger logger, IParseStats stats, float threshold = 0.99f)
{
    public async Task<List<EncodingSegment>> DetectEncodingSegments()
    {
        await logger.LogEncodingHeader();
        Stopwatch sw = Stopwatch.StartNew();

        List<EncodingSegment> encSegments = await DetectConsistentEncoding();
        if (encSegments.Count != 1)
        {
            encSegments = await DetectConcatenatedEncoding();
            encSegments = MergeSameEncodingSegments(encSegments);
        }

        if (encSegments.Count == 1)
        {
            stats.Encoding = encSegments[0].Encoding;
            await logger.Log($"Encoding detection finished successfully. Detected consistent [{encSegments[0].Encoding?.WebName}] with " +
                             $"[{encSegments[0].Confidence:F2}] confidence. Time taken: {sw.Elapsed}.", LogLevel.Success, LogContext.Encoding);
        }
        else if (encSegments.Count > 1)
        {
            var distinctEncSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var segment in encSegments)
                if (segment.Encoding?.WebName is { } name)
                    distinctEncSet.Add(name);

            await logger.Log($"Encoding detection finished successfully. Detected concatenated encoding with {distinctEncSet.Count} " +
                             $"distinct encodings. Time taken: {sw.Elapsed}.", LogLevel.Success, LogContext.Encoding);
        }
        else
        {
            await logger.Log($"Encoding detection failed. Detector did not detect any encoding. Time taken: {sw.Elapsed}.",
                            LogLevel.Warning, LogContext.Encoding);
        }
        
        stats.EncodingSegments = encSegments;
        await logger.LogEncodingStats(encSegments);
        await logger.LogEncodingDetails(encSegments);
        
        return encSegments;
    }
    
    private async Task<List<EncodingSegment>> DetectConsistentEncoding()
    {
        await logger.Log("Detection of consistent encoding.");
        
        var segments = new List<EncodingSegment>();
        await using var stream = File.OpenRead(filePath);

        // Ensure detector will read all stream
        var result = CharsetDetector.DetectFromStream(stream, stream.Length);
        
        stats.Encoding = result?.Detected?.Encoding;
        if (result?.Detected?.Confidence >= threshold )
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
            await logger.Log("Consistent encoding detection failed: returned [NULL].");
        else
            await logger.Log($"Consistent encoding detection not satisfied: detected [{result.Detected?.Encoding?.WebName}] " +
                             $"with low confidence [{result.Detected?.Confidence:F2}]."); 

        return segments;
    }
    
    private async Task<List<EncodingSegment>> DetectConcatenatedEncoding(int sampleSize = SizeEnum.KiloByte * 4)
    {
        await logger.Log("Detection of concatenated encoding.");
        int bytesRead;
        long offset = 0;
        var buffer = new byte[sampleSize];
        var segments = new List<EncodingSegment>();

        await using var stream = File.OpenRead(filePath);

        while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
        {
            var result = CharsetDetector.DetectFromBytes(buffer, 0, bytesRead);
            float confidence = result?.Detected?.Confidence ?? 0f;
            Encoding? encoding = result?.Detected?.Encoding;

            if (confidence >= threshold)
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
                var preciseSegments = await DetectEncodingBoundaries(offset, bytesRead);
                segments.AddRange(preciseSegments);
            }

            offset += bytesRead;
        }

        return segments;
    }
    
    private async Task<List<EncodingSegment>> DetectEncodingBoundaries(long startOffset, long length)
    {
        var segments = new List<EncodingSegment>();
        byte[] buffer = new byte[SizeEnum.KiloByte * 64]; // Reused buffer for efficiency

        // Use an explicit stack to avoid deep recursion
        var stack = new Stack<(long start, long end)>();
        stack.Push((startOffset, startOffset + length));

        await using var stream = File.OpenRead(filePath);

        while (stack.Count > 0)
        {
            var (start, end) = stack.Pop();
            long size = end - start;
            if (size <= 0)
                continue;

            // Read range
            stream.Seek(start, SeekOrigin.Begin);
            int readSize = (int)Math.Min(size, buffer.Length);
            int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, readSize));
            if (bytesRead <= 0)
                continue;

            var result = CharsetDetector.DetectFromBytes(buffer, 0, bytesRead);
            float confidence = result?.Detected?.Confidence ?? 0f;
            var encoding = result?.Detected?.Encoding;

            if (confidence >= threshold || size <= 4)
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
    
    private static List<EncodingSegment> MergeSameEncodingSegments(List<EncodingSegment> segments)
    {
        if (segments.Count <= 1)
            return segments;

        segments.Sort(static (a, b) => a.StartOffset.CompareTo(b.StartOffset));

        var merged = new List<EncodingSegment>(segments.Count);
        var current = segments[0];

        for (int i = 1; i < segments.Count; i++)
        {
            var next = segments[i];

            bool contiguous = current.StartOffset + current.Length == next.StartOffset;
            if (!contiguous)
            {
                merged.Add(current);
                current = next;
                continue;
            }

            if (TryGetMergedEncoding(current.Encoding, next.Encoding, out var mergedEncoding))
            {
                current = new EncodingSegment
                {
                    StartOffset = current.StartOffset,
                    Length = current.Length + next.Length,
                    Encoding = mergedEncoding,
                    Confidence = (current.Confidence * current.Length + next.Confidence * next.Length) /
                                 (current.Length + next.Length)
                };
            }
            else
            {
                merged.Add(current);
                current = next;
            }
        }

        merged.Add(current);
        return merged;
    }

    private static bool TryGetMergedEncoding(Encoding? a, Encoding? b, out Encoding? merged)
    {
        // Both unknown - merge
        if (a is null && b is null)
        {
            merged = null;
            return true;
        }

        // One known, one unknown - do not merge
        if (a is null || b is null)
        {
            merged = null;
            return false;
        }

        // Both known - merge
        if (a.CodePage == b.CodePage)
        {
            merged = a;
            return true;
        }

        if (IsExactSubsetEncoding(a, b))
        {
            merged = b;
            return true;
        }

        if (IsExactSubsetEncoding(b, a))
        {
            merged = a;
            return true;
        }

        merged = null;
        return false;
    }

    private static bool IsExactSubsetEncoding(Encoding subset, Encoding superset)
    {
        if (subset.CodePage == superset.CodePage)
            return true;

        // Only prove relations we can prove exactly.
        if (subset.CodePage == 20127) // US-ASCII
            return superset.IsAsciiSuperset();

        return false;
    }
}
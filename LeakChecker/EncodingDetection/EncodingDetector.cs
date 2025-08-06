using System.Globalization;
using System.Text;
using LeakChecker.FileTracking;
using LeakChecker.Utilities;
using UtfUnknown;

namespace LeakChecker.EncodingDetection;

public class EncodingDetector
{
    private async Task<List<EncodingSegment>> ProcessFileAsConsistentEncoding(FileContext file)
    {
        var segments = new List<EncodingSegment>();
        await using var stream = File.OpenRead(file.Path);

        // Ensure detector will read all file
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
            await file.Log("Consistent encoding detection failed.", LogLevel.Warning, LogContext.Encoding);
        
        if (result != null)
            await file.Log($"Consistent encoding detection not satisfied [{result.Detected?.EncodingName}] " +
                           $"with low confidence [{result.Detected?.Confidence:F2}]", LogLevel.Warning, LogContext.Encoding);
        
        return segments;
    }

    private async Task<List<EncodingSegment>> ProcessFileAsConcatenatedEncoding(string filePath, int sampleSize = 4 * 1024 )
    {
        int bytesRead;
        long offset = 0;
        var buffer = new byte[sampleSize];
        var segments = new List<EncodingSegment>();

        await using var stream = File.OpenRead(filePath);

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
                var preciseSegments = await DetectEncodingsWithPrecision(filePath, offset, bytesRead);
                segments.AddRange(preciseSegments);
            }

            offset += bytesRead;
        }

        return segments;
    }


    public async Task<List<EncodingSegment>> DetectEncodingFromFilePath(FileContext file)
    {
        List<EncodingSegment> result;
        await file.LogEncodingProcessingStart();

        result = await ProcessFileAsConsistentEncoding(file);
        if (result.Count == 1) return await MergeSequenceOfSameEncodingSegments(result, file);

        result = await ProcessFileAsConcatenatedEncoding(file.Path);
        return await MergeSequenceOfSameEncodingSegments(result, file);
    }
    
    private async Task<List<EncodingSegment>> DetectEncodingsWithPrecision(string filePath, long startOffset, long length, int minBlockSize = 1)
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

    private async Task<List<EncodingSegment>> MergeSequenceOfSameEncodingSegments(List<EncodingSegment> segments, FileContext file)
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
        
        await file.Log($"Encoding processing finished successfully. Current DateTime: " +
                       $"{DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}", LogLevel.Success, LogContext.Encoding);
        await file.LogEncodingStats(merged);
        await file.LogEncodingDetails(merged);
        
        return merged;
    }
    
    // Read & process each segment with correct encoding
    public async Task ReadAndProcessWithDynamicEncodings(string filePath, List<EncodingSegment> segments)
    {
        await using var stream = File.OpenRead(filePath);

        Logger.LogInfo(filePath);
        Console.WriteLine("seg count: " + segments.Count);
        foreach (var segment in segments)
        {
            Logger.LogInfo("enc name:" + segment.EncodingName);
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
    
    //TODO tmp bypass to avoid encoding detection with segments
    public async Task<Encoding> DetectEncodingFromOneStream(FileContext file)
    {
        await using var stream = File.OpenRead(file.Path);
        await file.LogEncodingProcessingStart();
        var result = CharsetDetector.DetectFromStream(stream);
        {
            try
            {
                string encoding = result.Detected?.Encoding?.WebName ?? string.Empty;
                float confidence = result.Detected?.Confidence ?? 0f;
                if (!string.IsNullOrEmpty(encoding) || confidence > 0f)
                {
                    await file.Log($"Encoding detected [{encoding}] with confidence [{confidence}]");
                }
                return Encoding.GetEncoding(encoding);
            }
            catch (Exception e)
            {
                
                await file.Log($"Encoding detection failed. {e.Message} Fallback to [{Encoding.UTF8.WebName}]", LogLevel.Exception);
                await file.Log("Encoding processing complete");
                return Encoding.UTF8;
            }
        }
    }
}
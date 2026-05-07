using System.Diagnostics;
using System.Text;
using LeakChecker.DataParser.Helpers.Enums;
using LeakChecker.DataParser.Logging;
using LeakChecker.DataParser.Logging.Parse;

namespace LeakChecker.DataParser.Encodings.Conversion;

public static class EncodingConverter
{
    private static readonly string Utf8Name = Encoding.UTF8.WebName;
    private static readonly string AsciiName = Encoding.ASCII.WebName;
    private static readonly DecoderReplacementFallback FallbackDrop = new(string.Empty); // Drop invalid bytes
    
    public static readonly Encoding Utf8 = Encoding.GetEncoding(Utf8Name, 
        encoderFallback: new EncoderReplacementFallback(string.Empty),  // Drop unencodable chars
        decoderFallback: new DecoderReplacementFallback(string.Empty)); // Drop invalid bytes

    /// <summary>
    /// Converts a file with mixed encodings into a single UTF-8 encoded output file.
    /// </summary>
    public static async Task<string> ConvertFileToUtf8(List<EncodingSegment> encodingSegments, IParseLogger logger, int bufferSize = SizeEnum.MegaByte * 2)
    {
        // File is already in UTF-8 or US-ASCII as a single segment
        if (encodingSegments is [{ Encoding: not null }] &&
            (
                Equals(encodingSegments[0].Encoding?.WebName, Utf8Name) ||
                Equals(encodingSegments[0].Encoding?.WebName, AsciiName)
            ))
        {
            return logger.SubjectFilePath;
        }

        logger.LogEncodingConversion();
        Stopwatch sw = Stopwatch.StartNew();

        await using var inStream = File.OpenRead(logger.SubjectFilePath);
        await using var outStream = new StreamWriter(logger.SubjectTmpFilePath, false, Utf8);

        foreach (var segment in encodingSegments)
        {
            if (segment.Encoding == null)
            {
                logger.Log($"Encoding missing for encoding segment [{segment.ToByteString()}]. Set UTF-8 as default.");
                segment.Encoding = Utf8;
            }

            inStream.Seek(segment.StartOffset, SeekOrigin.Begin);

            long remaining = segment.Length;
            byte[] buffer = new byte[bufferSize];
            Decoder decoder = segment.Encoding.GetDecoder();
            decoder.Fallback = FallbackDrop;
            char[] charBuffer = new char[segment.Encoding.GetMaxCharCount(bufferSize)];

            while (remaining > 0)
            {
                int toRead = remaining > bufferSize ? bufferSize : (int)remaining;
                int bytesRead = await inStream.ReadAsync(buffer.AsMemory(0, toRead));
                if (bytesRead == 0) break;

                remaining -= bytesRead;

                int charsDecoded = decoder.GetChars(buffer, 0, bytesRead, charBuffer, 0, flush: remaining == 0);
                await outStream.WriteAsync(charBuffer, 0, charsDecoded);
            }
        }

        await outStream.FlushAsync();
        
        logger.Log($"Encoding conversion finished. Converted {encodingSegments.Count} segments. Time taken: {sw.Elapsed}", LogLevel.Success, LogContext.Encoding);
        
        return logger.SubjectTmpFilePath;
    }
}
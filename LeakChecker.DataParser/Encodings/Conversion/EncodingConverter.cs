using System.Diagnostics;
using System.Text;
using LeakChecker.DataParser.Logging;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Utilities;

namespace LeakChecker.DataParser.Encodings.Conversion;

public static class EncodingConverter
{
    /// <summary>
    /// Converts a file with mixed encodings into a single UTF-8 encoded output file.
    /// If the file is already UTF-8 (single segment), it is just copied.
    /// </summary>
    public static async Task ConvertFileToUtf8(
        List<EncodingSegment> encodingSegments, IParseLogger logger, IParseStats stats, int bufferSize = SizeEnum.MegaByte)
    {
        string inputFilePath = logger.SubjectFilePath;
        string outputFilePath = logger.SubjectTmpFilePath;
        Stopwatch sw = Stopwatch.StartNew();
        Encoding utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false); // no BOM
        
        // If file is already in UTF-8 as a single UTF-8 segment, just copy
        if (encodingSegments is [{ Encoding: not null }] && Equals(encodingSegments[0].Encoding?.WebName, Encoding.UTF8.WebName))
        {
            File.Copy(inputFilePath, outputFilePath, overwrite: true);
            await logger.Log($"File is already in UTF-8 encoding. Copy takes {sw.Elapsed}", LogLevel.Info, LogContext.Encoding);
            return;
        }

        await using var inputStream = File.OpenRead(inputFilePath);
        await using var outputStream = new StreamWriter(outputFilePath, false, utf8);

        foreach (var segment in encodingSegments)
        {
            if (segment.Encoding == null)
            {
                await logger.LogEncodingConversion($"Encoding missing for encoding segment [{segment.ToByteString()}]. Set UTF-8 as default.");
                segment.Encoding = utf8;
            }

            inputStream.Seek(segment.StartOffset, SeekOrigin.Begin);

            long remaining = segment.Length;
            byte[] buffer = new byte[bufferSize];
            Decoder decoder = segment.Encoding.GetDecoder();
            char[] charBuffer = new char[segment.Encoding.GetMaxCharCount(bufferSize)];

            while (remaining > 0)
            {
                int toRead = remaining > bufferSize ? bufferSize : (int)remaining;
                int bytesRead = await inputStream.ReadAsync(buffer.AsMemory(0, toRead));
                if (bytesRead == 0) break;

                remaining -= bytesRead;

                int charsDecoded = decoder.GetChars(buffer, 0, bytesRead, charBuffer, 0, flush: remaining == 0);
                await outputStream.WriteAsync(charBuffer, 0, charsDecoded);
            }
        }

        await outputStream.FlushAsync();
        await logger.LogEncodingConversion($"{encodingSegments.Count} encoding segments conversion takes {sw.Elapsed}");
    }
}
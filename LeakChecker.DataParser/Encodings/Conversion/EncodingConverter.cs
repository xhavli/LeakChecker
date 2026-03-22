using System.Diagnostics;
using System.Text;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Utilities;

namespace LeakChecker.DataParser.Encodings.Conversion;

public static class EncodingConverter
{
    /// <summary>
    /// Converts a file with mixed encodings into a single UTF-8 encoded output file.
    /// If the file is already UTF-8 (single segment), it is just copied.
    /// </summary>
    public static async Task<string> ConvertFileToUtf8(List<EncodingSegment> encodingSegments, IParseLogger logger, int bufferSize = SizeEnum.MegaByte * 2)
    {
        // If file is already in UTF-8 as a single UTF-8 segment
        if (encodingSegments is [{ Encoding: not null }] && Equals(encodingSegments[0].Encoding?.WebName, Encoding.UTF8.WebName))
            return logger.SubjectFilePath;

        await logger.LogEncodingConversion();
        Stopwatch sw = Stopwatch.StartNew();
        Encoding utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false); // no BOM

        await using var inStream = File.OpenRead(logger.SubjectFilePath);
        await using var outStream = new StreamWriter(logger.SubjectTmpFilePath, false, utf8);

        foreach (var segment in encodingSegments)
        {
            if (segment.Encoding == null)
            {
                await logger.Log($"Encoding missing for encoding segment [{segment.ToByteString()}]. Set UTF-8 as default.");
                segment.Encoding = utf8;
            }

            inStream.Seek(segment.StartOffset, SeekOrigin.Begin);

            long remaining = segment.Length;
            byte[] buffer = new byte[bufferSize];
            Decoder decoder = segment.Encoding.GetDecoder();
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
        
        await logger.Log($"Encoding conversion of {encodingSegments.Count}. Time taken: {sw.Elapsed}");
        
        return logger.SubjectTmpFilePath;
    }
}
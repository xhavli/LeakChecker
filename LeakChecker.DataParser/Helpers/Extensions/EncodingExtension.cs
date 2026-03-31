using System.Collections.Concurrent;
using System.Text;

namespace LeakChecker.DataParser.Helpers.Extensions;

public static class EncodingExtension
{
    private static readonly Encoding StrictAscii =
        Encoding.GetEncoding(
            20127, // US-ASCII
            EncoderFallback.ExceptionFallback,
            DecoderFallback.ExceptionFallback);

    private static readonly ConcurrentDictionary<int, bool> AsciiSupersetCache = new();

    /// <summary>
    /// Determines whether the given encoding is a strict superset of 7-bit ASCII (0x00–0x7F),
    /// meaning it decodes and re-encodes ASCII bytes identically.
    /// </summary>
    public static bool IsAsciiSuperset(this Encoding candidateSuperset)
    {
        ArgumentNullException.ThrowIfNull(candidateSuperset);

        return AsciiSupersetCache.GetOrAdd(candidateSuperset.CodePage, static codePage =>
            {
                var superset = Encoding.GetEncoding(
                    codePage,
                    EncoderFallback.ExceptionFallback,
                    DecoderFallback.ExceptionFallback);

                // Explicit exclusions:
                // UTF-7 (65000) is stateful/shifted
                // UTF-16 LE (1200) and BE (1201) use multibyte code units
                // UTF-32 LE (12000) and BE (12001) use multibyte code units
                if (superset.CodePage is 65000 or 1200 or 1201 or 12000 or 12001)
                    return false;

                for (int b = 0; b <= 0x7F; b++)
                {
                    byte[] one = [(byte)b];
                    string asciiText;
                    string superText;
                    try
                    {
                        asciiText = StrictAscii.GetString(one);
                        superText = superset.GetString(one);
                    }
                    catch
                    {
                        return false;
                    }
                    
                    if (!string.Equals(asciiText, superText, StringComparison.Ordinal))
                        return false;

                    byte[] roundTrip;
                    try
                    {
                        roundTrip = superset.GetBytes(asciiText);
                    }
                    catch
                    {
                        return false;
                    }

                    if (roundTrip.Length != 1 || roundTrip[0] != (byte)b)
                        return false;
                }

                return true;
            });
    }
}
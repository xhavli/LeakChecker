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
    
    public static bool IsSubsetOf(this Encoding subset, Encoding superset)
    {
        ArgumentNullException.ThrowIfNull(subset);
        ArgumentNullException.ThrowIfNull(superset);

        if (subset.CodePage == superset.CodePage)
            return true;

        // Subset is ASCII and superset is any of ASCII-superset
        if (subset.IsAscii())
            return superset.IsAsciiSuperset();

        // Subset is x-cp20936 and superset is gb2312 or GB18030
        if (subset.IsCp20936())
            return superset.IsGb2312() || superset.IsGb18030();

        // Subset is gb2312 and superset is GB18030
        if (subset.IsGb2312())
            return superset.IsGb18030();

        return false;
    }
    
    /// <summary>
    /// Determines whether the given encoding is a strict superset of 7-bit ASCII (0x00–0x7F),
    /// meaning it decodes and re-encodes ASCII bytes identically.
    /// </summary>
    private static bool IsAsciiSuperset(this Encoding candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        return AsciiSupersetCache.GetOrAdd(candidate.CodePage, static codePage =>
            {
                var superset = Encoding.GetEncoding(
                    codePage,
                    EncoderFallback.ExceptionFallback,
                    DecoderFallback.ExceptionFallback);

                // Explicit exclude encodings non-compatible with ASCII:
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

    private static bool IsAscii(this Encoding encoding) =>
        encoding.CodePage == 20127;

    private static bool IsCp20936(this Encoding encoding) =>
        encoding.CodePage == 20936;

    private static bool IsGb2312(this Encoding encoding) =>
        encoding.CodePage == 936;

    private static bool IsGb18030(this Encoding encoding) =>
        encoding.CodePage == 54936;
}
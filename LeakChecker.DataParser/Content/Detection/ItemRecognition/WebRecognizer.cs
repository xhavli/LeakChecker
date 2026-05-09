using LeakChecker.DataParser.Helpers.Extensions;
using Microsoft.Recognizers.Text.Sequence;

namespace LeakChecker.DataParser.Content.Detection.ItemRecognition;

public static class WebRecognizer
{
    private const string Culture = Microsoft.Recognizers.Text.Culture.English; // or Culture.EnglishOthers

    public static Boolean TryRecognize(string line, out List<string> stringUris, out List<Uri> uris)
    {
        stringUris = new List<string>();
        uris = new List<Uri>();
        bool found = false;
        
        var results = SequenceRecognizer.RecognizeURL(line, Culture);
        foreach (var result in results)
        {
            int start = result.Start;
            int end = result.End;
            string textUri = line.Substring(start, end - start + 1);
            
            // if (textUri.Contains('_')) { continue; }    // TODO domain name cant contain '_'
            
            if (Uri.TryCreate(textUri, UriKind.RelativeOrAbsolute, out Uri? uri))
            {
                stringUris.Add(textUri);
                uris.Add(uri);
                found = true;
            }
        }

        return found;
    }
    
    public static string? ExtractReversedDomain(string value)
    {
        ReadOnlySpan<char> span = value.AsSpan().Trim();
        if (span.IsEmpty) return null;

        // Strip scheme: http:// https:// ftp:// etc.
        int schemeEnd = span.IndexOf("://".AsSpan(), StringComparison.Ordinal);
        if (schemeEnd >= 0)
            span = span[(schemeEnd + 3)..];

        // Strip leading www. (case-insensitive)
        if (span.StartsWith("www.".AsSpan(), StringComparison.OrdinalIgnoreCase))
            span = span[4..];

        if (span.IsEmpty) return null;

        // Strip path/query/fragment — take only up to first / ? # and port :
        int end = span.Length;
        for (int i = 0; i < span.Length; i++)
        {
            char c = span[i];
            if (c == '/' || c == '?' || c == '#' || c == ':')
            {
                end = i;
                break;
            }
        }

        span = span[..end];
        if (span.IsEmpty) return null;

        // Must contain a dot and no spaces
        bool hasDot = false;
        for (int i = 0; i < span.Length; i++)
        {
            char c = span[i];
            if (c == '.') { hasDot = true; continue; }
            if (char.IsWhiteSpace(c)) return null;
        }
        if (!hasDot) return null;

        // Lowercase + reverse
        return span.ToString().ToLowerInvariant().ReverseString();
    }
}
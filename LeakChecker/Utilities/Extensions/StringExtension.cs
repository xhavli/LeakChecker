namespace LeakChecker.Utilities.Extensions;

public static class StringExtensions
{
    public static string TrimOuterQuotes(this string input)
    {
        if (input.Length < 2) return input;
        
        ReadOnlySpan<char> span = input.AsSpan();
        char first = span[0];
        char last = span[^1];

        // Check if first and last are the same allowed quote character
        if (first == last && first is '\'' or '"' or '`')
        {
            // Slice without allocating, then create string once
            return span.Slice(1, span.Length - 2).ToString();
        }

        return input;
    }
    
    public static string TrimOuterParentheses(this string input)
    {
        if (input.Length < 2) return input;

        ReadOnlySpan<char> span = input.AsSpan();

        if (span[0] == '(' && span[^1] == ')')
        {
            return span.Slice(1, span.Length - 2).ToString();
        }

        return input;
    }
    
    public static string TrimOuterParenthesesAndComma(this string input)
    {
        if (input.Length < 3) return input;

        ReadOnlySpan<char> span = input.AsSpan();

        // Case 1: ",(" ... ")"
        if (span.StartsWith(",(".AsSpan()) && span.EndsWith(")".AsSpan()))
        {
            return span.Slice(2, span.Length - 3).ToString();
        }

        // Case 2: "(" ... "),"
        if (span.StartsWith("(".AsSpan()) && span.EndsWith("),".AsSpan()))
        {
            return span.Slice(1, span.Length - 3).ToString();
        }

        return input;
    }
    
    public static string TrimOuterWhiteSpace(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        ReadOnlySpan<char> span = input.AsSpan();

        int start = 0;
        int end = span.Length - 1;

        // Move start forward past leading whitespace
        while (start <= end && char.IsWhiteSpace(span[start]))
            start++;

        // Move end backward past trailing whitespace
        while (end >= start && char.IsWhiteSpace(span[end]))
            end--;

        // If no outer whitespace trimmed, return original
        if (start == 0 && end == span.Length - 1)
            return input;

        // Slice without allocation, then create the string
        return span.Slice(start, end - start + 1).ToString();
    }
}
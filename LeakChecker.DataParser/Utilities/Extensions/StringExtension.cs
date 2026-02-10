namespace LeakChecker.Utilities.Extensions;

public static class StringExtensions
{
    public static string TrimEnclosingChars(this string input)
    {
        if (input.Length < 2) return input;
        
        ReadOnlySpan<char> span = input.AsSpan();
        char first = span[0];
        char last = span[^1];

        // Case: ' ... ', " ... ", ` ... `, | ... |
        if (first == last && first is '\'' or '"' or '`' or '|')
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

        // Case: "( ... )"
        if (span[0] == '(' && span[^1] == ')')
        {
            return span.Slice(1, span.Length - 2).ToString();
        }
        
        // Case: "< ... >"
        if (span[0] == '<' && span[^1] == '>')
        {
            return span.Slice(1, span.Length - 2).ToString();
        }
        
        // Case: "[ ... ]"
        if (span[0] == '[' && span[^1] == ']')
        {
            return span.Slice(1, span.Length - 2).ToString();
        }

        return input;
    }
    
    public static string TrimOuterParenthesesWithComma(this string input)
    {
        if (input.Length < 3) return input;

        ReadOnlySpan<char> span = input.AsSpan();

        // Case: ",( ... )"
        if (span.StartsWith(",(".AsSpan()) && span.EndsWith(")".AsSpan()))
        {
            return span.Slice(2, span.Length - 3).ToString();
        }

        // Case: "( ... ),"
        if (span.StartsWith("(".AsSpan()) && span.EndsWith("),".AsSpan()))
        {
            return span.Slice(1, span.Length - 3).ToString();
        }

        return input;
    }
}
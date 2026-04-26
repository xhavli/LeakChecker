using System.Buffers;
using System.Globalization;

namespace LeakChecker.DataParser.Helpers.Extensions;

public static class StringExtensions
{
    private const string Insert = "INSERT ";
    private const string Into = " INTO ";
    private const string Values = " VALUES";
    private const string Create = "CREATE ";
    private const string Table = " TABLE ";
    private const string Drop = "DROP ";
    
    public static string TrimEnclosingChars(this string input)
    {
        if (input.Length < 2) return input;
        
        ReadOnlySpan<char> span = input.AsSpan();
        char first = span[0];
        char last = span[^1];

        // Case: ' ... ', ` ... `, " ... ", | ... |
        if (first == last && first is '\'' or '`' or '"' or '|')
            return span.Slice(1, span.Length - 2).ToString();

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
    
    public static bool IsTrashOrEmpty(this string line)
    {
        if (line.IsSqlDropTable())
            return true;
        
        if (string.IsNullOrWhiteSpace(line))
            return true;
        
        line = line.Replace(" ", "");

        if (line == ";")    // or line.Length == 1 
            return true;
        
        if (line.IsPossibleAsciiTable())
            return true;
        
        if (line.All(ch => char.GetUnicodeCategory(ch) == UnicodeCategory.DashPunctuation)) // or UnicodeCategory.OtherPunctuation 
            return true; // SQL comment boundary
        if (line.LastIndexOf("--", StringComparison.Ordinal) == 0)
            return true; // SQL comment start
        
        return false;
    }
    
    public static string ReverseString(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return string.Create(input.Length, input, (span, state) =>
        {
            for (int i = 0, j = state.Length - 1; i < state.Length; i++, j--)
            {
                span[i] = state[j];
            }
        });
    }
    
    public static string RemoveWhitespaces(this string input)
    {
        char[] buffer = ArrayPool<char>.Shared.Rent(input.Length);
        int j = 0;

        foreach (char c in input)
        {
            if (!char.IsWhiteSpace(c))
                buffer[j++] = c;
        }

        string result = new string(buffer, 0, j);
        ArrayPool<char>.Shared.Return(buffer);

        return result;
    }
    
    
    
    // DROP [modifiers] TABLE [modifiers] <table_name> [ , <table_name> ... ] ;
    private static bool IsSqlDropTable(this string line)
    {
        int drop = line.IndexOf(Drop, StringComparison.OrdinalIgnoreCase);
        if (drop != 0)
            return false;

        int table = line.IndexOf(Table, StringComparison.OrdinalIgnoreCase);
        if (table < 0)
            return false;
        
        bool validSemicolon = line.EndsWith(';') && 
                              line.IndexOf(';') == line.LastIndexOf(';');

        return drop < table && validSemicolon;
    }
    
    // INSERT [modifiers] INTO <table_name> [ (columns...) ] VALUES
    // ( literal , literal , literal , literal ),
    // ( ... );
    public static bool IsSqlInsert(this string line)
    {
        int start = line.IndexOf(Insert, StringComparison.OrdinalIgnoreCase);
        if (start != 0)
            return false;
        
        int into = line.IndexOf(Into, StringComparison.OrdinalIgnoreCase);
        if (into < 0)
            return false;
        
        int openParen = line.IndexOf('(', into);
        if (openParen == -1)
            return false;
        
        int closeParen = line.IndexOf(')', openParen + 1);
        if (closeParen == -1)
            return false;

        int values = line.IndexOf(Values, StringComparison.OrdinalIgnoreCase);
        if (values < 0)
            return false;

        return start < into && into < openParen && openParen < closeParen && closeParen < values;
    }
    
    // CREATE [modifiers] TABLE [modifiers] <table_name>
    // ( <column> <data_type> [constraint] ,
    //   <column> <data_type> [constraint] ,
    //   ...
    // ) [options];
    public static bool IsSqlCreateTable(this string line)
    {
        int create = line.IndexOf(Create, StringComparison.OrdinalIgnoreCase);
        if (create != 0)
            return false;

        int table = line.IndexOf(Table, StringComparison.OrdinalIgnoreCase);
        if (table < 0)
            return false;

        return create < table;
    }
    
    public static bool IsSqlInsertEnd(this string line)
    {
        return line.EndsWith(");", StringComparison.Ordinal)
               || line.EndsWith(") ;", StringComparison.Ordinal)
               || line.EndsWith(")\t;", StringComparison.Ordinal);
    }
    
    // +----+ or ------
    // ASCII Table header
    public static bool IsPossibleAsciiTable(this string line)
    {
        return line.Length > 2 &&
               line.First() == line.Last() &&
               line.All(c => c == '+' || char.GetUnicodeCategory(c) == UnicodeCategory.DashPunctuation);
    }
}
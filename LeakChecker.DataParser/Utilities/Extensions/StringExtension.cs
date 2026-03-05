using System.Globalization;

namespace LeakChecker.DataParser.Utilities.Extensions;

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
            return true; // Sql comment boundary
        if (line.LastIndexOf("--", StringComparison.Ordinal) == 0)
            return true; // Sql comment start
        
        return false;
    }
    
    
    // DROP [modifiers] TABLE [modifiers] <table_name> [ , <table_name> ... ]
    public static bool IsSqlDropTable(this string line)
    {
        int drop = line.IndexOf("DROP ", StringComparison.OrdinalIgnoreCase);
        if (drop != 0)
            return false;

        int table = line.IndexOf(" TABLE ", StringComparison.OrdinalIgnoreCase);
        if (table < 0)
            return false;
        
        bool validSemicolon = line.EndsWith(';') && 
                              line.IndexOf(';') == line.LastIndexOf(';');

        return drop < table && validSemicolon;
    }
    
    // INSERT [modifiers] INTO <table_name> [ (columns...) ] VALUES
    // ( literal , literal , literal , literal ),
    // ( ... );
    public static bool IsSqlInsertValues(this string line)
    {
        int start = line.IndexOf("INSERT ", StringComparison.OrdinalIgnoreCase);
        if (start != 0)
            return false;

        int into = line.IndexOf(" INTO ", StringComparison.OrdinalIgnoreCase);
        if (into < 0)
            return false;

        int values = line.IndexOf(" VALUES", StringComparison.OrdinalIgnoreCase);
        if (values < 0)
            return false;

        return start < into && into < values;
    }
    
    // CREATE [modifiers] TABLE [modifiers] <table_name>
    // ( <column> <data_type> [constraint] ,
    //   <column> <data_type> [constraint] ,
    //   ...
    // ) [options];
    public static bool IsSqlCreateTable(this string line)
    {
        int create = line.IndexOf("CREATE ", StringComparison.OrdinalIgnoreCase);
        if (create != 0)
            return false;

        int table = line.IndexOf(" TABLE ", StringComparison.OrdinalIgnoreCase);
        if (table < 0)
            return false;

        return create < table;
    }
    
    // +----+
    // ------
    public static bool IsPossibleAsciiTable(this string line)
    {
        return line.Length > 2 &&
               line.First() == line.Last() &&
               line.All(c => c == '+' || char.GetUnicodeCategory(c) == UnicodeCategory.DashPunctuation);
    }
}
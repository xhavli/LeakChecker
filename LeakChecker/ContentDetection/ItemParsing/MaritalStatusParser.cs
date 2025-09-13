namespace LeakChecker.ContentDetection.ItemParsing;

public static class MaritalStatusParser
{
    //TODO Performance -0,0001ms per query StringComparer.OrdinalIgnoreCase but cant compare straße and strasse
    private static readonly HashSet<string> MaritalStatuses = new(StringComparer.InvariantCultureIgnoreCase)
    {
        "Single",
        "Dating",
        "Taken",
        "Partnered",
        "In a relationship",
        "Engaged",
        "Married",
        "Separated",
        "Divorced",
        "Widowed",
    };

    public static bool TryParse(string token, out string? maritalStatus)
    {
        maritalStatus = string.Empty;
        
        token = token.Trim();
        if (string.IsNullOrEmpty(token)) return false;

        if (MaritalStatuses.Contains(token))
        {
            maritalStatus = token;
            return true;
        }

        return false;
    }
}
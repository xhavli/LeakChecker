namespace LeakChecker.DataParser.Content.Detection.ItemParsing;

public static class MaritalStatusParser
{
    private static readonly HashSet<string> MaritalStatuses = new(StringComparer.InvariantCultureIgnoreCase)
    {
        "Single",
        "Dating",
        "Taken",
        "Partnered",
        "In a domestic partnership",
        "In a relationship",
        "Engaged",
        "Married",
        "Separated",
        "Divorced",
        "Widowed",
        "It's complicated",  //TODO is it a MaritalStatus?
    };

    public static bool TryParse(string token, out string maritalStatus)
    {
        if (MaritalStatuses.Contains(token))
        {
            maritalStatus = token;
            return true;
        }

        maritalStatus = string.Empty;
        return false;
    }
}
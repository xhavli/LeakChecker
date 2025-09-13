namespace LeakChecker.ContentDetection.ItemParsing;

public static class GenderParser
{
    //TODO Performance -0,0001ms per query StringComparer.OrdinalIgnoreCase but cant compare straße and strasse
    private static readonly HashSet<string> MaleValues = new(StringComparer.InvariantCultureIgnoreCase)
    {
        "man", "male", "masculino", "masc", "boy"
    };
    
    //TODO Performance -0,0001ms per query StringComparer.OrdinalIgnoreCase but cant compare straße and strasse
    private static readonly HashSet<string> FemaleValues = new(StringComparer.InvariantCultureIgnoreCase)
    {
        "woman", "female", "feminino", "fem", "girl"
    };
    
    //TODO Performance -0,0001ms per query StringComparer.OrdinalIgnoreCase but cant compare straße and strasse
    private static readonly HashSet<string> OtherValues = new(StringComparer.InvariantCultureIgnoreCase)
    {
        "trans", "transgender", "nonbinary", "non-binary"
    };
    
    public static Boolean TryParse(string token, out string gender)
    {
        gender = string.Empty;

        token = token.Trim();
        if (string.IsNullOrEmpty(token)) return false;

        if (MaleValues.Contains(token))
        {
            gender = "Male";
            return true;
        }
        if (FemaleValues.Contains(token))
        {
            gender = "Female";
            return true;
        }
        if (OtherValues.Contains(token))
        {
            gender = "Other";
            return true;
        }

        return false;
    }
}
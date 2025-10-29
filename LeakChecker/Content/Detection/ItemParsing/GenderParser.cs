namespace LeakChecker.Content.Detection.ItemParsing;

public static class GenderParser
{
    private static readonly HashSet<string> MaleValues = new(StringComparer.InvariantCultureIgnoreCase)
    {
        "man", "male", "masculino", "masc", "boy", "he"
    };
    
    private static readonly HashSet<string> FemaleValues = new(StringComparer.InvariantCultureIgnoreCase)
    {
        "woman", "female", "feminino", "fem", "girl", "she"
    };
    
    private static readonly HashSet<string> OtherValues = new(StringComparer.InvariantCultureIgnoreCase)
    {
        "trans", "transgender", "nonbinary", "non-binary", "shemale"
    };
    
    public static Boolean TryParse(string token, out string gender)
    {
        gender = token;
        
        if (MaleValues.Contains(token)) return true;
        if (FemaleValues.Contains(token)) return true;
        if (OtherValues.Contains(token)) return true;

        gender = string.Empty;
        return false;
    }
}
using System.Text.RegularExpressions;
using IbanNet;
using IbanNet.Registry;

namespace LeakChecker.DataParser.Content.Detection.ItemParsing;

public static class IbanParser
{
    private static readonly IbanValidator Validator = new();
    private static readonly IbanNet.IbanParser Parser = new(IbanRegistry.Default);

    private const RegexOptions Options = RegexOptions.Compiled | RegexOptions.IgnoreCase;
    private static readonly Regex IbanPrefixRegex = new(@"^[A-Z]{2}\d{2}", Options);

    public static bool TryParse(string token)
    {
        token = token.Replace("-", "").Replace(" ", "");
        
        if (token.Length is < 15 or > 34) return false;
        if (!IbanPrefixRegex.IsMatch(token)) return false;
        
        ValidationResult validationResult = Validator.Validate(token);
        
        return validationResult.IsValid || Parser.TryParse(token, out _);
    }
}
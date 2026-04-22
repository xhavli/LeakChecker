using PhoneNumbers;

namespace LeakChecker.DataParser.Content.Detection.ItemParsing;

public static class PhoneNumberParser
{
    private static readonly PhoneNumberUtil PhoneUtil = PhoneNumberUtil.GetInstance();
    private static readonly HashSet<string> SupportedRegions = PhoneUtil.GetSupportedRegions();
    
    public static bool TryParse(string token, out string phoneNumber)
    {
        phoneNumber = string.Empty;
        
        string phoneNum = token.Replace(" ", "");
        
        bool hasOnlyDigits = phoneNum.All(char.IsDigit);
        bool hasNoLetters = !phoneNum.Any(char.IsLetter);
        bool plusIsValid = !phoneNum.Contains('+') || phoneNum.LastIndexOf('+') == 0;
        if (hasNoLetters && (hasOnlyDigits || plusIsValid))
        {
            string phoneNumInternational = phoneNum;
            
            if (!phoneNum.StartsWith('+'))
                phoneNumInternational = "+" + phoneNum;
            
            try
            {
                var number = PhoneUtil.Parse(phoneNumInternational, null);
                if (PhoneUtil.IsValidNumber(number))
                {
                    phoneNumber = PhoneUtil.Format(number, PhoneNumberFormat.E164); // International format with '+' and numbers together
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        
        return false;
    }
}
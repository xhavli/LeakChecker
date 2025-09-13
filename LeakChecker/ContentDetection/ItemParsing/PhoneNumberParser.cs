using PhoneNumbers;

namespace LeakChecker.ContentDetection.ItemParsing;

public static class PhoneNumberParser
{
    private static readonly PhoneNumberUtil PhoneUtil = PhoneNumberUtil.GetInstance();

    
    public static bool TryParse(string token, out string phoneNumber)
    {
        phoneNumber = string.Empty;
        
        string phoneNum = token.Replace(" ", "");
        if (phoneNum.All(char.IsDigit) || phoneNum.StartsWith('+'))
        {
            string phoneNumInternational = phoneNum;
            if (!phoneNum.StartsWith('+'))
            {
                phoneNumInternational = "+" + phoneNum;
            }
                    
            try
            {
                var number = PhoneUtil.Parse(phoneNumInternational, null);
                if (PhoneUtil.IsValidNumber(number))
                {
                    phoneNumber = phoneNumInternational;
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

public class PhoneUtil
{
}
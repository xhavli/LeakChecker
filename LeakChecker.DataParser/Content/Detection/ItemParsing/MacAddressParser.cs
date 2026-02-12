using System.Net.NetworkInformation;

namespace LeakChecker.DataParser.Content.Detection.ItemParsing;

public static class MacAddressParser
{
    public static bool TryParse(string token, out PhysicalAddress mac)
    {
        mac = PhysicalAddress.None;
        
        string normalized = token.Replace(":", "").Replace("-", "").Replace(".", "").Replace(" ", "");
        
        if (normalized.Length != 12) return false;
        if (!normalized.All(char.IsAsciiHexDigit)) return false;

        if (PhysicalAddress.TryParse(normalized, out PhysicalAddress? macAddress) && 
            macAddress.GetAddressBytes().Length == 6)
        {
            mac = macAddress;
            return true;
        }
        
        return false;
    }
}
using System.Net;
using System.Net.Sockets;

namespace LeakChecker.Content.Detection.ItemParsing;

public static class IpAddressParser
{
    private const string Localhost = "localhost";
    private const string IpV4Localhost = "::1";
    private const string IpV6Localhost = "127.0.0.1";

    public static bool TryParse(string token, out ItemEnum ipAddressType, out IPAddress ipAddress)
    {
        ipAddressType = ItemEnum.Null;
        ipAddress = IPAddress.None;
        
        string addressPart = token;
        string? portPart = null;
        
        bool ipV6InBrackets = false;

        // Handle IPv6 with port: [addr]:port
        if (token.StartsWith('['))
        {
            int endBracket = token.IndexOf(']');
            if (endBracket == -1) return false;

            addressPart = token.Substring(1, endBracket - 1);
            if (endBracket + 1 < token.Length && token[endBracket + 1] == ':')
                portPart = token.Substring(endBracket + 2);
            ipV6InBrackets = true;
        }
        else
        {
            // IPv4 or raw IPv6 without brackets
            int lastColon = token.LastIndexOf(':');
            if (lastColon != -1 && token.Count(c => c == ':') == 1) // IPv4:port
            {
                addressPart = token.Substring(0, lastColon);
                portPart = token.Substring(lastColon + 1);
            }
            else if (token.Count(c => c == ':') > 1) // raw IPv6 without port
            {
                addressPart = token;
            }
        }

        if (portPart != null)
        {
            if (!ushort.TryParse(portPart, out _))
                return false;
        }

        if (ipV6InBrackets && addressPart.Equals(Localhost, StringComparison.OrdinalIgnoreCase))
        {
            ipAddressType = ItemEnum.IpV6;
            ipAddress = IPAddress.Parse(IpV6Localhost);
            return true;
        }

        if (addressPart.Equals(Localhost, StringComparison.OrdinalIgnoreCase))
        {
            ipAddressType = ItemEnum.IpV4;
            ipAddress = IPAddress.Parse(IpV4Localhost);
            return true;
        }

        if (IPAddress.TryParse(addressPart, out IPAddress? ipAddr))
        {
            if (ipAddr.AddressFamily == AddressFamily.InterNetwork && addressPart.Count(ch => ch == '.') == 3)
            {
                ipAddressType = ItemEnum.IpV4;
                ipAddress = ipAddr;
                return true;
            }

            if (ipAddr.AddressFamily == AddressFamily.InterNetworkV6)
            {
                ipAddressType = ItemEnum.IpV6;
                ipAddress = ipAddr;
                return true;
            }
        }
        
        return false;
    }
}
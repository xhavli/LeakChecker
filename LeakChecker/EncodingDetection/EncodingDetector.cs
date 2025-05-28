using System.Runtime.InteropServices;
using System.Text;
using LeakChecker.Utilities;

namespace LeakChecker.EncodingDetection;

public class EncodingDetector
{
    public static void VerifySupportedEncodings()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var mappedEncodings = EncodingMapper.EncodingMap.Values;
        var supportedEncodings = Encoding.GetEncodings().Select(ei => ei.Name).ToList();
        var unsupportedEncodings = mappedEncodings.Except(supportedEncodings).ToList();
        if (unsupportedEncodings.Any())
        {
            Logger.LogWarning("List of encodings mapped from charset-normalizer but not supported in C# on your machine");
            PrintMachineInfo();
            foreach (var unsupportedEncoding in unsupportedEncodings)
            {
                Logger.LogWarning($"Encoding {unsupportedEncoding} is not supported on current machine");
            }
        }
    }

    private static void PrintMachineInfo()
    {
        Logger.LogInfo("OS Platform: " + RuntimeInformation.OSDescription);
        Logger.LogInfo("OS Architecture: " + RuntimeInformation.OSArchitecture);
        Logger.LogInfo("Framework: " + RuntimeInformation.FrameworkDescription);
        Logger.LogInfo("Environment.OSVersion: " + Environment.OSVersion);
    }
    
    // Source https://learn.microsoft.com/en-us/dotnet/api/system.text.encodinginfo.codepage?view=net-10.0
    public static void PrintSupportedEncodings()
    {
        Logger.LogInfo("List of supported encodings after provide " +
                          "Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);");
        // Print the header.
        Console.Write( "Info.CodePage      " );
        Console.Write( "Info.Name                    " );
        Console.Write( "Info.DisplayName" );
        Console.WriteLine();

        // Display the EncodingInfo names for every encoding, and compare with the equivalent Encoding names.
        var sortedEncodings = Encoding.GetEncodings()
            .OrderBy(ei => ei.Name);
        
        foreach( EncodingInfo ei in sortedEncodings)  {
            Encoding e = ei.GetEncoding();

            Console.Write( "{0,-15}", ei.CodePage );
            Console.Write(ei.CodePage == e.CodePage ? "    " : "*** ");

            Console.Write( "{0,-25}", ei.Name );
            Console.Write(ei.CodePage == e.CodePage ? "    " : "*** ");

            Console.Write( "{0,-25}", ei.DisplayName );
            Console.Write(ei.CodePage == e.CodePage ? "    " : "*** ");

            Console.WriteLine();
        }
    }
}
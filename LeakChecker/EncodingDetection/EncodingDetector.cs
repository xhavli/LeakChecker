using System.Runtime.InteropServices;
using System.Text;
using LeakChecker.Utilities;
using UtfUnknown;

namespace LeakChecker.EncodingDetection;

public class EncodingDetector
{
    public static async Task<string> DetectEncoding(string filePath)
    {
        await using Stream fileStream = File.OpenRead(filePath);
        var result = CharsetDetector.DetectFromStream(fileStream, long.MaxValue);
        
        if (result.Detected == null)
        {
            Logger.LogError($"Failed to detect encoding from file {filePath}");
            return "Unknown";
        }
        
        if (result.Detected.Confidence < 1)
        {
            Logger.LogWarning($"Detected encoding [{result.Detected.EncodingName}] with confidence " +
                           $"[{result.Detected.Confidence}] for file {filePath}.");
        }
        
        return result.Detected.EncodingName;
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
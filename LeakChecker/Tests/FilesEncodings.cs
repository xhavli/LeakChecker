namespace LeakChecker.Tests;

public class FilesEncodings
{
    // [KEY] Path, [VALUE] Encoding,    Size, Note
    public static readonly Dictionary<string, string> FilesEncodingsDictionary = new()
    {
        [@"D:Bc\Collection #2_New combo cloud_General Splits Collection_АП42.txt_1001"] = "ascii",  // 0.02MB - Everything in one line
        [@"D:Bc\2444051_2444569"] = "utf-8",    // 0.1MB
        [@"D:Bc\7_unparsedRecords"] = "utf-8",  // 0.5MB - Might br binary
        [@"D:Bc\1353316_1358895"] = "windows-1252",   // 1.3MB
        [@"D:Bc\100.txt"] = "utf-8",            // 2MB
        [@"D:Bc\0"] = "utf-8",                  // 5MB - Weird symbols or zeroes and hyphens in the start of the line
        [@"D:Bc\300к phonepass.txt"] = "ascii", // 6MB
        [@"D:Bc\Czech Republic.txt"] = "utf-8", // 141MB
        [@"D:Bc\000webhost.com.txt"] = "utf-8", // 850MB
        [@"D:Bc\sha2.txt"] = "ibm852",          // 1.3GB
        [@"D:Bc\bf_1.txt"] = "ascii",           // 2GB
        [@"D:Bc\bigDB"] = "utf-8",              // 4GB - SCP terminated in the middle. Original file have More than 80GB
    };
}
namespace LeakChecker.Tests;

public static class FilesDelimiters
{
    // [KEY] Path, [VALUE] Delimiter        // Size - Note - content
    public static readonly Dictionary<string, string> FilesEncodingsDictionary = new()
    {
        [@"D:Bc\Collection #2_New combo cloud_General Splits Collection_АП42.txt_1001"] = ":",  // 0.02MB - Everything in one line - raw - emails
        [@"D:Bc\2444051_2444569"] = ",",    // 0.1MB - SQL - Facebook
        [@"D:Bc\7_unparsedRecords"] = ":",  // 0.5MB - Might be binary - cant detect correct encoding
        [@"D:Bc\1353316_1358895"] = ",",    // 1.3MB - SQL - CS ban history
        [@"D:Bc\100.txt"] = ":",            // 2MB - nickname:passwd
        [@"D:Bc\0"] = ":",                  // 5MB - Weird symbols or zeroes and hyphens in the start of the line - email:passwd
        [@"D:Bc\300к phonepass.txt"] = ":", // 6MB - phone:passwd
        [@"D:Bc\Czech Republic.txt"] = ":", // 141MB - Facebook public info
        [@"D:Bc\000webhost.com.txt"] = ":", // 850MB - nickname:email:ip:passwd
        [@"D:Bc\sha2.txt"] = ":",           // 1.3GB - email:hash
        [@"D:Bc\bf_1.txt"] = ":",           // 2GB - email:hash
        [@"D:Bc\bigDB"] = ":",              // 4GB - SCP terminated in the middle. Original file have More than 80GB - email:passwd
    };
}
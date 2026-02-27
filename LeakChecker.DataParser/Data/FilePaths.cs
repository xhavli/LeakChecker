namespace LeakChecker.DataParser.Data;

public static class FilePaths
{
    // SubjectFilePath  // Size - Note - Content
    public static readonly string[] Original =
    [
        @"D:Bc\Collection #2_New combo cloud_General Splits Collection_АП42.txt_1001",  // 0.02MB - Everything in one line - raw - emails
        @"D:Bc\2444051_2444569",    // 0.1MB - SQL - Facebook
        @"D:Bc\1353316_1358895",    // 1.3MB - SQL - CS ban history
        @"D:Bc\100.txt",            // 2MB - nickname:passwd
        @"D:Bc\0",                  // 5MB - Weird symbols or zeroes and hyphens in the start of the line - email:passwd
        @"D:Bc\300к phonepass.txt", // 6MB - phone:passwd
        @"D:Bc\Czech Republic.txt", // 141MB - Facebook public info
        @"D:Bc\000webhost.com.txt", // 850MB - 15,3 million lines - nickname:email:ip:passwd
        @"D:Bc\sha2.txt",           // 1.3GB - email:hash
        @"D:Bc\bf_1.txt",           // 2GB - email:hash
        @"D:Bc\bigDB",              // 4GB - SCP terminated in the middle. Original file have More than 80GB - email:passwd
    ];
    
    // SubjectFilePath  // Size - Note - Content
    public static readonly string[] Utf8 =
    [
        @"D:Bc\tmp\Utf8\Collection #2_New combo cloud_General Splits Collection_АП42.txt_1001.txt",  // 0.02MB - Everything in one line - raw - emails
        @"D:Bc\tmp\Utf8\2444051_2444569.txt",    // 0.1MB - SQL - Facebook
        // // // // @"D:Bc\tmp\7_unparsedRecords.txt,  // 0.5MB - Might be binary - cant detect correct encoding it was reencoded multiple times
        @"D:Bc\tmp\Utf8\7_unparsedRecords_fixed_utf8.txt",  // 0.5MB - ChatGPT fixed encoding - email:email:passwd
        @"D:Bc\tmp\Utf8\1353316_1358895.txt",    // 1.3MB - SQL - CS ban history
        @"D:Bc\tmp\Utf8\100.txt.txt",            // 2MB - nickname:passwd
        @"D:Bc\tmp\Utf8\0.txt",                  // 5MB - Weird symbols or zeroes and hyphens in the start of the line - email:passwd
        @"D:Bc\tmp\Utf8\300к phonepass.txt.txt", // 6MB - phone:passwd
        @"D:Bc\tmp\Utf8\Czech Republic.txt.txt", // 141MB - Facebook public info
        @"D:Bc\tmp\Utf8\000webhost.com.txt.txt", // 850MB - 15,3 million lines - nickname:email:ip:passwd
        @"D:Bc\tmp\Utf8\sha2.txt.txt",           // 1.3GB - email:hash
        @"D:Bc\tmp\Utf8\bf_1.txt.txt",           // 2GB - email:hash
        @"D:Bc\tmp\Utf8\bigDB.txt", // 4GB - SCP terminated in the middle. Original file have More than 80GB - email:passwd
        // @"D:Bc\tmp\Excel2Sheets.xlsx",      // Sample Excel with 2 sheets, ip mail pass and mail ip pass
    ];
    
    public static readonly string[] Test =
    [
        "C:/Users/ahavl/RiderProjects/LeakChecker/LeakChecker.DataParser.Tests/Data/FormatMixed/SqlThenCsv_Messy.txt",
    ];
}
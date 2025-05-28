namespace LeakChecker.EncodingDetection;

public static class EncodingMapper
{
    // source https://www.iana.org/assignments/character-sets/character-sets.xhtml
    // source https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-text-encoding
    // source https://learn.microsoft.com/en-us/dotnet/api/system.text.encoding.getencodings?view=net-10.0
    // source Wikipedia for Mac encodings and some edge cases
    
    // [KEY] charset-normalized encoding, [VALUE] C# encoding
    public static readonly Dictionary<string, string> EncodingMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // ASCII
        ["ascii"] = "us-ascii",
        
        // Unicode Encodings
        // ["utf7"] = "utf-7",  // TODO C# Not supported
        ["utf8"] = "utf-8",
        ["utf16"] = "utf-16",
        ["utf16le"] = "utf-16",
        ["utf16be"] = "utf-16BE",
        ["utf32"] = "utf-32",
        ["utf32le"] = "utf-32",
        ["utf32be"] = "utf-32BE",

        // ISO 8859 Family
        ["latin1"] = "iso-8859-1",
        // This is not possible output of charset-normalizer
        // ["latin2"] = "iso-8859-2",
        // ["latin3"] = "iso-8859-3",
        // ["latin4"] = "iso-8859-4",
        // ["latin5"] = "iso-8859-9",
        // ["latin9"] = "iso-8859-15",
        // ["latin10"] = "iso-8859-16",

        ["iso88591"] = "iso-8859-1",
        ["iso88592"] = "iso-8859-2",
        ["iso88593"] = "iso-8859-3",
        ["iso88594"] = "iso-8859-4",
        ["iso88595"] = "iso-8859-5",
        ["iso88596"] = "iso-8859-6",
        ["iso88597"] = "iso-8859-7",
        ["iso88598"] = "iso-8859-8",
        ["iso88599"] = "iso-8859-9",
        ["iso885913"] = "iso-8859-13",
        // ["iso885914"] = "iso-8859-14",   // TODO C# Not supported
        ["iso885915"] = "iso-8859-15",
        // ["iso885916"] = "iso-8859-16",   // TODO C# Not supported

        // Windows Code Pages
        ["windows1250"] = "windows-1250",
        ["windows1251"] = "windows-1251",
        ["windows1252"] = "windows-1252",
        ["windows1253"] = "windows-1253",
        ["windows1254"] = "windows-1254",
        ["windows1255"] = "windows-1255",
        ["windows1256"] = "windows-1256",
        ["windows1257"] = "windows-1257",
        ["windows1258"] = "windows-1258",

        // DOS / IBM Code Pages
        ["cp037"] = "IBM037",
        ["cp273"] = "IBM273",
        ["cp424"] = "IBM424",
        ["cp437"] = "IBM437",
        ["cp500"] = "IBM500",
        ["cp775"] = "ibm775",
        ["cp850"] = "ibm850",
        ["cp852"] = "ibm852",
        ["cp855"] = "IBM855",
        ["cp857"] = "ibm857",
        ["cp858"] = "IBM00858",
        ["cp860"] = "IBM860",
        ["cp861"] = "ibm861",
        ["cp862"] = "DOS-862",
        ["cp863"] = "IBM863",
        ["cp864"] = "IBM864",
        ["cp865"] = "IBM865",
        ["cp866"] = "cp866",
        ["cp869"] = "ibm869",
        ["cp875"] = "cp875",
        ["cp1025"] = "cp1025",
        ["cp1026"] = "IBM1026",
        ["cp1140"] = "IBM01140",
        ["cp1250"] = "windows-1250",
        ["cp1251"] = "windows-1251",
        ["cp1252"] = "windows-1252",
        ["cp1253"] = "windows-1253",
        ["cp1254"] = "windows-1254",
        ["cp1255"] = "windows-1255",
        ["cp1256"] = "windows-1256",
        ["cp1257"] = "windows-1257",
        ["cp1258"] = "windows-1258",

        // Cyrillic Encodings
        ["koi8r"] = "koi8-r",
        ["koi8u"] = "koi8-u",

        // Mac Encodings
        ["maccyrillic"] = "x-mac-cyrillic",
        ["macgreek"] = "x-mac-greek",
        ["maciceland"] = "x-mac-icelandic",
        ["maclatin2"] = "x-mac-ce",
        ["macroman"] = "macintosh",
        ["macturkish"] = "x-mac-turkish",

        // East Asian Encodings
        ["big5"] = "big5",
        ["big5hkscs"] = "big5", // Partially supported
        ["gb2312"] = "gb2312",
        // ["gb18030"] = "GB18030", // TODO C# Not supported
        // ["gbk"] = "x-cp936", // Partially supported  // TODO C# Not supported
        ["eucjp"] = "EUC-JP",
        // ["iso2022jp"] = "iso-2022-jp",   // TODO C# Not supported
        ["cp932"] = "shift_jis",
        ["shiftjis"] = "shift_jis",
        ["cp950"] = "big5",
        
        // Japanese Unicode extensions
        ["eucjis2004"] = "EUC-JP", // Partially supported
        ["eucjisx0213"] = "EUC-JP", // Partially supported
        ["shiftjis2004"] = "shift_jis", // Partially supported
        ["shiftjisx0213"] = "shift_jis", // Partially supported
        // ["iso2022jp1"] = "iso-2022-jp", // Partially supported       // TODO C# Not supported
        // ["iso2022jp2"] = "iso-2022-jp", // Partially supported       // TODO C# Not supported
        // ["iso2022jp2004"] = "iso-2022-jp", // Partially supported    // TODO C# Not supported
        // ["iso2022jp3"] = "iso-2022-jp", // Partially supported       // TODO C# Not supported
        // ["iso2022jpext"] = "iso-2022-jp", // Partially supported     // TODO C# Not supported
        
        // Korean
        // ["euckr"] = "euc-kr",    // TODO C# Not supported
        // ["cp949"] = "ks_c_5601-1987",   // TODO Not valid
        ["johab"] = "Johab",

        // Thai
        ["tis620"] = "windows-874",
        ["cp874"] = "windows-874"
    };
}
# LeakChecker Changelog

Author: Adam Havlík

## Changes

### Encoding

- `27.5.2025` - EncodingMap  
  Map between pythons [charset-normalizer](https://pypi.org/project/charset-normalizer/) and .NET supported encodings been created and verified to [IANA](https://www.iana.org/assignments/character-sets/character-sets.xhtml), [Learn.Microsoft](https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-text-encoding), Wikipedia...
- `17.6.2025` - Encoding detection convert to .NET NuGet [UtfUnknown](https://github.com/CharsetDetector/UTF-unknown)  
  Due to performance issue with pythons charset-normalizer.  
  [NOTE] UtfUnknown can read stream and is pure C# tool based on Mozilla’s Universal Charset Detector used in Firefox. According to some officials charset-normalizer is more accurate but i find it slower and have big performance issue in my use-case because it loads all file to a memory.
- `30.7.2025` - Encoding detection done precisely  
  Detection of concatenated files with various encodings and its segments.

### Content

- `15.7.2025` - Pythons FastAPI  
  Created as localhost service for easy integration of AI recognition models.
- `15.7.2025` - Automated recognition  
  [Microsoft Presidio](https://microsoft.github.io/presidio/) (Regex + NER) base configuration and model was tested on raw lines with unsatisfied results as NER recognizers due to large variety of data without context, cant handle as NLP.
- `16.7.2025` - Automated recognition  
  [Microsoft Presidio](https://microsoft.github.io/presidio/) Tested as orchestrator with [obi/deid_roberta_i2b2](https://huggingface.co/obi/deid_roberta_i2b2), [spacy/en_core_web_lg](https://huggingface.co/spacy/en_core_web_lg) and [s2w-ai/CyBERTuned-SecurityLLM](https://huggingface.co/s2w-ai/CyBERTuned-SecurityLLM) on raw lines with unsatisfied results as NER recognizers with same reason.
- `17.7.2025` - Zero-shot model  
  [facebook/bart-large-mnli](https://huggingface.co/facebook/bart-large-mnli) was tested with almost good results. There is need to know delimiters in data, process it by commonly know regexes and at the end tokens which are still unknown send to Zero-shot recognizer for categorization.  
  [NOTE] This model is 10x faster on Nvidia 1650Ti than on Intel 10300H. There is need to reinstall python torch from CPU to GPU.
- `3.8.2025` - Content detections  
  - C# - [MailAddress.TryCreate()](https://learn.microsoft.com/en-us/dotnet/api/system.net.mail.mailaddress.trycreate?view=net-10.0) is good enough.
  - C# - [DateTime.TryParse()](https://learn.microsoft.com/en-us/dotnet/api/system.datetime.tryparse?view=net-10.0) is good for now.  
    [NOTE] It cant parse 4/15/2018 12:00:00 AM from Facebook leak.
  - C# - [IPAddress.TryParse()](https://learn.microsoft.com/en-us/dotnet/api/system.net.ipaddress.tryparse?view=net-10.0) is good for now, it can parse wide scale of formats  
    [NOTE] It can also parse decimal or hexadecimal format. For example US phone number in local format 4085551234 can be misinterpreted as 243.132.144.130. Then we need additional validation for IPv4.
    ```csharp
    if (ipAddress.AddressFamily == AddressFamily.InterNetwork &&
        token.Count(ch => ch == '.') == 3)
    ```
    IPv6 was not properly tested. This can also parse IpV4 mapped to IpV6 - 192.168.1.1 = ::ffff:192.168.1.1
 = ::ffff:c0a8:0101.
  - NuGet - [PhoneNumbers](https://github.com/google/libphonenumber) for phone number validation and optional localization.
- `4.8.2025` - Hash identification  
  - Hash Identification applications were manually tested with dataset from [onlinehashcrack](https://www.onlinehashcrack.com/hash-acceptance.php).
  - [www.hashes.com](https://hashes.com/en/tools/hash_identifier) - Tools - Hash Identifier do proper validation and return most successful results, have demo its web application with well documented [api](https://hashes.com/en/docs). Chosen solution.  
    [NOTE] It can misinterpret 2), 5)... as Base64 encoded text of plaintext ''. Then we need additional validation.
    ```csharp
    if (Base64.IsValid(hash))
    ```
  - [HAITI](https://github.com/noraj/haiti) - Wide scale of supported hash types (600+) but do not validation, match everything including mobile number, don't have a demo.
  - [CyberChef](https://github.com/gchq/CyberChef) - Do validation, have demo, do not support hashes with salt.
  - [Name That Hash](https://github.com/bee-san/Name-That-Hash) - Wide scale of supported hash types (300+), have demo, do some validation but not 100% correct, most of unknown hash fall in "default" BigCrypt hash type.
- `18.8.2025` - Automated recognition of Name, Location and Organization  
  - [Microsoft Presidio](https://microsoft.github.io/presidio/) with [flair/ner-english-large](https://huggingface.co/flair/ner-english-large) model integrated after google close issue with sentencepiece used by flair.
- `11.9.2025` - Automated recognition from text where item may contain delimiter 
  - NuGet - [Microsoft.Recognizers.Text.DateTime](https://github.com/microsoft/Recognizers-Text) integrated for recognition of wide scale of TimeStamps in text and conversion to C# DateTime format.  
    [NOTE] It can detect and convert 4/15/2018 12:00:00 AM from Facebook leak and also natural language like first of October 2018 15:32:18. It also detect a time range what we dont want to.
  - NuGet - [Microsoft.Recognizers.Text.Sequence](https://github.com/microsoft/Recognizers-Text) for recognition and conversion to C# structures  
    - Email + [MailAddress.TryCreate()](https://learn.microsoft.com/en-us/dotnet/api/system.net.mail.mailaddress.trycreate?view=net-10.0) for extra validation
    - Guid
    - Url
- `13.9.2025` - Validation of Gender and Marital Status  
  - Validation done with some hardcoded values
- `21.9.2025` - HeuristicAnalyzer  
  - Heuristic analyzer created with some helper methods. Code readability degraded due to performance. Not use a List for HeuristicRecords. First shot of Pattern or schema dramatically boost performance by decade or two
- `22.9.2025` - MAC Address parser  
  - C# - [PhysicalAddress.TryParse()](https://learn.microsoft.com/en-us/dotnet/api/system.net.networkinformation.physicaladdress.tryparse?view=net-10.0#system-net-networkinformation-physicaladdress-tryparse(system-string-system-net-networkinformation-physicaladdress@)) mac address validation added  
    [NOTE] SHA1 hash 08137e51edc9d3bf54fd051e3d91bd471c93a240 can be misinterpreted as Mac address. Then we need additional validation.
    ```csharp
    if (mac.GetAddressBytes().Length == 6)
    ```
- `28.9.2025` - TimeStamp parser
  - Unix seconds since epoch (1970-01-01 UTC) - 1284982477 is 2010-09-20 18:34:37 UTC
  - Unix milliseconds since epoch (1970-01-01 UTC)
  - Windows FileTime - 100-nanosecond intervals since 1601-01-01 00:00:00 UTC
  - .Net ticks - 100-nanoseconds = 1 tic, tics since 0001-01-01 00:00:00 UTC
  - Excel serial date - days since 1899-12-30  
  [NOTE] As it could be almost every bigger number, we need additional validation and Excel serials might be removed in future according to possible mismatch with one of Ids.
  ```csharp
  DateTime minDate = new DateTime(2000, 1, 1); // Might be adjusted as needed. Counter Strike and Mario cart released in 2000
  DateTime maxDate = DateTime.UtcNow;

  bool IsInRange(DateTime dt) =>
    dt >= minDate && dt <= maxDate;
  ```
  Then valid numeric ranges (approx as of 2025-09-28)
  ```bash
  Unix seconds: 946684800 .. 1759075200
  Unix ms: 946684800000 .. 1759075200000
  FILETIME: 125911584000000000 .. 133805000000000000
  .NET ticks: 630822816000000000 .. 638644800000000000
  Excel serials: 36526 .. 45849
  ```

### Utilities

- `30.7.2025` - Added some logging tools to log processing details and statistics to log file.
- `6.8.2025` - Logging utilities improvement.
- `19.8.2025` - Detailed file processing logging added
- `13.9.2025` - Detailed execution logging added
- `19.9.2025` - StringExtension added for custom and performance trimming of quoted text `content`` / 'content' / "content" or SQL line (content),

## TODO

- Search for delimiters properly [python CSV sniffer](https://docs.python.org/3/library/csv.html#csv.Sniffer) cant parse files where lot of attributes are missing as Facebook leak.
- Create a pattern of content from given format.
- Create a tmp files for encoding and another one for parsed content.
- Detect Username and plaintext Password.
- Decide if IpAddress parsing of cross mapped addresses are feature or limitation. Can be done by [Microsoft.Recognizers.Text.Sequence](https://github.com/microsoft/Recognizers-Text) automated recognition as Email, Guid and Urls are.
- Test Excel serials in TimeStampParser and decide if its feature or limitation.
- Detect other content.
- Do IPv6 validation which may be done by regex from [Vladimir Vesely - IPK2024-06-IPv6](https://moodle.vut.cz/pluginfile.php/823898/mod_folder/content/0/IPK2023-24L-09-IPv6.pdf)

```regexp
`/^\s*((([0-9A-Fa-f]{1,4}:){7}(([0-9A-Fa-f]{1,4})|:))|(([0-9A-Fa-f]{1,4}:){6}(:|((25[0-5]|2[0-4]
\d|[01]?\d{1,2})(\.(25[0-5]|2[0-4]\d|[01]?\d{1,2})){3})|(:[0-9A-Fa-f]{1,4})))|(([0-9A-Fa-f]
{1,4}:){5}((:((25[0-5]|2[0-4]\d|[01]?\d{1,2})(\.(25[0-5]|2[0-4]\d|[01]?\d{1,2})){3})?)|((:[0-9A-Fa-f]
{1,4}){1,2})))|(([0-9A-Fa-f]{1,4}:){4}(:[0-9A-Fa-f]{1,4}){0,1}((:((25[0-5]|2[0-4]\d|[01]?\d{1,2})(\.(25[0-5]
|2[0-4]\d|[01]?\d{1,2})){3})?)|((:[0-9A-Fa-f]{1,4}){1,2})))|(([0-9A-Fa-f]{1,4}:){3}(:[0-9A-Fa-f]
{1,4}){0,2}((:((25[0-5]|2[0-4]\d|[01]?\d{1,2})(\.(25[0-5]|2[0-4]\d|[01]?\d{1,2})){3})?)|((:[0-9A-Fa-f]
{1,4}){1,2})))|(([0-9A-Fa-f]{1,4}:){2}(:[0-9A-Fa-f]{1,4}){0,3}((:((25[0-5]|2[0-4]\d|[01]?\d{1,2})(\.(25[0-5]
|2[0-4]\d|[01]?\d{1,2})){3})?)|((:[0-9A-Fa-f]{1,4}){1,2})))|(([0-9A-Fa-f]{1,4}:)(:[0-9A-Fa-f]
{1,4}){0,4}((:((25[0-5]|2[0-4]\d|[01]?\d{1,2})(\.(25[0-5]|2[0-4]\d|[01]?\d{1,2})){3})?)|((:[0-9A-Fa-f]
{1,4}){1,2})))|(:(:[0-9A-Fa-f]{1,4}){0,5}((:((25[0-5]|2[0-4]\d|[01]?\d{1,2})(\.(25[0-5]|2[0-4]
\d|[01]?\d{1,2})){3})?)|((:[0-9A-Fa-f]{1,4}){1,2})))|(((25[0-5]|2[0-4]\d|[01]?\d{1,2})(\.(25[0-5]|2[0-4]
\d|[01]?\d{1,2})){3})))(%.+)?\s*$/`
```

- Make tests for: Encoding, Format and Content
- Test performance and profiling
- Implement DI
- Implement proper AppSettings

## Notes

- Performance optimization  
  - `string[]` vs. `List<string>`  
    - `string[]` have better performance, cant do list operations
    ```csharp
        List<string> tokens = line.Split(delimiter).ToList();
        for (int i = 0; i < tokens.Count; i++)
        {
            ...
        }
    ```
    
    - `List<string>` worse performance, if specially need list operations (Add, Sort...)
    ```csharp
        List<string> tokens = line.Split(delimiter).ToList();
        for (int i = 0; i < tokens.Count; i++)
        {
            ...
        }
    ```
    
  - `StringComparer.OrdinalIgnoreCase` vs. `StringComparer.InvariantCultureIgnoreCase`
    - `StringComparison.OrdinalIgnoreCase` have better performance, binary comparison, predictable
    ```csharp
      tokens[] tokens = line.Split(delimiter).ToList();
      foreach (token in tokens)
      { 
          token.Contains("value", StringComparison.OrdinalIgnoreCase);
      }
    ```

  - `StringComparison.InvariantCultureIgnoreCase` have worse performance, can match straße and strasse, +0,0001ms slower
    ```csharp
      tokens[] tokens = line.Split(delimiter).ToList();
      foreach (token in tokens)
      { 
          token.Contains("value", StringComparison.OrdinalIgnoreCase);
      }
    ```
    
  - Every string comparison and replacement or removing from line as `ReadOnly<char>` span
    - Other comparison and removing create new string which takes too loong time

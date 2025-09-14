# LeakChecker Changelog

Author: Adam Havlík

## Changes

### Encoding

- `27.5.2025` - EncodingMap between pythons [charset-normalizer](https://pypi.org/project/charset-normalizer/) and .NET supported encodings been verified to [IANA](https://www.iana.org/assignments/character-sets/character-sets.xhtml), [Learn.Microsoft](https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-text-encoding), Wikipedia...
- `17.6.2025` - Encoding detection convert to .NET NuGet [UtfUnknown](https://github.com/CharsetDetector/UTF-unknown) due to performance issue with pythons charset-normalizer.  
  [NOTE] UtfUnknown can read stream and is pure C# tool based on Mozilla’s Universal Charset Detector used in Firefox. According to some officials charset-normalizer is more accurate but i find it slower and have big performance issue in my use-case because it loads all file to a memory.
- `30.7.2025` - Encoding detection done precisely to detect concatenated files with various encodings and its segments.

### Content

- `15.7.2025` - Pythons FastAPI was created as localhost service for easy integration of AI recognition models.
- `15.7.2025` - [Microsoft Presidio](https://microsoft.github.io/presidio/) with (Regex + NER) was tested on raw lines with unsatisfied results as NER recognizers due to large variety of data without context, cant handle as NLP.
- `16.7.2025` - [Microsoft Presidio](https://microsoft.github.io/presidio/) as orchestrator tested with [obi/deid_roberta_i2b2](https://huggingface.co/obi/deid_roberta_i2b2) and [s2w-ai/CyBERTuned-SecurityLLM](https://huggingface.co/s2w-ai/CyBERTuned-SecurityLLM) on raw lines with unsatisfied results as NER recognizers with same reason.
- `17.7.2025` - Zero-shot model [facebook/bart-large-mnli](https://huggingface.co/facebook/bart-large-mnli) was tested with satisfying results. There is need to know delimiters in data, process it by commonly know regexes and at the end tokens which are still unknown send to Zero-shot recognizer for categorization.  
  [NOTE] This model is 10x faster on Nvidia 1650Ti than on Intel 10300H. There is need to reinstall python torch from CPU to GPU.
- `3.8.2025` - Content detections  
  - C# - [MailAddress.TryCreate()](https://learn.microsoft.com/en-us/dotnet/api/system.net.mail.mailaddress.trycreate?view=net-10.0) is good enough.
  - C# - [DateTime.TryParse()](https://learn.microsoft.com/en-us/dotnet/api/system.datetime.tryparse?view=net-10.0) is good for now.  
    [NOTE] It cant parse 4/15/2018 12:00:00 AM from Facebook leak.
  - C# - [IPAddress.TryParse()](https://learn.microsoft.com/en-us/dotnet/api/system.net.ipaddress.tryparse?view=net-10.0) is good for now, it can parse wide scale of formats  
    [NOTE] It can also parse decimal or hexadecimal format. For example US phone number in local format 4085551234 can be misinterpreted as 243.132.144.130. Then we need additional validation for IPv4 if it contains 3x '.'. IPv6 was not properly tested. This can also parse IpV4 mapped to IpV6 - 192.168.1.1 = ::ffff:192.168.1.1
 = ::ffff:c0a8:0101.
  - NuGet - [PhoneNumbers](https://github.com/google/libphonenumber) for phone number validation and optional localization.
- `4.8.2025` - Hash identification  
  - Hash Identification applications were manually tested with dataset from [onlinehashcrack](https://www.onlinehashcrack.com/hash-acceptance.php).
  - [www.hashes.com](https://hashes.com/en/tools/hash_identifier) - Tools - Hash Identifier do proper validation and return most successful results, have demo its web application with well documented [api](https://hashes.com/en/docs). Chosen solution.
  - [HAITI](https://github.com/noraj/haiti) - Wide scale of supported hash types (600+) but do not validation, match everything including mobile number, don't have a demo.
  - [CyberChef](https://github.com/gchq/CyberChef) - Do validation, have demo, do not support hashes with salt.
  - [Name That Hash](https://github.com/bee-san/Name-That-Hash) - Wide scale of supported hash types (300+), have demo, do some validation but not 100% correct, most of unknown hash fall in "default" BigCrypt hash type.
- `18.8.2025` - Automated recognition of Name, Location and Organziation  
  - [Microsoft Presidio](https://microsoft.github.io/presidio/) with [flair/ner-english-large](https://huggingface.co/flair/ner-english-large) model integrated after google close issue with sentencepiece used by flair.
- `11.9.2025` - Automated recognition from text where item may contain delimiter 
  - NuGet - [Microsoft.Recognizers.Text.DateTime](https://github.com/microsoft/Recognizers-Text) integrated for recognition of wide scale of TimeStamps in text and conversion to C# DateTime format.  
    [NOTE] It can detect and convert 4/15/2018 12:00:00 AM from Facebook leak and also natural language like first of October 2018 15:32:18. 
  - NuGet - [Microsoft.Recognizers.Text.Sequence](https://github.com/microsoft/Recognizers-Text) for recognition and conversion to C# structures  
    - Email
    - Guid
    - Url
- `13.9.2025` - Validation of hardcoded Gender and Marital Status

### Utilities

- `30.7.2025` - Added some logging tools to log processing details and statistics to log file.
- `6.8.2025` - Logging utilities improvement.
- `19.8.2025` - Detailed file processing logging added
- `13.9.2025` - Detailed execution logging added

## TODO

- Search for delimiters properly [python CSV sniffer](https://docs.python.org/3/library/csv.html#csv.Sniffer) cant parse files where lot of attributes are missing as Facebook leak.
- Create a pattern of content from given format.
- Detect Username and plaintext Password.
- Decide if IpAddress parsing of cross mapped addresses are feature or limitation.
- Detect other content.
- Do IPv6 validation which may be done by regex from [Vladimir Vesely - IPK2024-06-IPv6](https://moodle.vut.cz/pluginfile.php/823898/mod_folder/content/0/IPK2023-24L-09-IPv6.pdf)

```bash
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

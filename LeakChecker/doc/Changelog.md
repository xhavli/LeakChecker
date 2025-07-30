# LeakChecker Changelog

Author: Adam Havlík

## Changes

### Encoding

- `27.5.2025` - EncodingMap between pythons [charset-normalizer](https://pypi.org/project/charset-normalizer/) and .NET supported encodings been verified to [IANA](https://www.iana.org/assignments/character-sets/character-sets.xhtml), [Learn.Microsoft](https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-text-encoding), Wikipedia
- `17.6.2025` - Encoding detection convert to .NET NuGet [UtfUnknown](https://github.com/CharsetDetector/UTF-unknown) due to performance issue with pythons charset-normalizer.  
  [NOTE] UtfUnknown can read stream and is pure C# tool based on Mozilla’s Universal Charset Detector used in Firefox. According to some officials charset-normalizer is more accurate but i find it slower and have big performance issue in my use-case because it loads all file to a memory
- `30.7.2025` - Encoding detection done precisely to detect concatenated files with various encodings and its segments

### Content

- `15.7.2025` - Pythons FastAPI was created as localhost service for easy integration of AI recognition models
- `15.7.2025` - [Microsoft Presidio](https://microsoft.github.io/presidio/) (Regex + NER) was tested on raw lines with unsatisfied results as NER recognizers due to large variety of data without context, cant handle as NLP
- `16.7.2025` - [Microsoft Presidio](https://microsoft.github.io/presidio/) as orchestrator tested with [obi/deid_roberta_i2b2](https://huggingface.co/obi/deid_roberta_i2b2) and [s2w-ai/CyBERTuned-SecurityLLM](https://huggingface.co/s2w-ai/CyBERTuned-SecurityLLM) on raw lines with unsatisfied results as NER recognizers with same reason
- `17.7.2025` - Zero-shot model [facebook/bart-large-mnli](https://huggingface.co/facebook/bart-large-mnli) was tested with satisfying results. There is need to know delimiters in data, process it by commonly know regexes and at the end tokens which are still unknown send to Zero-shot recognizer for categorization.  
  [NOTE] This model is 10x faster on Nvidia 1650Ti than on Intel 10300H. There is need to reinstall python torch from CPU to GPU.

### Utilities

- `30.7.2025` - Added some logging tools to log processing details and statistics to log file

## TODO

- Search for delimiters properly
- Process tokens from a single line
- Detect content
- Implement DI
- Implement proper AppSettings

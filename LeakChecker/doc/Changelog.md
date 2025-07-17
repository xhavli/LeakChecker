# Changelog

Author: Adam Havlík

This is changelog of LeakChecker tool

## Changes

- `27.5.2025` - EncodingMap verified to IANA, Learn.Microsoft, Wikipedia and also ChatGPT
- `28.5.2025` - Initial commit
- `17.6.2025` - Convert from pythons charset-normalizer to .net [UtfUnknown](https://github.com/CharsetDetector/UTF-unknown) encoding detector nuget
- `15.7.2025` - To architecture was added Python FastAPI for easy, fast and scalable communication between C# and AI  
- `15.7.2025` - Microsoft Presidio (Regex + NER) was tested with unsatisfied results as NER recognizers due to large variety of data without context, no NLP
- `16.7.2025` - [Microsoft Presidio](https://microsoft.github.io/presidio/) as orchestrator tested with [obi/deid_roberta_i2b2](https://huggingface.co/obi/deid_roberta_i2b2) and [s2w-ai/CyBERTuned-SecurityLLM](https://huggingface.co/s2w-ai/CyBERTuned-SecurityLLM) with unsatisfied results as NER recognizers
- `17.7.2025` - Zero-shot model [facebook/bart-large-mnli](https://huggingface.co/facebook/bart-large-mnli) was tested with satisfying results. There is need to know delimiters in data, process it by commonly know regexes and at the end tokens which are still unknown send to Zero-shot recognizer for categorization. This model is 10x faster on Nvidia 1650Ti than on Intel 10300H. There is need to reinstall python torch from CPU to GPU.

## TODO

- Detect concatenation border of different encodings in same file
- Test the accuracy of UtfUnknown
- Search for delimiters properly
- Process tokens from a single line
- Implement sample size limits because of C# byte[] array limit is 2GB to be able parse large files
- Implement proper parallelization
- Implement DI
- Implement proper AppSettings

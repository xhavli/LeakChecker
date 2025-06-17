# Changelog

Author: Adam Havlík

This is changelog of LeakChecker tool

## Changes

- `27.5.2025` - EncodingMap verified to IANA, Learn.Microsoft, Wikipedia and also ChatGPT
- `28.5.2025` - Initial commit
- `17.6.2025` - Convert from pythons charset-normalizer to .net UtfUnknown encoding detector nuget

## TODO

- Detect concatenation border of different encodings in same file
- Test the accuracy of UtfUnknown
- Implement sample size limits because of C# byte[] array limit is 2GB to be able parse large files
- Implement proper paralization
- Implement DI
- Implement propper AppSettings

# Changelog

Author: Adam Havlík

This is changelog of LeakChecker tool

## Changes

- `27.5.2025` - EncodingMap verified to IANA, Learn.Microsoft, Wikipedia and also ChatGPT
- `28.5.2025` - Initial commit

## TODO

- Divide files into smaller samples and test each other to avoid memory overflow
  - test the accuracy in context of smaller samples
- Implement sample size limits because of C# byte[] array limit is 2GB to be able parse large files
- Avoid naming conflict with EncodingDetector
- Implement DI
- Implement propper AppSettings

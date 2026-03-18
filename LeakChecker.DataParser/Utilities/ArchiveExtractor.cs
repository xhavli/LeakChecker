using Microsoft.CST.RecursiveExtractor;

namespace LeakChecker.DataParser.Utilities;

public static class ArchiveExtractor
{
    private static readonly char[] DirectorySeparators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];
    private static readonly string[] MultiPartArchiveExtensions =
    [
        ".tar.bz2", ".tar.gz", ".tar.lz", ".tar.lzma", ".tar.lzo", ".tar.xz", ".tar.z", ".tar.zst"
    ];
    
    public static async Task<IEnumerable<string>> ExtractArchives(IEnumerable<string> inputPaths, string extractionPath)
    {
        var extractor = new Extractor();
        List<string> outputPaths = new();
        
        string extractionRoot = Path.GetFullPath(extractionPath);
        if (!extractionRoot.EndsWith(Path.DirectorySeparatorChar))
            extractionRoot += Path.DirectorySeparatorChar;

        foreach (var path in inputPaths)
        {
            foreach (var entry in extractor.Extract(path))
            {
                if (entry.Parent == null)
                {
                    // Not an archive
                    outputPaths.Add(path);
                    continue;
                }
                
                string relativeArchivePath = GetRelativeArchivePath(path, entry.FullPath);
                string relativePath = NormalizeArchiveRelativePath(relativeArchivePath);    //TODO can overwrite a.zip/b.txt and a.tar/b.txt
                string dstPath = Path.GetFullPath(Path.Combine(extractionRoot, relativePath));

                if (!dstPath.StartsWith(extractionRoot, StringComparison.Ordinal))
                {
                    Console.WriteLine($"Potential Zip Slip. Archive entry resolves outside extraction root: '{entry.FullPath}'");
                    continue;
                }
                
                string? directory = Path.GetDirectoryName(dstPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                await using var outStream = File.Create(dstPath);
                await entry.Content.CopyToAsync(outStream);
                
                outputPaths.Add(dstPath);
            }
        }
        
        return outputPaths;
    }
    
    private static string GetRelativeArchivePath(string sourcePath, string entryFullPath)
    {
        var rootName = Path.GetFileName(sourcePath);

        var segments = entryFullPath.Split(DirectorySeparators, StringSplitOptions.RemoveEmptyEntries);

        var startIndex = Array.FindIndex(
            segments, s => string.Equals(s, rootName, StringComparison.OrdinalIgnoreCase));

        return startIndex < 0
            ? Path.GetFileName(entryFullPath)
            : Path.Combine(segments[startIndex..]);
    }
    
    private static string NormalizeArchiveRelativePath(string relativePath)
    {
        var parts = relativePath.Split(DirectorySeparators);

        for (int i = 0; i < parts.Length - 1; i++) // skip final file
        {
            parts[i] = StripArchiveExtension(parts[i]);
        }

        return Path.Combine(parts);
    }
    
    private static string StripArchiveExtension(string fileName)
    {
        string lower = fileName.ToLowerInvariant();

        foreach (var ext in MultiPartArchiveExtensions)
        {
            if (lower.EndsWith(ext))
                return fileName[..^ext.Length];
        }

        return Path.GetFileNameWithoutExtension(fileName);
    }
}
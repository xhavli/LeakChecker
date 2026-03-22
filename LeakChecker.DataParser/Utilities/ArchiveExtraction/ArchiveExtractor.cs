using Microsoft.CST.RecursiveExtractor;

namespace LeakChecker.DataParser.Utilities.ArchiveExtraction;

public static class ArchiveExtractor
{
    private static readonly char[] DirectorySeparators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    public static async Task<IEnumerable<string>> ExtractArchives(IEnumerable<string> inputPaths, string tmpPath)
    {
        var extractor = new Extractor();
        HashSet<string> parsePaths = new();
        string extractionRoot = Path.GetFullPath(Path.Combine(tmpPath, "Extracted") + Path.DirectorySeparatorChar);
        
        foreach (var path in inputPaths)
        {
            foreach (var entry in extractor.Extract(path))
            {
                if (entry.Parent == null)
                {
                    // Not an archive
                    parsePaths.Add(path);
                    continue;
                }
                
                string relativeArchivePath = GetRelativeArchivePath(path, entry.FullPath);
                string dstPath = Path.GetFullPath(Path.Combine(extractionRoot, relativeArchivePath));

                if (!dstPath.StartsWith(extractionRoot, StringComparison.Ordinal))
                {
                    Console.WriteLine($"Potential Zip Slip. Archive entry resolves outside extraction root: '{entry.FullPath}'");
                    continue;
                }
                
                string? directory = Path.GetDirectoryName(dstPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                await using var outStream = new FileStream(dstPath, FileMode.Append, FileAccess.Write);
                await entry.Content.CopyToAsync(outStream);
                
                parsePaths.Add(dstPath);
            }
        }
        
        return parsePaths;
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
}
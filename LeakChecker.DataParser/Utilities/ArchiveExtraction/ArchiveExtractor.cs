using LeakChecker.DataParser.Logging;
using LeakChecker.DataParser.Logging.Execution;
using LeakChecker.DataParser.Utilities.Settings;
using Microsoft.CST.RecursiveExtractor;

namespace LeakChecker.DataParser.Utilities.ArchiveExtraction;

public sealed class ArchiveExtractor(ISettings settings, ExecutionLogger logger)
{
    private static readonly char[] DirectorySeparators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    public async Task<IEnumerable<string>> ExtractArchives(IEnumerable<string> inputPaths)
    {
        var extractor = new Extractor();
        HashSet<string> parsePaths = new();
        string extractionRoot = Path.GetFullPath(Path.Combine(settings.TmpDirectory, "Extracted") + Path.DirectorySeparatorChar);
        
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
                    await logger.Log($"Potential Zip Slip. Archive entry tries resolve outside extraction root: '{entry.FullPath}'", LogLevel.Warning);
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
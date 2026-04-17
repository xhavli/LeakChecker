using System.Collections.Concurrent;
using LeakChecker.DataParser.Helpers.Enums;
using LeakChecker.DataParser.Helpers.Settings;
using LeakChecker.DataParser.Logging;
using LeakChecker.DataParser.Logging.Execution;
using Microsoft.CST.RecursiveExtractor;

namespace LeakChecker.DataParser.Helpers.ArchiveExtraction;

public sealed class ArchiveExtractor(ISettings settings, ExecutionLogger logger)
{
    private static readonly char[] DirectorySeparators = 
        [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];
    
    private readonly string _extractionRoot = Path.GetFullPath(
        Path.Combine(settings.TmpDirectory, "Extracted") + Path.DirectorySeparatorChar);
    
    private static readonly StringComparer StringComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;
    
    private static readonly StringComparison StringComparison = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    public async Task<IEnumerable<string>> ExtractArchives(IEnumerable<string> inputPaths)
    {
        var parsePaths = new ConcurrentDictionary<string, byte>(StringComparer);
        var writtenPaths = new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer);
        int archivesCount = 0;
        
        try
        {
            await Parallel.ForEachAsync(
                inputPaths,
                new ParallelOptions { MaxDegreeOfParallelism = settings.ThreadsCapacity },
                async (path, ct) =>
                {
                    if (!ArchiveDetector.IsPossibleArchive(path))
                    {
                        parsePaths.TryAdd(path, 0);
                        return;
                    }

                    archivesCount++;
                    Extractor extractor = new();

                    foreach (var entry in extractor.Extract(path))
                    {
                        string? extractedPath = await ExtractFile(path, entry, writtenPaths, ct);
                        
                        if (extractedPath != null)
                            parsePaths.TryAdd(extractedPath, 0);
                    }
                });
        }
        finally
        {
            if (!writtenPaths.IsEmpty)
                logger.Log($"Extracted {archivesCount} into {writtenPaths.Count} files.");
            
            foreach (var gate in writtenPaths.Values)
                gate.Dispose();
        }
        
        return parsePaths.Keys;
    }
    
    private async Task<string?> ExtractFile(
        string sourcePath,
        FileEntry entry,
        ConcurrentDictionary<string, SemaphoreSlim> writtenPaths,
        CancellationToken ct)
    {
        string relativeFilePath = GetRelativeArchivePath(sourcePath, entry.FullPath);
        string extractionPath = Path.GetFullPath(Path.Combine(_extractionRoot, relativeFilePath));

        if (!extractionPath.StartsWith(_extractionRoot, StringComparison))
        {
            logger.Log($"Potential Zip Slip. Archive entry tries resolve outside " +
                       $"extraction root: '{entry.FullPath}'", LogLevel.Warning);
            return null;
        }

        string? directory = Path.GetDirectoryName(extractionPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var gate = writtenPaths.GetOrAdd(extractionPath, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);

        try
        {
            await using var outStream = new FileStream(
                extractionPath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.None,
                bufferSize: SizeEnum.MegaByte * 4,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);

            await using var inStream = entry.Content;
            await inStream.CopyToAsync(outStream, ct);
        }
        finally
        {
            gate.Release();
        }

        return extractionPath;
    }
    
    private static string GetRelativeArchivePath(string sourcePath, string entryFullPath)
    {
        var rootName = Path.GetFileName(sourcePath);

        var segments = entryFullPath.Split(DirectorySeparators, StringSplitOptions.RemoveEmptyEntries);

        var startIndex = Array.FindIndex(
            segments, s => string.Equals(s, rootName, StringComparison));

        return startIndex < 0
            ? Path.GetFileName(entryFullPath)
            : Path.Combine(segments[startIndex..]);
    }
}
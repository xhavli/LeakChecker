using System.Collections.Concurrent;
using LeakChecker.DataParser.Helpers.Enums;
using LeakChecker.DataParser.Helpers.FileHelp;
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
    
    private readonly ParallelOptions _parallelOptions = new() { MaxDegreeOfParallelism = settings.ThreadsCapacity };

    public async Task<IEnumerable<string>> ExtractArchives(IEnumerable<string> inputPaths)
    {
        var pathsToParse = new ConcurrentDictionary<string, byte>(StringComparer);
        var writeLocks = new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer);
        
        int extractedArchives = 0;
        
        try
        {
            await Parallel.ForEachAsync(inputPaths, _parallelOptions, async (inputPath, ct) =>
                {
                    if (FileHelper.IsExcel(inputPath) || !ArchiveDetector.IsPossibleArchive(inputPath))
                    {
                        pathsToParse.TryAdd(inputPath, 0);
                        return;
                    }

                    Interlocked.Increment(ref extractedArchives);
                    Extractor extractor = new();

                    foreach (var entry in extractor.Extract(inputPath))
                    {
                        string? extractedPath = await ExtractFile(inputPath, entry, writeLocks, ct);
                        
                        if (extractedPath != null)
                            pathsToParse.TryAdd(extractedPath, 0);
                    }
                });
        }
        finally
        {
            if (!writeLocks.IsEmpty)
                logger.Log($"Extracted {extractedArchives} archives into {writeLocks.Count} files.");
            
            foreach (var gate in writeLocks.Values)
                gate.Dispose();
        }
        
        return pathsToParse.Keys;
    }
    
    private async Task<string?> ExtractFile(
        string inputPath,
        FileEntry entry,
        ConcurrentDictionary<string, SemaphoreSlim> writeLocks,
        CancellationToken ct)
    {
        string relativePath = BuildRelativeExtractionPath(inputPath, entry.FullPath);
        string extractionPath = Path.GetFullPath(Path.Combine(_extractionRoot, relativePath));

        if (!extractionPath.StartsWith(_extractionRoot, StringComparison))
        {
            logger.Log($"Potential Zip Slip. Archive entry tries resolve outside " +
                       $"extraction root: '{entry.FullPath}'", LogLevel.Warning);
            return null;
        }

        FileHelper.EnsureDirectoryExists(extractionPath);

        var writeLock = writeLocks.GetOrAdd(extractionPath, _ => new SemaphoreSlim(1, 1));
        await writeLock.WaitAsync(ct);

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
            writeLock.Release();
        }

        return extractionPath;
    }
    
    private static string BuildRelativeExtractionPath(string sourcePath, string entryFullPath)
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
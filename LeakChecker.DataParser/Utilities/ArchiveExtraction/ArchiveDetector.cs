using MimeDetective;
using MimeDetective.Definitions;
using MimeDetective.Definitions.Licensing;

namespace LeakChecker.DataParser.Utilities.ArchiveExtraction;

public static class ArchiveDetector
{
    private static readonly IContentInspector MimeInspector = new ContentInspectorBuilder
    {
        Definitions = new ExhaustiveBuilder { UsageType = UsageType.PersonalNonCommercial }.Build()
    }.Build();
    
    private static readonly HashSet<string> ArchiveMimes = new(StringComparer.OrdinalIgnoreCase)
    {
        // --- ZIP family ---
        "application/zip",
        "application/x-zip",
        "application/x-zip-compressed",
        "multipart/x-zip",

        // --- RAR ---
        "application/vnd.rar",
        "application/x-rar",
        "application/x-rar-compressed",

        // --- 7-Zip ---
        "application/x-7z-compressed",

        // --- TAR ---
        "application/x-tar",
        "application/tar",

        // --- GZIP ---
        "application/gzip",
        "application/x-gzip",

        // --- BZIP2 ---
        "application/x-bzip2",
        "application/bzip2",

        // --- XZ ---
        "application/x-xz",

        // --- LZMA ---
        "application/x-lzma",

        // --- Z (Unix compress) ---
        "application/x-compress",

        // --- AR (Unix archive, .a) ---
        "application/x-archive",

        // --- LZH / LHA ---
        "application/x-lzh",
        "application/x-lha",

        // --- ACE ---
        "application/x-ace-compressed",

        // --- StuffIt ---
        "application/x-stuffit",
        "application/x-sit",

        // --- Generic binary fallback sometimes returned ---
        "application/octet-stream"
    };

    private static readonly HashSet<string> ArchiveExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // --- Core formats ---
        ".zip",
        ".rar",
        ".7z",

        // --- TAR and compressed TAR ---
        ".tar",
        ".tgz",
        ".tar.gz",
        ".tbz",
        ".tbz2",
        ".tar.bz2",
        ".txz",
        ".tar.xz",
        ".tar.z",
        ".tar.lz",
        ".tar.lzma",

        // --- Single-file compressors ---
        ".gz",
        ".bz2",
        ".xz",
        ".lz",
        ".lzma",
        ".z",
        ".zst",

        // --- Less common ---
        ".ar",
        ".cpio",

        // --- Legacy ---
        ".ace",
        ".arj",
        ".lzh",
        ".lha",
        ".zoo"
    };

    public static bool IsPossibleArchive(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        if (IsDefinitelyArchive(filePath))
            return true;

        return HaveArchiveExtension(filePath);
    }

    private static bool IsDefinitelyArchive(string filePath, int reliableThreshold = 3500)
    {
        using var stream = File.OpenRead(filePath);
        
        var results = MimeInspector.Inspect(stream);
        var best = results.ByMimeType().FirstOrDefault();
        
        if (best is not null && best.Points > reliableThreshold)
        {
            if (!ArchiveMimes.Contains(best.MimeType))
                return false;
        }
        
        return false;
    }

    private static bool HaveArchiveExtension(string filePath)
    {
        string fileName = Path.GetFileName(filePath);

        foreach (var ext in ArchiveExtensions)
        {
            if (fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace LeakChecker.Format;

/// <summary>
/// Result for a single delimiter candidate.
/// </summary>
public sealed class DelimiterCandidate
{
    public char Delimiter { get; init; }
    public double ProbabilityPercent { get; internal set; }
    public double MeanPerLine { get; internal set; }
    public double StdDevPerLine { get; internal set; }
    public int LinesSeen { get; internal set; }
    public int LinesWithAny { get; internal set; }
    public double Coverage => LinesSeen == 0 ? 0 : (double)LinesWithAny / LinesSeen;
    /// <summary>Fraction of lines whose count is within ±1 of the final mean (only among lines with any occurrences).</summary>
    public double WithinOneOfMeanFraction { get; internal set; }
    /// <summary>The most common per-line count we observed and how often it occurred (mode estimate).</summary>
    public int ApproxModeCount { get; internal set; }
    public int ApproxModeFrequency { get; internal set; }

    public override string ToString()
        => $"'{(Delimiter == '\t' ? "\\t" : Delimiter.ToString())}' => {ProbabilityPercent:F1}% " +
           $"(mean {MeanPerLine:F2} ± {StdDevPerLine:F2}, coverage {Coverage:P0})";
}

public sealed class DelimiterHeuristicResult
{
    public char? BestDelimiter { get; internal set; }
    public int SampledLines { get; internal set; }
    public int SampledBytes { get; internal set; }
    public IReadOnlyList<DelimiterCandidate> Candidates { get; internal set; } = Array.Empty<DelimiterCandidate>();
}

/// <summary>
/// High-performance delimiter detector for very large UTF-8 text files.
/// </summary>
public static class DelimiterHeuristic
{
    // ASCII range we care about for delimiter candidates: punctuation & whitespace (space and tab).
    // We’ll mark candidates dynamically on first sight to keep the inner loop tight.
    private static bool IsAsciiPunctuationOrSpace(char ch)
    {
        if (ch > 0x7E) return false;                  // non-ASCII
        if (ch == '\r' ||  ch == '\n') return false;   // line breaks
        if (char.IsLetterOrDigit(ch)) return false;
        // Keep space and tab (common delimiters), exclude quotes separately (we still count them for quote detection).
        // We simply allow: space (0x20), tab (0x09), and other punctuation.
        return true;
    }

    /// <summary>
    /// Analyze a UTF-8 file and guess its delimiter using first <paramref name="maxLines"/> lines.
    /// </summary>
    /// <param name="path">File path (UTF-8 text).</param>
    /// <param name="maxLines">Max lines to analyze (default 10,000).</param>
    /// <param name="readBufferChars">Internal char buffer size (default 1,048,576).</param>
    /// <returns>Heuristic result with probabilities and diagnostics.</returns>
    public static DelimiterHeuristicResult Analyze(
        string path,
        int maxLines = 10_000,
        int readBufferChars = 1_048_576)
    {
        using var fs = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 1 << 20, // 1 MiB
            FileOptions.SequentialScan);

        var reader = new StreamReader(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), detectEncodingFromByteOrderMarks: true, bufferSize: 1 << 20, leaveOpen: false);

        return Analyze(reader, maxLines, readBufferChars);
    }

    /// <summary>
    /// Analyze from an existing <see cref="TextReader"/>. Reader must deliver UTF-8 text; only first <paramref name="maxLines"/> are read.
    /// </summary>
    private static DelimiterHeuristicResult Analyze(TextReader reader, int maxLines = 10_000, int readBufferChars = 1_048_576)
    {
        // Per-candidate running stats (ASCII 0..127).
        var stats = new DelimStats[128];
        var hasCandidate = new bool[128];
        
        // Per-line counters (stackalloc keeps GC cool). We only count ASCII candidates.
        Span<int> perLineCounts = stackalloc int[128];
        perLineCounts.Clear();

        // For estimating "within ±1 of mean" we’ll re-check at line end with evolving mean (approximation).
        // We also keep a tiny mode estimator per candidate (two most frequent counts).
        var modeA = new SmallMode[128];

        // State
        bool inQuote = false;
        char quoteChar = '\0';
        char prevChar = '\0';       // for CRLF and backslash-escape detection
        int lines = 0;
        int sampledBytesApprox = 0;

        // Reader loop
        char[] buffer = ArrayPool<char>.Shared.Rent(readBufferChars);
        try
        {
            while (lines < maxLines)
            {
                int read = reader.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                {
                    // flush any last unterminated line
                    if (AnyCounts(perLineCounts)) EndLine(ref lines, perLineCounts, stats, modeA);
                    break;
                }
                sampledBytesApprox += read; // chars ~ bytes for ASCII-heavy; fine for a sample stat

                int i = 0;
                while (i < read)
                {
                    char ch = buffer[i++];

                    // Handle CRLF newlines gracefully
                    if (!inQuote && (ch == '\n' || ch == '\r'))
                    {
                        // If CRLF, consume the LF if next
                        if (ch == '\r' && i < read && buffer[i] == '\n') i++;

                        EndLine(ref lines, perLineCounts, stats, modeA);
                        if (lines >= maxLines) break;

                        prevChar = ch;
                        continue;
                    }

                    // Quote handling (CSV-like): enter/exit on ' or " ; support doubled quotes and backslash-escaped quotes.
                    if (ch == '"' || ch == '\'')
                    {
                        if (!inQuote)
                        {
                            inQuote = true;
                            quoteChar = ch;
                            if (ch == '"')
                            {
                            }
                            else
                            {
                            }
                        }
                        else if (ch == quoteChar)
                        {
                            bool escapedByBackslash = prevChar == '\\';
                            bool doubled = (i < read && buffer[i] == quoteChar);
                            if (doubled)
                            {
                                // Consume the doubled quote, stays inQuote
                                i++;
                            }
                            else if (!escapedByBackslash)
                            {
                                // End quote
                                inQuote = false;
                                quoteChar = '\0';
                            }
                        }
                        prevChar = ch;
                        continue;
                    }

                    // Count delimiter candidates only when not inside quotes.
                    if (!inQuote)
                    {
                        if (ch <= 0x7F && IsAsciiPunctuationOrSpace(ch))
                        {
                            int idx = ch;
                            // Mark as candidate lazily (skip obvious non-delimiter punctuation later via scoring).
                            hasCandidate[idx] = true;
                            perLineCounts[idx]++;
                        }
                    }

                    prevChar = ch;
                }
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }

        // Build candidate list and compute probabilities
        var candidates = new List<DelimiterCandidate>(32);
        double bestScore = 0;
        double scoreSum = 0;

        for (int idx = 0; idx < 128; idx++)
        {
            if (!hasCandidate[idx]) continue;
            if (idx == '"' || idx == '\'') continue; // not delimiters

            ref var s = ref stats[idx];
            if (s.Lines == 0) continue;

            // Basic filters: ignore CR/LF (already separated); exclude non-printables except tab and space
            char delim = (char)idx;
            if (delim != '\t' && delim != ' ' && delim < 0x20) continue;

            double mean = s.Mean;
            double variance = s.Lines > 1 ? s.M2 / (s.Lines - 1) : 0.0;
            double stdev = Math.Sqrt(Math.Max(0, variance));

            double coverage = s.Lines == 0 ? 0 : (double)s.LinesWithAny / s.Lines;
            double coefVar = mean > 0 ? stdev / mean : 1.0;

            // Stability: fraction of lines with counts within ±1 of mean (among lines with any occurrences).
            double within1 = s.WithinOneOfMeanCount;
            double within1Frac = s.LinesWithAny == 0 ? 0 : within1 / s.LinesWithAny;

            // Scoring: encourage broad coverage, low variance, and non-trivial mean.
            // The log1p(mean) term favors delimiters with multiple columns.
            double score = coverage                           // appears on many lines
                           * (1.0 - Clamp01(coefVar))         // consistent counts per line
                           * Log1P(mean)                 // higher column counts
                           * (0.75 + 0.25 * within1Frac);     // slight bump for tight mode

            bestScore = Math.Max(bestScore, score);
            scoreSum += score;

            candidates.Add(new DelimiterCandidate
            {
                Delimiter = delim,
                MeanPerLine = mean,
                StdDevPerLine = stdev,
                LinesSeen = s.Lines,
                LinesWithAny = s.LinesWithAny,
                WithinOneOfMeanFraction = within1Frac,
                ApproxModeCount = s.ModeCount,
                ApproxModeFrequency = s.ModeFreq
            });
        }

        // Normalize scores => probabilities
        // If all scores 0 (e.g., fixed-width or weird data), everyone gets 0%; BestDelimiter stays null.
        if (scoreSum > 0)
        {
            // Recompute with the same formula to map to percentages
            foreach (var c in candidates)
            {
                ref var s = ref stats[c.Delimiter];
                double mean = c.MeanPerLine;
                double stdev = c.StdDevPerLine;
                double coverage = c.Coverage;
                double coefVar = mean > 0 ? stdev / mean : 1.0;
                double within1 = c.WithinOneOfMeanFraction;

                double score = coverage * (1.0 - Clamp01(coefVar)) * Log1P(mean) * (0.75 + 0.25 * within1);
                c.ProbabilityPercent = 100.0 * score / scoreSum;
            }
        }

        // Sort by probability desc, then by coverage desc as a tiebreaker
        candidates.Sort((a, b) =>
        {
            int cmp = b.ProbabilityPercent.CompareTo(a.ProbabilityPercent);
            if (cmp != 0) return cmp;
            return b.Coverage.CompareTo(a.Coverage);
        });

        var result = new DelimiterHeuristicResult
        {
            BestDelimiter = candidates.Count > 0 && candidates[0].ProbabilityPercent > 0 ? candidates[0].Delimiter : null,
            SampledLines = lines,
            SampledBytes = sampledBytesApprox,
            Candidates = candidates
        };
        return result;

        // ---- local helpers ----
        static bool AnyCounts(Span<int> counts)
        {
            ref int r0 = ref counts[0];
            for (int i = 0; i < counts.Length; i++)
                if (Unsafe.Add(ref r0, i) != 0) return true;
            return false;
        }

        static void EndLine(ref int lines, Span<int> perLine, DelimStats[] stats, SmallMode[] mode)
        {
            lines++;
            // We’ll update stats for any candidate that had a non-zero per-line count.
            ref int p0 = ref perLine[0];
            for (int idx = 0; idx < 128; idx++)
            {
                int count = Unsafe.Add(ref p0, idx);
                ref var s = ref stats[idx];

                // Update line counter even for zeros so variance reflects dataset lines (important for coverage).
                s.Lines++;

                if (count > 0)
                {
                    s.LinesWithAny++;
                    // Welford’s algorithm
                    double delta = count - s.Mean;
                    s.Mean += delta / s.Lines;
                    s.M2 += delta * (count - s.Mean);

                    // Track "within ±1 of current mean" approximation
                    double mu = s.Mean;
                    if (Math.Abs(count - mu) <= 1.0) s.WithinOneOfMeanCount++;

                    // Tiny mode estimator (2 bins with on-the-fly swap).
                    ref var m = ref mode[idx];
                    if (count == m.ACount) m.AFreq++;
                    else if (count == m.BCount) m.BFreq++;
                    else if (m.AFreq <= m.BFreq)
                    {
                        m.ACount = count; m.AFreq = 1;
                    }
                    else
                    {
                        m.BCount = count; m.BFreq = 1;
                    }

                    // Persist best mode into stats at line boundaries (keeps allocations at zero)
                    if (m.AFreq >= m.BFreq)
                    {
                        s.ModeCount = m.ACount; s.ModeFreq = m.AFreq;
                    }
                    else
                    {
                        s.ModeCount = m.BCount; s.ModeFreq = m.BFreq;
                    }
                }
            }
            perLine.Clear();
        }

        static double Clamp01(double x) => x < 0 ? 0 : (x > 1 ? 1 : x);
    }

    // Internal running-stat struct (kept as value type for cache locality).
    private struct DelimStats
    {
        public int Lines;
        public int LinesWithAny;
        public double Mean;
        public double M2;
        public int WithinOneOfMeanCount;
        public int ModeCount;
        public int ModeFreq;
    }

    private struct SmallMode
    {
        public int ACount, AFreq;
        public int BCount, BFreq;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double Log1P(double x)
    {
        // Numerically stable for small x
        if (Math.Abs(x) < 1e-4)
            return x - 0.5 * x * x;
        return Math.Log(1.0 + x);
    }
}
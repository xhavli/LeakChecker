using System.Diagnostics;
using System.Globalization;

namespace LeakChecker.DataParser.Format.Detection;

public sealed class DelimiterCandidate
{
    public char Delimiter { get; init; }
    public double ProbabilityPercent { get; internal set; }
    public double MeanPerLine { get; internal init; }
    public double StdDevPerLine { get; internal init; }
    public int LinesSeen { get; internal init; }
    public int LinesWithAny { get; internal init; }
    public double Coverage => LinesSeen == 0 ? 0 : (double)LinesWithAny / LinesSeen;
    public double WithinOneOfMeanFraction { get; internal init; }

    public override string ToString()
    {
        string delimiter = Delimiter == '\t' ? "\\t" : Delimiter.ToString();
        return $"'{delimiter}' = {ProbabilityPercent:F1}% (mean {MeanPerLine:F2} ± {StdDevPerLine:F2}, coverage {Coverage:P0})";
    }
}

public sealed class DelimiterHeuristicResult
{
    public char? BestDelimiter { get; internal init; }
    public int SampledLines { get; internal init; }
    public int SampledBytes { get; internal init; }
    public IReadOnlyList<DelimiterCandidate> Candidates { get; internal init; } = Array.Empty<DelimiterCandidate>();
    public TimeSpan Duration { get; internal init; }
}

public static class DelimiterHeuristic
{
    private static readonly HashSet<char> AllowedDelimiters = [' ', ',', ':', '\t', '|', ';', '~', '-', '_'];

    public static DelimiterHeuristicResult Analyze(StreamReader reader, int maxLines = 10_000, int bufferSize = 1_048_576)
    {
        Stopwatch sw = Stopwatch.StartNew();
        var stats = new DelimStats[128];
        var isCandidate = new bool[128];
        var perLine = new int[128];
        var mode = new SmallMode[128];

        int lines = 0, bytesRead = 0;
        bool inQuote = false;
        char quoteChar = '\0', prev = '\0';
        var buf = new char[bufferSize];

        while (lines < maxLines)
        {
            int read = reader.Read(buf, 0, buf.Length);
            if (read <= 0)
            {
                if (HasCounts(perLine))
                    EndLine(ref lines, perLine, stats, mode);
                break;
            }

            bytesRead += read;

            for (int i = 0; i < read; i++)
            {
                char ch = buf[i];

                if (!inQuote && ch is '\n' or '\r')
                {
                    if (ch == '\r' && i + 1 < read && buf[i + 1] == '\n') i++;
                    EndLine(ref lines, perLine, stats, mode);
                    if (lines >= maxLines) break;
                    prev = ch;
                    continue;
                }

                if (ch is '"' or '\'')
                {
                    if (!inQuote)
                    {
                        inQuote = true;
                        quoteChar = ch;
                    }
                    else if (ch == quoteChar)
                    {
                        bool escaped = prev == '\\';
                        bool doubled = (i + 1 < read && buf[i + 1] == quoteChar);
                        if (doubled) i++;
                        else if (!escaped)
                        {
                            inQuote = false;
                            quoteChar = '\0';
                        }
                    }
                    prev = ch;
                    continue;
                }

                if (!inQuote && ch <= 127 && IsPossibleDelimiter(ch))
                {
                    int idx = ch;
                    isCandidate[idx] = true;
                    perLine[idx]++;
                }

                prev = ch;
            }
        }

        var candidates = new List<DelimiterCandidate>();
        double totalScore = 0, bestScore = 0;

        for (int idx = 0; idx < 128; idx++)
        {
            if (!isCandidate[idx]) continue;
            char delim = (char)idx;
            if (!AllowedDelimiters.Contains(delim)) continue;

            var s = stats[idx];
            if (s.Lines == 0) continue;

            double mean = s.Mean;
            double stdev = s.Lines > 1 ? Math.Sqrt(s.M2 / (s.Lines - 1)) : 0;
            double coverage = (double)s.LinesWithAny / s.Lines;
            double coefVar = mean > 0 ? stdev / mean : 1;
            double within1Frac = s.LinesWithAny == 0 ? 0 : (double)s.Within1 / s.LinesWithAny;

            double score = coverage * (1 - Clamp01(coefVar)) * Log1P(mean) * (0.75 + 0.25 * within1Frac);
            totalScore += score;
            bestScore = Math.Max(bestScore, score);

            candidates.Add(new DelimiterCandidate
            {
                Delimiter = delim,
                MeanPerLine = mean,
                StdDevPerLine = stdev,
                LinesSeen = s.Lines,
                LinesWithAny = s.LinesWithAny,
                WithinOneOfMeanFraction = within1Frac,
            });
        }

        if (totalScore > 0)
        {
            foreach (var c in candidates)
            {
                double coefVar = c.StdDevPerLine / Math.Max(1e-9, c.MeanPerLine);
                double score = c.Coverage * (1 - Clamp01(coefVar)) * Log1P(c.MeanPerLine) * (0.75 + 0.25 * c.WithinOneOfMeanFraction);
                c.ProbabilityPercent = 100 * score / totalScore;
            }
        }

        candidates.Sort((a, b) =>
        {
            int cmp = b.ProbabilityPercent.CompareTo(a.ProbabilityPercent);
            return cmp != 0 ? cmp : b.Coverage.CompareTo(a.Coverage);
        });

        return new DelimiterHeuristicResult
        {
            BestDelimiter = candidates.Count > 0 && candidates[0].ProbabilityPercent > 0 ? candidates[0].Delimiter : null,
            SampledLines = lines,
            SampledBytes = bytesRead,
            Candidates = candidates,
            Duration = sw.Elapsed
        };
    }

    private static bool IsPossibleDelimiter(char ch)
    {
        if (AllowedDelimiters.Contains(ch)) return true;
        if (char.IsLetterOrDigit(ch) || char.IsControl(ch)) return false;
        UnicodeCategory cat = char.GetUnicodeCategory(ch);
        return cat is UnicodeCategory.SpaceSeparator
            or UnicodeCategory.DashPunctuation
            or UnicodeCategory.ModifierSymbol;
    }

    private static bool HasCounts(int[] arr)
    {
        foreach (int x in arr)
            if (x != 0) return true;
        return false;
    }

    private static void EndLine(ref int lines, int[] perLine, DelimStats[] stats, SmallMode[] mode)
    {
        lines++;
        for (int i = 0; i < perLine.Length; i++)
        {
            int count = perLine[i];
            var s = stats[i];
            s.Lines++;

            if (count > 0)
            {
                s.LinesWithAny++;
                double delta = count - s.Mean;
                s.Mean += delta / s.Lines;
                s.M2 += delta * (count - s.Mean);
                if (Math.Abs(count - s.Mean) <= 1) s.Within1++;

                var m = mode[i];
                if (count == m.ACount) m.AFreq++;
                else if (count == m.BCount) m.BFreq++;
                else if (m.AFreq <= m.BFreq) (m.ACount, m.AFreq) = (count, 1);
                else (m.BCount, m.BFreq) = (count, 1);

                mode[i] = m;
            }

            perLine[i] = 0;
            stats[i] = s;
        }
    }

    private static double Log1P(double x) => Math.Abs(x) < 1e-4 ? x - 0.5 * x * x : Math.Log(1 + x);
    private static double Clamp01(double x) => x < 0 ? 0 : (x > 1 ? 1 : x);

    private struct DelimStats
    {
        public int Lines;
        public int LinesWithAny;
        public double Mean;
        public double M2;
        public int Within1;
    }

    private struct SmallMode
    {
        public int ACount, AFreq;
        public int BCount, BFreq;
    }
}
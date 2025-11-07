using LeakChecker.Content;

namespace LeakChecker.Format;

public class SchemaHeuristic
{
    public readonly Dictionary<int, int[]> AttributeCountsPerPosition = new();
    public readonly int AttributeCount = Enum.GetValues(typeof(ItemEnum)).Length;
    private readonly Dictionary<int, List<int>> _delimiterCountsPerPosition = new();

    private Dictionary<int, (ItemEnum attr, double percent)>? _cachedDominant;

    public void AddLinePatterns(List<SchemaHeuristicRecord> linePatterns)
    {
        foreach (var pattern in linePatterns)
        {
            if (!AttributeCountsPerPosition.TryGetValue(pattern.Position, out var counts))
            {
                counts = new int[AttributeCount];
                AttributeCountsPerPosition[pattern.Position] = counts;
            }
            counts[(int)pattern.Attribute]++;

            if (!_delimiterCountsPerPosition.TryGetValue(pattern.Position, out var spans))
            {
                spans = new List<int>();
                _delimiterCountsPerPosition[pattern.Position] = spans;
            }
            spans.Add(pattern.DelimitersInside);
        }

        // Invalidate cache when new data is added
        _cachedDominant = null;
    }

    private Dictionary<int, (ItemEnum attr, double percent)> CalculateDominant(double threshold)
    {
        var result = new Dictionary<int, (ItemEnum, double)>();

        foreach (var kvp in AttributeCountsPerPosition)
        {
            var array = kvp.Value;
            int total = array.Sum();
            if (total == 0) continue;

            int maxCount = array.Max();
            int maxIndex = Array.IndexOf(array, maxCount);
            double percent = Math.Round((double)maxCount / total * 100.0, 2);

            if (percent >= threshold)
                result[kvp.Key] = ((ItemEnum)maxIndex, percent);
        }

        return result;
    }

    public Dictionary<int, (ItemEnum attr, double percent)> GetDominantStats(double threshold)
    {
        return _cachedDominant ??= CalculateDominant(threshold);
    }

    public Dictionary<int, ItemEnum> GetDominantSchema(double threshold)
    {
        var dominant = GetDominantStats(threshold);

        var ordered = dominant.Keys.OrderBy(i => i).ToArray();
        var result = new Dictionary<int, ItemEnum>();

        for (int idx = 0; idx < ordered.Length; idx++)
        {
            int start = ordered[idx];
            var attr = dominant[start].attr;

            if (result.ContainsKey(start)) continue;

            int avgDelims = 0;
            if (_delimiterCountsPerPosition.TryGetValue(start, out var spans) && spans.Count > 0)
                avgDelims = Math.Max(0, (int)Math.Round(spans.Average()));

            int expectedLength = Math.Max(1, avgDelims + 1);

            bool hasNextBoundary = idx + 1 < ordered.Length;
            int maxSpanBeforeNextKnown = hasNextBoundary
                ? Math.Min(expectedLength, Math.Max(1, ordered[idx + 1] - start))
                : expectedLength;

            result[start] = attr;

            for (int offset = 1; offset < maxSpanBeforeNextKnown ; offset++)
            {
                int pos = start + offset;
                if (dominant.ContainsKey(pos)) break;
                result[pos] = ItemEnum.Previous;
            }
        }

        return result;
    }
}

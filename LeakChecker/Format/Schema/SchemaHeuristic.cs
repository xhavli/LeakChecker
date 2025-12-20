using LeakChecker.Content;

namespace LeakChecker.Format.Schema;

public class SchemaHeuristic
{
    public readonly Dictionary<int, int[]> AttributeCountsPerPosition = new();
    private readonly int _attributeCount = Enum.GetValues(typeof(ItemEnum)).Length;
    private readonly Dictionary<int, int> _delimiterCountsPerLine = new();
    private readonly Dictionary<int, List<int>> _delimiterCountsPerPosition = new();
    private Dictionary<int, (ItemEnum attr, double percent)>? _cachedDominant;

    public void AddLinePatterns(List<SchemaHeuristicRecord> linePatterns, int lineDelimiterCount)
    {
        if (_delimiterCountsPerLine.TryGetValue(lineDelimiterCount, out int seen))
            _delimiterCountsPerLine[lineDelimiterCount] = seen + 1;
        else
            _delimiterCountsPerLine[lineDelimiterCount] = 1;
        
        foreach (var pattern in linePatterns)
        {
            if (!AttributeCountsPerPosition.TryGetValue(pattern.Position, out var counts))
            {
                counts = new int[_attributeCount];
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
    
    public Dictionary<int, ItemEnum> GetDominantSchema(double threshold)
    {
        var rawSchema = BuildRawSchema(threshold);
        return NormalizeSchemaLength(rawSchema);
    }

    private Dictionary<int, ItemEnum> BuildRawSchema(double threshold)
    {
        var dominantByPos = GetDominantStats(threshold);
        var sortedPositions = dominantByPos.Keys.OrderBy(p => p).ToArray();

        var schemaByPos = new Dictionary<int, ItemEnum>();

        for (int i = 0; i < sortedPositions.Length; i++)
        {
            int fieldStartPos = sortedPositions[i];
            if (schemaByPos.ContainsKey(fieldStartPos))
                continue;

            ItemEnum fieldAttr = dominantByPos[fieldStartPos].attr;

            int fieldWidth = InferFieldWidth(fieldStartPos, i, sortedPositions);

            schemaByPos[fieldStartPos] = fieldAttr;

            for (int step = 1; step < fieldWidth; step++)
            {
                int pos = fieldStartPos + step;
                if (dominantByPos.ContainsKey(pos))
                    break;

                schemaByPos[pos] = ItemEnum.Previous;
            }
        }

        return schemaByPos;
    }
    
    public Dictionary<int, (ItemEnum attr, double percent)> GetDominantStats(double threshold)
    {
        return _cachedDominant ??= CalculateDominant(threshold);
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
            {
                result[kvp.Key] = ((ItemEnum)maxIndex, percent);
            }
            else
            {
                result[kvp.Key] = (ItemEnum.Other, 0);
            }
        }

        return result;
    }
    
    private int InferFieldWidth(int fieldStartPos, int indexInSorted, int[] sortedPositions)
    {
        int expectedWidth = InferExpectedFieldWidth(fieldStartPos);

        bool hasNextKnownPos = indexInSorted + 1 < sortedPositions.Length;
        if (!hasNextKnownPos)
            return expectedWidth;

        int nextKnownPos = sortedPositions[indexInSorted + 1];
        int distanceToNextKnown = Math.Max(1, nextKnownPos - fieldStartPos);

        return Math.Min(expectedWidth, distanceToNextKnown);
    }

    private int InferExpectedFieldWidth(int fieldStartPos)
    {
        if (_delimiterCountsPerPosition.TryGetValue(fieldStartPos, out var samples) && samples.Count > 0)
        {
            int avgInnerDelims = (int)Math.Round(samples.Average());
            return Math.Max(1, avgInnerDelims + 1);
        }

        return 1;
    }

    private Dictionary<int, ItemEnum> NormalizeSchemaLength(Dictionary<int, ItemEnum> rawSchema)
    {
        int targetLength = GetTargetLength();
        var normalized = new Dictionary<int, ItemEnum>(targetLength);

        for (int pos = 0; pos < targetLength; pos++)
        {
            normalized[pos] = rawSchema.GetValueOrDefault(pos, ItemEnum.Other);
        }

        return normalized;
    }

    private int GetTargetLength()
    {
        // delimiter count + 1 = field count
        return Math.Max(1, GetMostCommonDelimiterCount() + 1);
    }
    
    private int GetMostCommonDelimiterCount()
    {
        if (_delimiterCountsPerLine.Count == 0)
            return 0;

        return _delimiterCountsPerLine
            .OrderByDescending(kvp => kvp.Value)    // most frequent
            .ThenBy(kvp => kvp.Key) // smaller length if equal occurence
            .First()
            .Key;
    }
}

using LeakChecker.ContentDetection;

namespace LeakChecker.FormatDetection;

public class HeuristicAnalyzer
{
    // Instead of storing every line, keep direct tallies:
    // Position -> counts array (indexed by ItemEnum)
    public readonly Dictionary<int, int[]> PositionCounts = new();
    public readonly int AttributeCount = Enum.GetValues(typeof(ItemEnum)).Length;
    private readonly Dictionary<int, List<int>> _delimiterSpans = new(); 
    

    /// <summary>
    /// Add recognized patterns from a single line (streaming aggregation).
    /// </summary>
    public void AddLinePatterns(List<HeuristicRecord> linePatterns)
    {
        foreach (var pat in linePatterns)
        {
            if (!PositionCounts.TryGetValue(pat.TokenStart, out var counts))
            {
                counts = new int[AttributeCount];
                PositionCounts[pat.TokenStart] = counts;
            }
            counts[(int)pat.Attribute]++;

            if (!_delimiterSpans.ContainsKey(pat.TokenStart))
                _delimiterSpans[pat.TokenStart] = new List<int>();

            _delimiterSpans[pat.TokenStart].Add(pat.DelimiterCountInside);
        }
    }

    private Dictionary<int, (ItemEnum Attribute, double SuccessRate)> ComputeDominantWithSuccessRate(double threshold = 50.0)
    {
        var result = new Dictionary<int, (ItemEnum, double)>();

        foreach (var kvp in PositionCounts)
        {
            int totalAtPosition = kvp.Value.Sum();
            if (totalAtPosition == 0) continue;

            int maxCount = kvp.Value.Max();
            int maxIndex = Array.IndexOf(kvp.Value, maxCount);

            double percent = (double)maxCount / totalAtPosition * 100.0;
            percent = Math.Round(percent, 2);

            if (percent >= threshold)
                result[kvp.Key] = ((ItemEnum)maxIndex, percent);
        }

        return result;
    }

    /// <summary>
    /// Returns a schema of positions → attributes, only including
    /// those where dominance >= threshold.
    /// </summary>
    public Dictionary<int, ItemEnum> GetDominantSchema(double threshold = 50.0)
    {
        var dominantWithRate = ComputeDominantWithSuccessRate(threshold);
        return dominantWithRate.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Attribute);
    }
    
    public Dictionary<int, (ItemEnum Attribute, int DelimiterSpan)> GetSchemaWithSpans(double threshold = 50.0)
    {
        var schema = new Dictionary<int, (ItemEnum, int)>();

        foreach (var kvp in PositionCounts)
        {
            int totalAtPosition = kvp.Value.Sum();
            if (totalAtPosition == 0) continue;

            int maxCount = kvp.Value.Max();
            int maxIndex = Array.IndexOf(kvp.Value, maxCount);
            double percent = (double)maxCount / totalAtPosition * 100.0;

            if (percent >= threshold)
            {
                // compute average or max span observed for this position
                int span = _delimiterSpans.TryGetValue(kvp.Key, out var delimiterSpan) 
                    ? (int)Math.Round(delimiterSpan.Average())
                    : 0;

                schema[kvp.Key] = ((ItemEnum)maxIndex, span);
            }
            else
            {
                //TODO
                //Sql Insert processing: ('2741', '', 'S14DJ90B321353', 'HABCEGDA', '', '0', '0', 'SI', '????? ?????? 3.3', '1306677462', '300', '', 'Unknown', '1306695600', 'Bantime expired', 'OVERPRO AUTOUNBAN', '55386', '')
                // [0] Other = 2741
                // [1] Other =
                // [2] Other = S14DJ90B321353
                // [3] Other = HABCEGDA
                // [4] IpV4 =
                // [5] Other = 0
                // [6] Other = 0
                // [7] Other = SI
                // [8] Location = ????? ?????? 3.3
                // [9] TimeStamp = 1306677462
                // [10] Other = 300
                // [11] IpV4 =
                // '1353316_1358895' [0:41:56] [WARNING] [PROCESSING] Unmapped field[12] = Unknown
                // [13] TimeStamp = 1306695600
                // [14] Other = Bantime expired
                // [15] Other = OVERPRO AUTOUNBAN
                // [16] TimeStamp = 55386
                // [17] Web =
                schema[kvp.Key] = ((ItemEnum.Other), 0);
            }
        }

        return schema;
    }

    public void PrintHeuristicData()
    {
        Console.WriteLine("Heuristic data stats:");

        foreach (var kvp in PositionCounts.OrderBy(x => x.Key))
        {
            int totalAtPosition = kvp.Value.Sum();
            Console.WriteLine($"Position {kvp.Key}: (total {totalAtPosition})");

            // Order attributes by occurrence (count) descending
            var records = Enumerable.Range(0, AttributeCount)
                .Where(i => kvp.Value[i] > 0)
                .Select(i => new
                {
                    Attribute = (ItemEnum)i,
                    Count = kvp.Value[i]
                })
                .OrderByDescending(r => r.Count);

            foreach (var rec in records)
            {
                double percent = (double)rec.Count / totalAtPosition * 100.0;
                Console.WriteLine($"   {rec.Attribute} = {rec.Count} ({percent:0.##}%)");
            }
        }

        Console.WriteLine();
    }

    public void PrintDominantSchema(double threshold = 50.0)
    {
        Console.WriteLine($"Likely schema (SuccessRate => {threshold}%):");

        foreach (var kvp in PositionCounts.OrderBy(x => x.Key))
        {
            int totalAtPosition = kvp.Value.Sum();
            if (totalAtPosition == 0) continue;

            // Find dominant attribute at this position
            int maxCount = kvp.Value.Max();
            int maxIndex = Array.IndexOf(kvp.Value, maxCount);
            double percent = (double)maxCount / totalAtPosition * 100.0;

            if (percent >= threshold)
            {
                Console.WriteLine(
                    $"   Position {kvp.Key} = ({(ItemEnum)maxIndex}, {percent:0.##}%)"
                );
            }
        }

        Console.WriteLine();
    }
}
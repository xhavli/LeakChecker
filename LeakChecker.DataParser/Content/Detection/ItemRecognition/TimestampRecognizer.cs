using Microsoft.Recognizers.Text.DateTime;

namespace LeakChecker.DataParser.Content.Detection.ItemRecognition;

public static class TimestampRecognizer
{
    private const string Culture = Microsoft.Recognizers.Text.Culture.English; // or Culture.EnglishOthers

    private readonly record struct TimestampCandidate(string Text, DateTime Value, int Start, int End);

    public static bool TryRecognize(string line, out List<string> stringTimeStamps, out List<DateTime> timeStamps)
    {
        stringTimeStamps = new List<string>();
        timeStamps = new List<DateTime>();
        
        var results = DateTimeRecognizer.RecognizeDateTime(line, Culture);
        if (results.Count == 0)
            return false;

        var candidates = new List<TimestampCandidate>(results.Count);

        foreach (var result in results)
        {
            dynamic resolution = result.Resolution;

            // foreach (var value in resolution["values"])  // When value < 12h offer alternatives like AM/PM, [0] is original value
            if (resolution == null)
                continue;
            
            var value = resolution["values"][0];
            
            // If result type is timerange or datetimerange, value["value"] not present in dictionary
            if (!value.ContainsKey("value"))
                continue;
            
            string dateTimeString = value["value"]; // ISO 8601 string
            
            if (DateTime.TryParse(dateTimeString, out DateTime timeStamp))
            {
                candidates.Add(new TimestampCandidate(result.Text, timeStamp, result.Start, result.End));
            }
        }

        foreach (var candidate in candidates)
        {
            if (IsStandalone(candidate, candidates))
            {
                stringTimeStamps.Add(candidate.Text);
                timeStamps.Add(candidate.Value);
            }
        }

        // Deduplicate by (Start, End) — keep first occurrence
        if (stringTimeStamps.Count > 1)
            DeduplicateByPosition(stringTimeStamps, timeStamps, candidates);

        return stringTimeStamps.Count > 0;
    }

    private static bool IsStandalone(TimestampCandidate candidate, List<TimestampCandidate> all)
    {
        if (candidate.Start < 0 || candidate.End < candidate.Start)
            return false;

        foreach (var other in all)
        {
            if (other.Start < 0 || other.End < other.Start)
                continue;

            // candidate is nested inside other
            if (candidate.Start >= other.Start &&
                candidate.End <= other.End &&
                (candidate.Start != other.Start || candidate.End != other.End))
                return false;
        }
        return true;
    }

    private static void DeduplicateByPosition(List<string> texts, List<DateTime> times, List<TimestampCandidate> candidates)
    {
        // Walk backwards and remove duplicates with same (Start, End)
        var seen = new HashSet<(int, int)>(texts.Count);
        // Rebuild from candidates that passed IsStandalone (parallel lists)
        texts.Clear();
        times.Clear();
        foreach (var c in candidates)
        {
            if (c.Start >= 0 && c.End >= c.Start && seen.Add((c.Start, c.End)))
            {
                texts.Add(c.Text);
                times.Add(c.Value);
            }
        }
    }
}
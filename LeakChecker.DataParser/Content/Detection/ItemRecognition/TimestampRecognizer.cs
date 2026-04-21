using Microsoft.Recognizers.Text.DateTime;

namespace LeakChecker.DataParser.Content.Detection.ItemRecognition;

public static class TimestampRecognizer
{
    private const string Culture = Microsoft.Recognizers.Text.Culture.English; // or Culture.EnglishOthers

    private sealed class TimestampCandidate
    {
        public required string Text { get; init; }
        public required DateTime Value { get; init; }
        public int Start { get; init; }
        public int End { get; init; }
    }
    
    public static Boolean TryRecognize(string line, out List<string> stringTimeStamps, out List<DateTime> timeStamps)
    {
        stringTimeStamps = new List<string>();
        timeStamps = new List<DateTime>();
        var candidates = new List<TimestampCandidate>();
        
        var results = DateTimeRecognizer.RecognizeDateTime(line, Culture);
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
                candidates.Add(new TimestampCandidate
                {
                    Text = result.Text,
                    Value = timeStamp,
                    Start = result.Start,
                    End = result.End
                });
            }
        }

        var standaloneCandidates = candidates
            .Where(candidate => !candidates.Any(other => IsNested(candidate, other)))
            .GroupBy(c => new { c.Start, c.End })
            .Select(g => g.First())
            .ToList();

        foreach (var candidate in standaloneCandidates)
        {
            stringTimeStamps.Add(candidate.Text);
            timeStamps.Add(candidate.Value);
        }

        return standaloneCandidates.Count > 0;
    }

    private static bool IsNested(TimestampCandidate candidate, TimestampCandidate other)
    {
        if (ReferenceEquals(candidate, other))
            return false;

        bool hasCandidateRange = candidate.Start >= 0 && candidate.End >= candidate.Start;
        bool hasOtherRange = other.Start >= 0 && other.End >= other.Start;

        if (!hasCandidateRange || !hasOtherRange)
            return false;

        return candidate.Start >= other.Start &&
               candidate.End <= other.End &&
               (candidate.Start != other.Start || candidate.End != other.End);
    }
}
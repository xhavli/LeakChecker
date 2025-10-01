using Microsoft.Recognizers.Text.DateTime;

namespace LeakChecker.ContentDetection.ItemRecognition;

public static class TimeStampRecognizer
{
    private const string Culture = Microsoft.Recognizers.Text.Culture.English; // or Culture.EnglishOthers
    
    // Set valid range
    private static readonly DateTime MinDate = new DateTime(2000, 1, 1);
    private static readonly DateTime MaxDate = DateTime.UtcNow.AddYears(10);

    public static Boolean TryRecognize(string line, out List<string> stringTimeStamps, out List<DateTime> timeStamps)
    {
        stringTimeStamps = new List<string>();
        timeStamps = new List<DateTime>();
        bool found = false;
        
        var results = DateTimeRecognizer.RecognizeDateTime(line, Culture);
        foreach (var result in results)
        {
            dynamic resolution = result.Resolution;

            // foreach (var value in resolution["values"])  // When value < 12h offer alternatives like AM/PM, [0] is original value
            if (resolution == null) continue;
            var value = resolution["values"][0];
            if (!value.ContainsKey("value")) continue;  // If result.Type is timerange or datetimerange, value["value"] not present in dictionary
            string dateTimeString = value["value"]; // ISO 8601 string
            
            if (DateTime.TryParse(dateTimeString, out DateTime timeStamp) && IsInRange(timeStamp))
            {
                stringTimeStamps.Add(result.Text);
                timeStamps.Add(timeStamp);
                found = true;
            }
        }

        return found;
    }
    
    private static bool IsInRange(DateTime dt) =>
        dt >= MinDate && dt <= MaxDate;
}
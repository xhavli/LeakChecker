using Microsoft.Recognizers.Text.DateTime;

namespace LeakChecker.ContentDetection.ItemRecognition;

public static class TimeStampRecognizer
{
    private const string Culture = Microsoft.Recognizers.Text.Culture.English; // or Culture.EnglishOthers

    public static Boolean TryRecognize(string line, out List<string> stringTimeStamps, out List<DateTime> timeStamps)
    {
        stringTimeStamps = new List<string>();
        timeStamps = new List<DateTime>();
        bool found = false;
        
        var results = DateTimeRecognizer.RecognizeDateTime(line, Culture);
        foreach (var result in results)
        {
            dynamic resolution = result.Resolution;

            // foreach (var value in resolution["values"])  // when value < 12h offer alternatives like AM/PM, [0] is original value
            var value = resolution["values"][0];
            string dateTimeString = value["value"]; // <-- ISO 8601 string
            if (DateTime.TryParse(dateTimeString, out DateTime timeStamp))
            {
                stringTimeStamps.Add(result.Text);
                timeStamps.Add(timeStamp);
                found = true;
            }
        }

        return found;
    }
}
using Microsoft.Recognizers.Text.Sequence;

namespace LeakChecker.Content.Detection.ItemRecognition;

public static class GuidRecognizer
{
    private const string Culture = Microsoft.Recognizers.Text.Culture.English; // or Culture.EnglishOthers

    public static Boolean TryRecognize(string line, out List<string> stringGuids, out List<Guid> guids)
    {
        stringGuids = new List<string>();
        guids = new List<Guid>();
        bool found = false;
        
        var results = SequenceRecognizer.RecognizeGUID(line, Culture);
        foreach (var result in results)
        {
            if (Guid.TryParse(result.Text, out Guid guid))
            {
                stringGuids.Add(result.Text);
                guids.Add(guid);
                found = true;
            }
        }

        return found;
    }
}
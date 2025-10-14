using Microsoft.Recognizers.Text.Sequence;

namespace LeakChecker.Content.Detection.ItemRecognition;

public static class WebRecognizer
{
    private const string Culture = Microsoft.Recognizers.Text.Culture.English; // or Culture.EnglishOthers

    public static Boolean TryRecognize(string line, out List<string> stringUris, out List<Uri> uris)
    {
        stringUris = new List<string>();
        uris = new List<Uri>();
        bool found = false;
        
        var results = SequenceRecognizer.RecognizeURL(line, Culture);
        foreach (var result in results)
        {
            if (Uri.TryCreate(result.Text, UriKind.RelativeOrAbsolute, out Uri? uri))
            {
                stringUris.Add(result.Text);
                uris.Add(uri);
                found = true;
            }
        }

        return found;
    }
}
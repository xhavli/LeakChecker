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
            int start = result.Start;
            int end = result.End;
            string textUri = line.Substring(start, end - start + 1);
            
            // if (textUri.Contains('_')) { continue; }    // TODO domain name cant contain '_'
            
            if (Uri.TryCreate(textUri, UriKind.RelativeOrAbsolute, out Uri? uri))
            {
                stringUris.Add(textUri);
                uris.Add(uri);
                found = true;
            }
        }

        return found;
    }
}
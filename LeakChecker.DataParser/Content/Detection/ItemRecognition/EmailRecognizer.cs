using System.Net.Mail;
using Microsoft.Recognizers.Text.Sequence;

namespace LeakChecker.Content.Detection.ItemRecognition;

public static class EmailRecognizer
{
    private const string Culture = Microsoft.Recognizers.Text.Culture.English; // or Culture.EnglishOthers

    public static Boolean TryRecognize(string line, out List<string> stringEmails, out List<MailAddress> emails)
    {
        stringEmails = new List<string>();
        emails = new List<MailAddress>();
        bool found = false;
        
        var results = SequenceRecognizer.RecognizeEmail(line, Culture);
        foreach (var result in results)
        {
            if (MailAddress.TryCreate(result.Text, out MailAddress? email))
            {
                stringEmails.Add(result.Text);
                emails.Add(email);
                found = true;
            }
        }

        return found;
    }
}
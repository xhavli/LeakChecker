using System.Net.Mail;
using LeakChecker.Content.Detection.ItemRecognition;

namespace LeakProcessor.Tests.Content.Detection.ItemRecognition;

public class EmailRecognizerTests
{
    [Theory]
    // Czech emails
    [InlineData("123:petr.novak@seznam.cz:456", "petr.novak@seznam.cz")]
    [InlineData("id42:lenka.kralova@centrum.cz;data", "lenka.kralova@centrum.cz")]
    [InlineData("xyz99,info@firma.cz,abc", "info@firma.cz")]
    [InlineData("uID:martina.kralova+newsletter@volny.cz:77", "martina.kralova+newsletter@volny.cz")]
    [InlineData("999:u.krajina-123@post.cz", "u.krajina-123@post.cz")]
    public void TryRecognize_ShouldFindCzechEmails(string input, string expectedEmail)
    {
        // Act
        var ok = EmailRecognizer.TryRecognize(input, out List<string> stringEmails, out List<MailAddress> mails);

        Assert.True(ok, $"Recognizer should find an email in: {input}");
        Assert.Contains(expectedEmail, stringEmails);
        Assert.Equal(expectedEmail, mails.First().Address);
    }

    [Theory]
    // .COM emails
    [InlineData("11:john.doe@gmail.com,88", "john.doe@gmail.com")]
    [InlineData("token:342:sarah_connor+work@gmail.com", "sarah_connor+work@gmail.com")]
    [InlineData("user-9:alice.smith@yahoo.com:end", "alice.smith@yahoo.com")]
    [InlineData("meta:bob.builder@outlook.com:17", "bob.builder@outlook.com")]
    [InlineData("ID12:jane-doe@hotmail.com;", "jane-doe@hotmail.com")]
    [InlineData("77:info@protonmail.com:xyz", "info@protonmail.com")]
    [InlineData("x1:support@aol.com:x2", "support@aol.com")]
    [InlineData("data:admin@mail.example.com:001", "admin@mail.example.com")]
    [InlineData("val:contact@fastmail.com;done", "contact@fastmail.com")]
    [InlineData("log123:user@icloud.com:end", "user@icloud.com")]
    public void TryRecognize_ShouldFindDotComEmails(string input, string expectedEmail)
    {
        // Act
        var ok = EmailRecognizer.TryRecognize(input, out List<string> stringEmails, out List<MailAddress> mails);

        Assert.True(ok, $"Recognizer should find an email in: {input}");
        Assert.Contains(expectedEmail, stringEmails);
        Assert.Equal(expectedEmail, mails.First().Address);
    }

    [Theory]
    // Russian emails
    [InlineData("12:ivan.petrov@mail.ru:abc", "ivan.petrov@mail.ru")]
    [InlineData("77:olga.sidorova@yandex.ru;data", "olga.sidorova@yandex.ru")]
    [InlineData("999:alexey.kuznetsov@rambler.ru:111", "alexey.kuznetsov@rambler.ru")]
    [InlineData("555:info@company.ru,text", "info@company.ru")]
    [InlineData("random:anna-smirnova@vk.ru:end", "anna-smirnova@vk.ru")]
    public void TryRecognize_ShouldFindRussianEmails(string input, string expectedEmail)
    {
        // Act
        var ok = EmailRecognizer.TryRecognize(input, out List<string> stringEmails, out List<MailAddress> mails);

        Assert.True(ok, $"Recognizer should find an email in: {input}");
        Assert.Contains(expectedEmail, stringEmails);
        Assert.Equal(expectedEmail, mails.First().Address);
    }
    
    [Theory]
    // 2 emails - with and without text
    [InlineData("pair:john.doe@gmail.com, jane.smith@outlook.com:end",
        "john.doe@gmail.com,jane.smith@outlook.com")]
    [InlineData("user1@company.com user2@company.com",
        "user1@company.com,user2@company.com")]
    [InlineData("IDs: [user.alpha@protonmail.com]; [user.beta@yahoo.com]",
        "user.alpha@protonmail.com,user.beta@yahoo.com")]
    [InlineData("{first.person@fastmail.com}|{second.person@gmail.com}",
        "first.person@fastmail.com,second.person@gmail.com")]

    // 3 emails - with and without text
    [InlineData("batch: alice@company.com, bob@company.org, charlie@company.net",
        "alice@company.com,bob@company.org,charlie@company.net")]
    [InlineData("notify list -> admin@mail.example.com; support@mail.example.com; helpdesk@mail.example.com",
        "admin@mail.example.com,support@mail.example.com,helpdesk@mail.example.com")]
    [InlineData("alpha@domain.com beta@domain.com gamma@domain.com",
        "alpha@domain.com,beta@domain.com,gamma@domain.com")]
    [InlineData("contacts: <team.lead@gmail.com> <qa.engineer@outlook.com> <dev.ops@fastmail.com>",
        "team.lead@gmail.com,qa.engineer@outlook.com,dev.ops@fastmail.com")]
    public void TryRecognize_ShouldFindMultipleEmails(string input, string expectedEmailsCsv)
    {
        // Arrange
        var expectedEmails = expectedEmailsCsv.Split(',').ToList();
        
        // Act
        var ok = EmailRecognizer.TryRecognize(input, out List<string> stringEmails, out List<MailAddress> mails);

        Assert.True(ok, $"Recognizer should find emails in: {input}");
        Assert.Equal(expectedEmails.Count, mails.Count);
        Assert.Equal(expectedEmails.Count, stringEmails.Count);

        foreach (var expected in expectedEmails)
            Assert.Contains(expected, stringEmails);
    }

    
    [Theory]
    // Negative test cases
    [InlineData("john.doe@@gmail.com")]              // Double @
    [InlineData("user.gmail.com")]                   // Missing @
    [InlineData("user@domain")]                      // No TLD
    [InlineData("@seznam.cz")]                       // Missing username
    [InlineData("name@domain.c")]                    // Too short TLD
    [InlineData("invalid@domain..com")]              // Double dot
    [InlineData(" user @ gmail . com ")]             // Spaces in address
    [InlineData("contact#example.com")]              // Invalid symbol
    [InlineData("justtextwithoutemail")]             // No email at all
    [InlineData("")]                                 // Empty
    public void TryRecognize_ShouldRejectInvalidEmails(string input)
    {
        // Act
        var ok = EmailRecognizer.TryRecognize(input, out List<string> stringEmails, out List<MailAddress> mails);

        // Assert
        Assert.False(ok, $"Recognizer should NOT find a valid email in: {input}");
        Assert.True(stringEmails == null || stringEmails.Count == 0, "No emails should be returned.");
        Assert.True(mails == null || mails.Count == 0, "No MailAddress should be parsed.");
    }
}
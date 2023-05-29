using MimeKit;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EmailPreview;

public class EmailPreview
{
    private const int HtmlFileOpenMaxRetries = 10;

    protected readonly FileInfo htmlFile;
    protected readonly FileInfo outputFile;
    protected readonly string subject;
    protected readonly CancellationToken cancellationToken;

    public EmailPreview(FileInfo htmlFile, FileInfo? outputFile, string? subject, CancellationToken cancellationToken)
    {
        this.htmlFile = htmlFile;

        // Save in %TEMP%\htmlFile.eml if no output path specified
        this.outputFile = outputFile ?? new FileInfo(Path.Combine(Path.GetTempPath(), htmlFile.Name[..^htmlFile.Extension.Length] + ".eml"));

        // Subject will be required to find the correct Outlook window
        this.subject = string.IsNullOrEmpty(subject) ? htmlFile.Name : subject;

        this.cancellationToken = cancellationToken;
    }

    public virtual async Task<int> Execute()
    {
        try
        {
            string htmlBody = await ReadHtml();
            await WriteEmail(htmlBody);
            return 0;
        }
        catch (OperationCanceledException)
        {
            return 2;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return 1;
        }
    }

    protected async ValueTask<string> ReadHtml()
    {
        for (int retries = 0; ; retries++)
        {
            try
            {
                using StreamReader reader = htmlFile.OpenText();
                return await reader.ReadToEndAsync(cancellationToken);
            }
            catch (IOException) when (retries < HtmlFileOpenMaxRetries)
            {
                await Task.Delay(retries * 5, cancellationToken);
            }
        }
    }

    protected async ValueTask WriteEmail(string htmlBody)
    {
        MimeMessage message = new();
        message.From.Add(new MailboxAddress("Alice", "alice@example.org"));
        message.To.Add(new MailboxAddress("Bob", "bob@example.org"));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };
        await message.WriteToAsync(outputFile.FullName, cancellationToken);

        Outlook.CloseEmailWindow(subject);
        Outlook.Open(outputFile);
    }
}

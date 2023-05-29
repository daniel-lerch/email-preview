using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EmailPreview;

public sealed class EmailPreviewWatching : EmailPreview
{
    private readonly FileContentWatcher watcher;

    public EmailPreviewWatching(FileInfo htmlFile, FileInfo? outputFile, string? subject, CancellationToken cancellationToken)
        : base(htmlFile, outputFile, subject, cancellationToken)
    {
        watcher = new FileContentWatcher(htmlFile);
    }

    public override async Task<int> Execute()
    {
        try
        {
            string htmlBody;
            string lastHtmlBody = htmlBody = await ReadHtml();
            await WriteEmail(htmlBody);

            while (!cancellationToken.IsCancellationRequested)
            {
                await watcher.WaitForContentChange(cancellationToken);
                htmlBody = await ReadHtml();
                if (!lastHtmlBody.Equals(htmlBody, StringComparison.Ordinal))
                {
                    await WriteEmail(htmlBody);
                    lastHtmlBody = htmlBody;
                }
            }

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
}

using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EmailPreview;

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        Argument<FileInfo> htmlFileArgument = new(name: "HTML_FILE", description: "HTML file to use as email body");

        Option<FileInfo?> outputFileOption = new(name: "--output", description: "Write email to this .eml file");
        Option<string?> subjectOption = new(name: "--subject", description: "Subject for generated email");
        Option<bool> watchOption = new(name: "--watch", description: "Watch for changes of the HTML file and hot reload");

        RootCommand rootCommand = new(description: "Preview HTML email templates in Outlook");
        rootCommand.AddArgument(htmlFileArgument);
        rootCommand.AddOption(outputFileOption);
        rootCommand.AddOption(subjectOption);
        rootCommand.AddOption(watchOption);
        rootCommand.SetHandler(async context =>
        {
            FileInfo htmlFile = context.ParseResult.GetValueForArgument(htmlFileArgument);
            FileInfo? outputFile = context.ParseResult.GetValueForOption(outputFileOption);
            string? subject = context.ParseResult.GetValueForOption(subjectOption);
            bool watch = context.ParseResult.GetValueForOption(watchOption);
            CancellationToken cancellationToken = context.GetCancellationToken();

            EmailPreview emailPreview = watch 
                ? new EmailPreviewWatching(htmlFile, outputFile, subject, cancellationToken)
                : new EmailPreview(htmlFile, outputFile, subject, cancellationToken);

            context.ExitCode = await emailPreview.Execute();
        });

        return await rootCommand.InvokeAsync(args);
    }
}

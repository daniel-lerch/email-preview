using MimeKit;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;

namespace EmailPreview;

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        Argument<FileInfo> htmlFileArgument = new(name: "HTML_FILE", description: "HTML file to use as email body");

        Option<string?> subjectOption = new(name: "--subject", description: "Subject for generated email");

        RootCommand rootCommand = new();
        rootCommand.AddArgument(htmlFileArgument);
        rootCommand.AddOption(subjectOption);
        rootCommand.SetHandler(Process, htmlFileArgument, subjectOption);

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task Process(FileInfo file, string? subject)
    {
        using StreamReader reader = file.OpenText();
        string content = await reader.ReadToEndAsync();

        MimeMessage message = new();
        message.From.Add(new MailboxAddress("Alice", "alice@example.org"));
        message.To.Add(new MailboxAddress("Bob", "bob@example.org"));
        if (!string.IsNullOrEmpty(subject))
            message.Subject = subject;
        message.Body = new TextPart("html") { Text = content };
        await message.WriteToAsync(file.Name[..^file.Extension.Length] + ".eml");
    }
}
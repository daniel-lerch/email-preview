using System;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EmailPreview;

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        // In C# the args array only contains actual command line arguments, not the executable name
        if (args.Length == 0)
        {
            // Email Preview might have been launched via start menu, shortcut or execute menu (Win+R).
            // In that case the process should not terminate immediately with a non-zero exit code
            // and rather show an interactive help for users not familiar with console apps.

            Process? parentProcess = NativeMethods.GetParentProcess(Process.GetCurrentProcess());
#if DEBUG
            if (parentProcess != null && (parentProcess.ProcessName is "explorer" or "dotnet"))
#else
            if (parentProcess != null && parentProcess.ProcessName == "explorer")
#endif
            {
                // Email Preview should be launched from a shell.
                // That means its parent process should be cmd, powershell, or pwsh
                // If it is explorer, however, Email Preview shows an interactive help

                RunInteractiveHelp();
                return 1;
            }
        }

        return await RunCommandLineApp(args);
    }

    private static async ValueTask<int> RunCommandLineApp(string[] args)
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

    private static void RunInteractiveHelp()
    {
        Console.WriteLine("Email Preview has detected that you launched it via start menu, shortcut or execute window. "
            + "However, Email Preview is a console application. That means you have to use it from a terminal.");
        Console.WriteLine();
        Console.WriteLine("1. Press Win+S");
        Console.WriteLine("2. Search for \"Terminal\"");
        Console.WriteLine("3. Open Terminal");
        Console.WriteLine("4. Type \"email-preview --help\" and hit the return key");
        Console.WriteLine();
        Console.WriteLine("At this point you should see a help menu which arguments are required to use Email Preview.");
        Console.WriteLine();
        Console.Write("Press any key to exit...");
        Console.ReadKey(intercept: true);
        Console.WriteLine();
    }
}

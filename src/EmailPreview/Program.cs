using MimeKit;
using PInvoke;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        rootCommand.SetHandler(HandleRootCommand, htmlFileArgument, subjectOption);

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task HandleRootCommand(FileInfo file, string? subject)
    {
        // Subject will be required to find the correct Outlook window
        if (string.IsNullOrEmpty(subject)) subject = file.Name;

        string outputPath = file.Name[..^file.Extension.Length] + ".eml";

        using StreamReader reader = file.OpenText();
        string content = await reader.ReadToEndAsync();

        MimeMessage message = new();
        message.From.Add(new MailboxAddress("Alice", "alice@example.org"));
        message.To.Add(new MailboxAddress("Bob", "bob@example.org"));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = content };
        await message.WriteToAsync(outputPath);

        CloseOutlookEmailWindow(subject);
        Process.Start(new ProcessStartInfo(outputPath) { UseShellExecute = true });
    }

    private static IReadOnlyList<nint> GetProcessWindows(Process process)
    {
        List<nint> windows = new();

        User32.EnumWindows((hWnd, lParam) =>
        {
            if (User32.GetWindowThreadProcessId(hWnd, out int processId) != 0 && processId == process.Id)
                windows.Add(hWnd);
            return true;
        }, 0);

        return windows;
    }

    private static nint GetOutlookEmailWindow(string subject)
    {
        Process? outlookProcess = Process.GetProcessesByName("OUTLOOK").FirstOrDefault();

        if (outlookProcess == null)
        {
            Console.WriteLine("Could not find an Outlook process");
            return 0;
        }

        foreach (nint hWnd in GetProcessWindows(outlookProcess))
        {
            if (User32.GetWindowText(hWnd).Contains(subject))
            {
                return hWnd;
            }
        }

        Console.WriteLine("Could not find an Outlook windows that's title contains {0}", subject);
        return 0;
    }

    private static void CloseOutlookEmailWindow(string subject)
    {
        nint hWnd = GetOutlookEmailWindow(subject);
        if (hWnd != 0)
        {
            User32.SendMessage(hWnd, User32.WindowMessage.WM_CLOSE, 0, 0);
        }
    }
}

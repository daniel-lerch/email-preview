using PInvoke;
using System.Diagnostics;
using System;
using System.Linq;
using System.IO;

namespace EmailPreview;

public static class Outlook
{
    public static void Open(FileInfo file)
    {
        Process.Start(new ProcessStartInfo(file.FullName) { UseShellExecute = true });
    }

    public static void CloseEmailWindow(string subject)
    {
        nint hWnd = GetOutlookEmailWindow(subject);
        if (hWnd != 0)
        {
            User32.SendMessage(hWnd, User32.WindowMessage.WM_CLOSE, 0, 0);
        }
    }

    private static nint GetOutlookEmailWindow(string subject)
    {
        Process? outlookProcess = Process.GetProcessesByName("OUTLOOK").FirstOrDefault();

        if (outlookProcess == null)
        {
            Console.WriteLine("Could not find an Outlook process");
            return 0;
        }

        foreach (nint hWnd in NativeMethods.GetProcessWindows(outlookProcess))
        {
            if (User32.GetWindowText(hWnd).Contains(subject))
            {
                return hWnd;
            }
        }

        Console.WriteLine("Could not find an Outlook windows that's title contains {0}", subject);
        return 0;
    }
}

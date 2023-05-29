using PInvoke;
using System.Collections.Generic;
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
}

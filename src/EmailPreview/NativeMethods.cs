using PInvoke;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EmailPreview;

public static class NativeMethods
{
    public static IReadOnlyList<nint> GetProcessWindows(Process process)
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

    public static unsafe Process? GetParentProcess(Process process)
    {
        NTDll.PROCESS_BASIC_INFORMATION pbi = new();
        NTSTATUS status = NTDll.NtQueryInformationProcess(
            new Kernel32.SafeObjectHandle(process.Handle, ownsHandle: false),
            NTDll.PROCESSINFOCLASS.ProcessBasicInformation,
            &pbi,
            Marshal.SizeOf<NTDll.PROCESS_BASIC_INFORMATION>(),
            out _);
        status.ThrowOnError();

        try
        {
            return Process.GetProcessById((int)pbi.Reserved3);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}

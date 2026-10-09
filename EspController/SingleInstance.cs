using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EspController;

internal static class SingleInstance
{
    internal static void ActivateExistingWindow()
    {
        using Process current = Process.GetCurrentProcess();
        foreach (Process process in Process.GetProcessesByName(current.ProcessName))
        {
            using (process)
            {
                if (process.Id == current.Id || process.SessionId != current.SessionId) continue;
                nint window = process.MainWindowHandle;
                if (window == 0) continue;
                if (IsIconic(window)) ShowWindow(window, 9);
                SetForegroundWindow(window);
                return;
            }
        }
    }

    [DllImport("user32.dll")] private static extern bool IsIconic(nint window);
    [DllImport("user32.dll")] private static extern bool ShowWindow(nint window, int command);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
}

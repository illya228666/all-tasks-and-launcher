using System.Drawing;
using System.Runtime.InteropServices;

namespace Launcher.Pet.Hat;

// Получает реальные позиции значков рабочего стола из Explorer ListView.
// Геометрия кэшируется ненадолго, чтобы не читать память Explorer каждый physics tick.
internal sealed class DesktopIconSurfaceProvider
{
    private const long CacheDurationMs = 500;

    private const uint ListViewFirst = 0x1000;
    private const uint ListViewGetItemCount = ListViewFirst + 4;
    private const uint ListViewGetItemRect = ListViewFirst + 14;
    private const int ListViewIconBounds = 1;

    private const uint ProcessVmOperation = 0x0008;
    private const uint ProcessVmRead = 0x0010;
    private const uint ProcessVmWrite = 0x0020;
    private const uint MemCommit = 0x1000;
    private const uint MemReserve = 0x2000;
    private const uint MemRelease = 0x8000;
    private const uint PageReadWrite = 0x04;

    private readonly List<DesktopSurface> _cachedSurfaces = new();
    private long _lastRefresh;

    internal IReadOnlyList<DesktopSurface> GetSurfaces()
    {
        long now = Environment.TickCount64;
        if (_lastRefresh != 0 && now - _lastRefresh < CacheDurationMs)
            return _cachedSurfaces;

        _lastRefresh = now;
        _cachedSurfaces.Clear();
        Refresh(_cachedSurfaces);
        return _cachedSurfaces;
    }

    private static void Refresh(List<DesktopSurface> target)
    {
        IntPtr listView = FindDesktopListView();
        if (listView == IntPtr.Zero
            || GetWindowThreadProcessId(listView, out uint processId) == 0
            || processId == 0)
            return;

        IntPtr process = OpenProcess(
            ProcessVmOperation | ProcessVmRead | ProcessVmWrite,
            false,
            processId);
        if (process == IntPtr.Zero)
            return;

        IntPtr remoteRect = IntPtr.Zero;
        try
        {
            nuint rectSize = (nuint)Marshal.SizeOf<NativeRect>();
            remoteRect = VirtualAllocEx(
                process,
                IntPtr.Zero,
                rectSize,
                MemCommit | MemReserve,
                PageReadWrite);
            if (remoteRect == IntPtr.Zero)
                return;

            int itemCount = SendMessage(
                listView,
                ListViewGetItemCount,
                IntPtr.Zero,
                IntPtr.Zero).ToInt32();

            for (int itemIndex = 0; itemIndex < itemCount; itemIndex++)
            {
                var rect = new NativeRect { Left = ListViewIconBounds };
                if (!WriteProcessMemory(process, remoteRect, ref rect, rectSize, out _)
                    || SendMessage(listView, ListViewGetItemRect, new IntPtr(itemIndex), remoteRect) == IntPtr.Zero
                    || !ReadProcessMemory(process, remoteRect, out rect, rectSize, out _))
                    continue;

                var topLeft = new NativePoint { X = rect.Left, Y = rect.Top };
                var bottomRight = new NativePoint { X = rect.Right, Y = rect.Bottom };
                if (!ClientToScreen(listView, ref topLeft) || !ClientToScreen(listView, ref bottomRight))
                    continue;

                Rectangle bounds = Rectangle.FromLTRB(
                    topLeft.X,
                    topLeft.Y,
                    bottomRight.X,
                    bottomRight.Y);
                if (bounds.Width <= 0 || bounds.Height <= 0)
                    continue;

                target.Add(new DesktopSurface(
                    bounds,
                    listView,
                    DesktopSurfaceType.DesktopIcon));
            }
        }
        finally
        {
            if (remoteRect != IntPtr.Zero)
                VirtualFreeEx(process, remoteRect, 0, MemRelease);
            CloseHandle(process);
        }
    }

    private static IntPtr FindDesktopListView()
    {
        IntPtr shellView = IntPtr.Zero;
        IntPtr progman = FindWindow("Progman", null);
        if (progman != IntPtr.Zero)
            shellView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);

        if (shellView == IntPtr.Zero)
        {
            EnumWindows((handle, _) =>
            {
                IntPtr candidate = FindWindowEx(handle, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (candidate == IntPtr.Zero)
                    return true;

                shellView = candidate;
                return false;
            }, IntPtr.Zero);
        }

        return shellView == IntPtr.Zero
            ? IntPtr.Zero
            : FindWindowEx(shellView, IntPtr.Zero, "SysListView32", null);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        internal int X;
        internal int Y;
    }

    private delegate bool EnumWindowsCallback(IntPtr handle, IntPtr parameter);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string className, string? windowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowEx(
        IntPtr parent,
        IntPtr childAfter,
        string className,
        string? windowName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(
        IntPtr window,
        uint message,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ClientToScreen(IntPtr window, ref NativePoint point);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint desiredAccess, bool inheritHandle, uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr VirtualAllocEx(
        IntPtr process,
        IntPtr address,
        nuint size,
        uint allocationType,
        uint protection);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool VirtualFreeEx(
        IntPtr process,
        IntPtr address,
        nuint size,
        uint freeType);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WriteProcessMemory(
        IntPtr process,
        IntPtr baseAddress,
        ref NativeRect buffer,
        nuint size,
        out nuint bytesWritten);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReadProcessMemory(
        IntPtr process,
        IntPtr baseAddress,
        out NativeRect buffer,
        nuint size,
        out nuint bytesRead);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);
}

using System.Drawing;
using System.Runtime.InteropServices;

namespace Launcher.Pet.Hat;

// Получает реальные позиции значков рабочего стола из Explorer ListView.
// Геометрия кэшируется ненадолго, чтобы не читать память Explorer каждый physics tick.
internal sealed partial class DesktopIconSurfaceProvider
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

    private IReadOnlyList<DesktopSurface> _cachedSurfaces = Array.Empty<DesktopSurface>();
    private long _lastRefresh;
    private IntPtr _listView;
    private uint _processId;
    private bool _refreshing;

    internal IReadOnlyList<DesktopSurface> GetSurfaces()
    {
        long now = Environment.TickCount64;
        if (_refreshing || (_lastRefresh != 0 && now - _lastRefresh < CacheDurationMs))
        {
            if (!IsWindowVisible(_listView)
                || GetWindowThreadProcessId(_listView, out uint processId) == 0
                || processId != _processId)
                _cachedSurfaces = Array.Empty<DesktopSurface>();
            return _cachedSurfaces;
        }

        _lastRefresh = now;
        IntPtr listView = FindDesktopListView();
        if (listView != _listView)
            _cachedSurfaces = Array.Empty<DesktopSurface>();
        _listView = listView;
        if (_listView == IntPtr.Zero || !IsWindowVisible(_listView)
            || GetWindowThreadProcessId(_listView, out uint currentProcessId) == 0 || currentProcessId == 0)
            return _cachedSurfaces = Array.Empty<DesktopSurface>();
        if (_processId != currentProcessId)
            _cachedSurfaces = Array.Empty<DesktopSurface>();
        _processId = currentProcessId;

        _refreshing = true;
        try
        {
            var surfaces = new List<DesktopSurface>();
            Refresh(surfaces, _listView, _processId);
            // Не угадываем соответствие, если Shell вернул неоднозначные identity.
            var duplicates = surfaces.GroupBy(surface => surface.Identity)
                .Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet();
            surfaces.RemoveAll(surface => duplicates.Contains(surface.Identity));
            // COM может обрабатывать сообщения UI во время вызова. Публикуем
            // только целый снимок, чтобы повторный вход не увидел половину списка.
            _cachedSurfaces = surfaces;
        }
        catch (Exception exception) when (exception is COMException or InvalidCastException)
        {
            // Explorer может исчезнуть между получением view и чтением PIDL.
            _cachedSurfaces = Array.Empty<DesktopSurface>();
        }
        finally
        {
            _refreshing = false;
        }
        return _cachedSurfaces;
    }

    internal bool TryRefresh(DesktopSurfaceIdentity identity, out DesktopSurface surface)
    {
        foreach (DesktopSurface candidate in GetSurfaces())
        {
            if (candidate.Identity != identity)
                continue;
            surface = candidate;
            return true;
        }
        surface = default;
        return false;
    }

    private static void Refresh(List<DesktopSurface> target, IntPtr listView, uint processId)
    {
        IFolderView? view = GetDesktopFolderView(listView);
        if (view is null)
            return;

        IntPtr process = IntPtr.Zero;
        IntPtr remoteRect = IntPtr.Zero;
        try
        {
            process = OpenProcess(
                ProcessVmOperation | ProcessVmRead | ProcessVmWrite,
                false,
                processId);
            if (process == IntPtr.Zero)
                return;
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
                string? itemKey = GetItemKey(view, itemIndex);
                if (itemKey is null)
                    continue;

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

                // Индекс служит только адресом чтения. Если view перестроился
                // во время чтения геометрии, результат не становится опорой.
                if (itemKey != GetItemKey(view, itemIndex))
                    continue;

                target.Add(new DesktopSurface(
                    bounds,
                    new DesktopSurfaceIdentity(
                        DesktopSurfaceType.DesktopIcon, listView, $"{processId}:{itemKey}")));
            }
        }
        finally
        {
            if (remoteRect != IntPtr.Zero)
                VirtualFreeEx(process, remoteRect, 0, MemRelease);
            if (process != IntPtr.Zero)
                CloseHandle(process);
            Marshal.ReleaseComObject(view);
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
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr window);

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

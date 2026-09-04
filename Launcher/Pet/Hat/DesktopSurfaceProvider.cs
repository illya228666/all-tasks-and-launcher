using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace Launcher.Pet.Hat;

internal sealed class DesktopSurfaceProvider
{
    private const int ExtendedStyleIndex = -20;
    private const int ToolWindowStyle = 0x00000080;
    private const int ExtendedFrameBoundsAttribute = 9;
    private const int CloakedAttribute = 14;

    private readonly DesktopIconSurfaceProvider _desktopIconProvider = new();

    internal IReadOnlyList<DesktopSurface> GetSurfaces(IntPtr excludedWindow)
    {
        List<DesktopSurface> windows = EnumerateWindows(excludedWindow);
        var surfaces = new List<DesktopSurface>(windows);
        AddAvailableDesktopIcons(surfaces, windows);
        foreach (Screen screen in Screen.AllScreens)
            surfaces.Add(GetTaskbarSurface(screen));
        return surfaces;
    }

    internal bool TryRefresh(
        DesktopSurfaceIdentity identity,
        IntPtr excludedWindow,
        out DesktopSurface surface)
    {
        surface = default;
        switch (identity.Type)
        {
            case DesktopSurfaceType.Window:
                return TryGetWindowSurface(identity.WindowHandle, excludedWindow, out surface);
            case DesktopSurfaceType.DesktopIcon:
                return _desktopIconProvider.TryRefresh(identity, out surface);
            case DesktopSurfaceType.Taskbar:
                Screen? screen = Screen.AllScreens.FirstOrDefault(item => item.DeviceName == identity.ItemKey);
                if (screen is null)
                    return false;
                surface = GetTaskbarSurface(screen);
                return true;
            default:
                return false;
        }
    }

    private static DesktopSurface GetTaskbarSurface(Screen screen)
    {
        Rectangle area = screen.WorkingArea;
        return new DesktopSurface(
            Rectangle.FromLTRB(area.Left, area.Bottom, area.Right, area.Bottom + 1),
            new DesktopSurfaceIdentity(DesktopSurfaceType.Taskbar, IntPtr.Zero, screen.DeviceName));
    }

    private static bool TryGetWindowSurface(IntPtr handle, IntPtr excludedWindow, out DesktopSurface surface)
    {
        surface = default;
        if (!IsUsableWindow(handle, excludedWindow) || !TryGetBounds(handle, out Rectangle bounds))
            return false;
        surface = new DesktopSurface(bounds, new DesktopSurfaceIdentity(DesktopSurfaceType.Window, handle));
        return true;
    }

    private void AddAvailableDesktopIcons(
        List<DesktopSurface> surfaces,
        IReadOnlyCollection<DesktopSurface> windows)
    {
        foreach (DesktopSurface icon in _desktopIconProvider.GetSurfaces())
        {
            // Иконки находятся на самом нижнем слое рабочего стола. Если обычное
            // окно перекрывает конкретную иконку, она не является доступной платформой.
            if (IsIconAvailable(icon, windows))
                surfaces.Add(icon);
        }
    }

    private static bool IsIconAvailable(DesktopSurface icon, IReadOnlyCollection<DesktopSurface> windows) =>
        !windows.Any(window => window.Bounds.IntersectsWith(icon.Bounds));

    private static List<DesktopSurface> EnumerateWindows(IntPtr excludedWindow)
    {
        var surfaces = new List<DesktopSurface>();
        EnumWindows((handle, _) =>
        {
            if (TryGetWindowSurface(handle, excludedWindow, out DesktopSurface surface))
                surfaces.Add(surface);
            return true;
        }, IntPtr.Zero);
        return surfaces;
    }

    private static bool IsUsableWindow(IntPtr handle, IntPtr excludedWindow)
    {
        if (handle == IntPtr.Zero || handle == excludedWindow || handle == GetShellWindow()
            || !IsWindow(handle) || !IsWindowVisible(handle) || IsIconic(handle)
            || (GetWindowLong(handle, ExtendedStyleIndex) & ToolWindowStyle) != 0)
            return false;

        string className = GetClassName(handle);
        if (className is "Shell_TrayWnd" or "Shell_SecondaryTrayWnd" or "Progman" or "WorkerW")
            return false;

        int cloaked = 0;
        return DwmGetWindowAttribute(handle, CloakedAttribute, ref cloaked, sizeof(int)) != 0 || cloaked == 0;
    }

    private static bool TryGetBounds(IntPtr handle, out Rectangle bounds)
    {
        NativeRect rect;
        if (DwmGetWindowAttribute(handle, ExtendedFrameBoundsAttribute, out rect,
                Marshal.SizeOf<NativeRect>()) != 0 && !GetWindowRect(handle, out rect))
        {
            bounds = Rectangle.Empty;
            return false;
        }

        bounds = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
        return bounds.Width > 0 && bounds.Height > 0;
    }

    private static string GetClassName(IntPtr handle)
    {
        var name = new StringBuilder(256);
        return GetClassName(handle, name, name.Capacity) == 0 ? string.Empty : name.ToString();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;
    }

    private delegate bool EnumWindowsCallback(IntPtr handle, IntPtr parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(IntPtr handle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr handle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr handle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr handle, out NativeRect rect);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr handle, int index);

    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr handle, StringBuilder className, int maxCount);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(
        IntPtr handle, int attribute, out NativeRect value, int valueSize);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(
        IntPtr handle, int attribute, ref int value, int valueSize);
}

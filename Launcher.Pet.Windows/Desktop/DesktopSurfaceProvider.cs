using static Launcher.Pet.Windows.Desktop.DesktopWindowApi;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace Launcher.Pet.Windows.Desktop;
internal sealed class DesktopSurfaceProvider
{
    private const int ExtendedStyleIndex = -20;
    private const int ToolWindowStyle = 0x00000080;
    private const int ExtendedFrameBoundsAttribute = 9;
    private const int CloakedAttribute = 14;
    private readonly DesktopIconSurfaceProvider _desktopIconProvider = new();
    private readonly Func<Rectangle?> _getPetGround;
    private List<DesktopSurface> _windows = new();
    private long _windowsAt;
    private IntPtr _excluded;
    internal DesktopSurfaceProvider(Func<Rectangle?> getPetGround) => _getPetGround = getPetGround;
    private DesktopSurface? GetPetGround() => _getPetGround() is Rectangle bounds ? new DesktopSurface(bounds, new DesktopSurfaceIdentity(DesktopSurfaceType.PetGround, IntPtr.Zero)) : null;
    internal IReadOnlyList<DesktopSurface> GetSurfaces(IntPtr excludedWindow, bool includeDesktop)
    {
        long now = Environment.TickCount64;
        if (includeDesktop && (now >= _windowsAt || _excluded != excludedWindow))
        {
            _windows = EnumerateWindows(excludedWindow);
            _windowsAt = now + 100;
            _excluded = excludedWindow;
        }
        var surfaces = includeDesktop ? new List<DesktopSurface>(_windows) : new List<DesktopSurface>();
        if (GetPetGround() is DesktopSurface ground)
            surfaces.Add(ground);
        if (!includeDesktop)
            return surfaces;
        surfaces.AddRange(_desktopIconProvider.GetSurfaces());
        foreach (Screen screen in Screen.AllScreens)
            surfaces.Add(GetTaskbarSurface(screen));
        return surfaces;
    }

    private static DesktopSurface GetTaskbarSurface(Screen screen)
    {
        Rectangle area = screen.WorkingArea;
        return new DesktopSurface(Rectangle.FromLTRB(area.Left, area.Bottom, area.Right, area.Bottom + 1), new DesktopSurfaceIdentity(DesktopSurfaceType.Taskbar, IntPtr.Zero, screen.DeviceName));
    }

    private static bool TryGetWindowSurface(IntPtr handle, IntPtr excludedWindow, out DesktopSurface surface)
    {
        surface = default;
        if (!IsUsableWindow(handle, excludedWindow) || !TryGetBounds(handle, out Rectangle bounds))
            return false;
        surface = new DesktopSurface(bounds, new DesktopSurfaceIdentity(DesktopSurfaceType.Window, handle));
        return true;
    }

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
        if (handle == IntPtr.Zero || handle == excludedWindow || handle == GetShellWindow() || !IsWindow(handle) || !IsWindowVisible(handle) || IsIconic(handle) || (GetWindowLong(handle, ExtendedStyleIndex) & ToolWindowStyle) != 0)
            return false;
        string className = ReadClassName(handle);
        if (className is "Shell_TrayWnd" or "Shell_SecondaryTrayWnd" or "Progman" or "WorkerW")
            return false;
        int cloaked = 0;
        return DwmGetWindowAttribute(handle, CloakedAttribute, ref cloaked, sizeof(int)) != 0 || cloaked == 0;
    }

    private static bool TryGetBounds(IntPtr handle, out Rectangle bounds)
    {
        NativeRect rect;
        if (DwmGetWindowAttribute(handle, ExtendedFrameBoundsAttribute, out rect, Marshal.SizeOf<NativeRect>()) != 0 && !GetWindowRect(handle, out rect))
        {
            bounds = Rectangle.Empty;
            return false;
        }

        bounds = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
        return bounds.Width > 0 && bounds.Height > 0;
    }

    private static string ReadClassName(IntPtr handle)
    {
        var name = new StringBuilder(256);
        return GetClassName(handle, name, name.Capacity) == 0 ? string.Empty : name.ToString();
    }
}

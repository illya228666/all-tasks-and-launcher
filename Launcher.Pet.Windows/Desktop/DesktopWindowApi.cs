using System.Runtime.InteropServices;
using System.Text;

namespace Launcher.Pet.Windows.Desktop;
internal static class DesktopWindowApi
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeRect
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;
    }

    internal delegate bool EnumWindowsCallback(IntPtr handle, IntPtr parameter);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWindow(IntPtr handle);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWindowVisible(IntPtr handle);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsIconic(IntPtr handle);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetWindowRect(IntPtr handle, out NativeRect rect);
    [DllImport("user32.dll")]
    internal static extern int GetWindowLong(IntPtr handle, int index);
    [DllImport("user32.dll")]
    internal static extern IntPtr GetShellWindow();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern int GetClassName(IntPtr handle, StringBuilder className, int maxCount);
    [DllImport("dwmapi.dll")]
    internal static extern int DwmGetWindowAttribute(IntPtr handle, int attribute, out NativeRect value, int valueSize);
    [DllImport("dwmapi.dll")]
    internal static extern int DwmGetWindowAttribute(IntPtr handle, int attribute, ref int value, int valueSize);
}

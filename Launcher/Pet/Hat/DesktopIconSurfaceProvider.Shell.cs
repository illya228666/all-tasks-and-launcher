using System.Runtime.InteropServices;

namespace Launcher.Pet.Hat;

internal sealed partial class DesktopIconSurfaceProvider
{
    private static IFolderView? GetDesktopFolderView(IntPtr listView)
    {
        object? shellWindows = null;
        object? desktop = null;
        IShellBrowser? browser = null;
        IShellView? shellView = null;
        try
        {
            // Официальный путь к desktop view: IShellWindows -> browser -> view.
            // https://devblogs.microsoft.com/oldnewthing/20130318-00/?p=4933
            shellWindows = Activator.CreateInstance(Type.GetTypeFromCLSID(
                new Guid("9BA05972-F6A8-11CF-A442-00A0C90A8F39"), throwOnError: true)!);
            if (shellWindows is null)
                return null;

            object location = 0; // CSIDL_DESKTOP
            object empty = null!; // VT_EMPTY
            int desktopWindow;
            desktop = ((dynamic)shellWindows).FindWindowSW(
                ref location, ref empty, 8, out desktopWindow, 1); // SWC_DESKTOP, SWFO_NEEDDISPATCH
            if (desktop is null)
                return null;

            var service = new Guid("4C96BE40-915C-11CF-99D3-00AA004AE837"); // SID_STopLevelBrowser
            Guid browserId = typeof(IShellBrowser).GUID;
            if (((IShellServiceProvider)desktop).QueryService(ref service, ref browserId, out browser) < 0
                || browser is null || browser.QueryActiveShellView(out shellView) < 0
                || shellView is null || shellView.GetWindow(out IntPtr viewWindow) < 0
                || !IsChild(viewWindow, listView))
                return null;

            var folderView = (IFolderView)shellView;
            shellView = null; // Владение RCW передаётся вызывающему Refresh.
            return folderView;
        }
        finally
        {
            if (shellView is not null)
                Marshal.ReleaseComObject(shellView);
            if (browser is not null)
                Marshal.ReleaseComObject(browser);
            if (desktop is not null)
                Marshal.ReleaseComObject(desktop);
            if (shellWindows is not null)
                Marshal.ReleaseComObject(shellWindows);
        }
    }

    private static string? GetItemKey(IFolderView view, int index)
    {
        IntPtr pidl = IntPtr.Zero;
        try
        {
            if (view.Item(index, out pidl) < 0 || pidl == IntPtr.Zero)
                return null;
            uint size = ILGetSize(pidl);
            if (size <= 2 || size > ushort.MaxValue)
                return null;
            var bytes = new byte[(int)size];
            Marshal.Copy(pidl, bytes, 0, bytes.Length);
            return Convert.ToBase64String(bytes);
        }
        finally
        {
            if (pidl != IntPtr.Zero)
                Marshal.FreeCoTaskMem(pidl);
        }
    }

    [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellServiceProvider
    {
        [PreserveSig]
        int QueryService(ref Guid service, ref Guid interfaceId, out IShellBrowser browser);
    }

    [ComImport, Guid("000214E2-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellBrowser
    {
        // Предшествующие методы сохраняют порядок слотов native vtable.
        [PreserveSig] int GetWindow(out IntPtr window);
        [PreserveSig] int ContextSensitiveHelp([MarshalAs(UnmanagedType.Bool)] bool enterMode);
        [PreserveSig] int InsertMenusSB(IntPtr menu, IntPtr widths);
        [PreserveSig] int SetMenuSB(IntPtr menu, IntPtr oleMenu, IntPtr activeWindow);
        [PreserveSig] int RemoveMenusSB(IntPtr menu);
        [PreserveSig] int SetStatusTextSB([MarshalAs(UnmanagedType.LPWStr)] string text);
        [PreserveSig] int EnableModelessSB([MarshalAs(UnmanagedType.Bool)] bool enable);
        [PreserveSig] int TranslateAcceleratorSB(IntPtr message, ushort id);
        [PreserveSig] int BrowseObject(IntPtr pidl, uint flags);
        [PreserveSig] int GetViewStateStream(uint mode, out IntPtr stream);
        [PreserveSig] int GetControlWindow(uint id, out IntPtr window);
        [PreserveSig] int SendControlMsg(uint id, uint message, IntPtr wParam, IntPtr lParam, out IntPtr result);
        [PreserveSig] int QueryActiveShellView(out IShellView view);
    }

    [ComImport, Guid("000214E3-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellView
    {
        [PreserveSig] int GetWindow(out IntPtr window);
    }

    [ComImport, Guid("CDE725B0-CCC9-4519-917E-325D72FAB4CE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFolderView
    {
        [PreserveSig] int GetCurrentViewMode(out uint mode);
        [PreserveSig] int SetCurrentViewMode(uint mode);
        [PreserveSig] int GetFolder(ref Guid interfaceId, out IntPtr folder);
        [PreserveSig] int Item(int index, out IntPtr pidl);
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsChild(IntPtr parent, IntPtr child);

    [DllImport("shell32.dll")]
    private static extern uint ILGetSize(IntPtr pidl);
}

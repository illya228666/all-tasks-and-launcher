using static Launcher.Pet.Windows.Desktop.DesktopIconApi;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Launcher.Pet.Windows.Desktop;
// Получает реальные позиции значков рабочего стола из Explorer ListView.
// Геометрия кэшируется ненадолго, чтобы не читать память Explorer каждый physics tick.
internal sealed class DesktopIconSurfaceProvider
{
    private const float IconCollisionWidthRatio = 0.35f;
    private const int SnapshotLifetimeMs = 500; // Срок снимка значков / Lebensdauer der Symbolaufnahme.
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
        if (_refreshing || (_lastRefresh != 0 && now - _lastRefresh < SnapshotLifetimeMs))
        {
            if (!IsWindowVisible(_listView) || GetWindowThreadProcessId(_listView, out uint processId) == 0 || processId != _processId)
                _cachedSurfaces = Array.Empty<DesktopSurface>();
            return _cachedSurfaces;
        }

        _lastRefresh = now;
        IntPtr listView = FindDesktopListView();
        if (listView != _listView)
            _cachedSurfaces = Array.Empty<DesktopSurface>();
        _listView = listView;
        if (_listView == IntPtr.Zero || !IsWindowVisible(_listView) || GetWindowThreadProcessId(_listView, out uint currentProcessId) == 0 || currentProcessId == 0)
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
            var duplicates = surfaces.GroupBy(surface => surface.Identity).Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet();
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

    private static void Refresh(List<DesktopSurface> target, IntPtr listView, uint processId)
    {
        IFolderView? view = GetDesktopFolderView(listView);
        if (view is null)
            return;
        IntPtr process = IntPtr.Zero;
        IntPtr remoteRect = IntPtr.Zero;
        try
        {
            process = OpenProcess(ProcessVmOperation | ProcessVmRead | ProcessVmWrite, false, processId);
            if (process == IntPtr.Zero)
                return;
            nuint rectSize = (nuint)Marshal.SizeOf<NativeRect>();
            remoteRect = VirtualAllocEx(process, IntPtr.Zero, rectSize, MemCommit | MemReserve, PageReadWrite);
            if (remoteRect == IntPtr.Zero)
                return;
            int itemCount = SendMessage(listView, ListViewGetItemCount, IntPtr.Zero, IntPtr.Zero).ToInt32();
            for (int itemIndex = 0; itemIndex < itemCount; itemIndex++)
            {
                string? itemKey = GetItemKey(view, itemIndex);
                if (itemKey is null)
                    continue;
                var rect = new NativeRect
                {
                    Left = ListViewIconBounds
                };
                if (!WriteProcessMemory(process, remoteRect, ref rect, rectSize, out _) || SendMessage(listView, ListViewGetItemRect, new IntPtr(itemIndex), remoteRect) == IntPtr.Zero || !ReadProcessMemory(process, remoteRect, out rect, rectSize, out _))
                    continue;
                var topLeft = new NativePoint
                {
                    X = rect.Left,
                    Y = rect.Top
                };
                var bottomRight = new NativePoint
                {
                    X = rect.Right,
                    Y = rect.Bottom
                };
                if (!ClientToScreen(listView, ref topLeft) || !ClientToScreen(listView, ref bottomRight))
                    continue;
                Rectangle bounds = Rectangle.FromLTRB(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
                if (bounds.Width <= 0 || bounds.Height <= 0)
                    continue;
                int horizontalInset = (int)Math.Round(bounds.Width * (1f - IconCollisionWidthRatio) / 2f);
                Rectangle collisionBounds = Rectangle.FromLTRB(bounds.Left + horizontalInset, bounds.Top, bounds.Right - horizontalInset, bounds.Bottom);
                // Индекс служит только адресом чтения. Если view перестроился
                // во время чтения геометрии, результат не становится опорой.
                if (itemKey != GetItemKey(view, itemIndex))
                    continue;
                Rectangle labelBounds = Rectangle.Empty;
                rect = new NativeRect { Left = 2 }; // LVIR_LABEL
                if (WriteProcessMemory(process, remoteRect, ref rect, rectSize, out _)
                    && SendMessage(listView, ListViewGetItemRect, new IntPtr(itemIndex), remoteRect) != IntPtr.Zero
                    && ReadProcessMemory(process, remoteRect, out rect, rectSize, out _))
                {
                    var labelStart = new NativePoint { X = rect.Left, Y = rect.Top };
                    var labelEnd = new NativePoint { X = rect.Right, Y = rect.Bottom };
                    if (ClientToScreen(listView, ref labelStart) && ClientToScreen(listView, ref labelEnd))
                        labelBounds = Rectangle.FromLTRB(labelStart.X, labelStart.Y, labelEnd.X, labelEnd.Y);
                }
                if (itemKey == GetItemKey(view, itemIndex))
                    target.Add(new DesktopSurface(collisionBounds, new DesktopSurfaceIdentity(DesktopSurfaceType.DesktopIcon, listView, $"{processId}:{itemKey}"), bounds, labelBounds));
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

        return shellView == IntPtr.Zero ? IntPtr.Zero : FindWindowEx(shellView, IntPtr.Zero, "SysListView32", null);
    }

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
            shellWindows = Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("9BA05972-F6A8-11CF-A442-00A0C90A8F39"), throwOnError: true)!);
            if (shellWindows is null)
                return null;
            object location = 0; // CSIDL_DESKTOP
            object empty = null!; // VT_EMPTY
            int desktopWindow;
            desktop = ((dynamic)shellWindows).FindWindowSW(ref location, ref empty, 8, out desktopWindow, 1); // SWC_DESKTOP, SWFO_NEEDDISPATCH
            if (desktop is null)
                return null;
            var service = new Guid("4C96BE40-915C-11CF-99D3-00AA004AE837"); // SID_STopLevelBrowser
            Guid browserId = typeof(IShellBrowser).GUID;
            if (((IShellServiceProvider)desktop).QueryService(ref service, ref browserId, out browser) < 0 || browser is null || browser.QueryActiveShellView(out shellView) < 0 || shellView is null || shellView.GetWindow(out IntPtr viewWindow) < 0 || !IsChild(viewWindow, listView))
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
}

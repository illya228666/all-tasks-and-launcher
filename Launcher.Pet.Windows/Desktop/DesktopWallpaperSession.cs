using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace Launcher.Pet.Windows.Desktop;

// The journal is written before changing Windows settings so a later launch can undo a crash.
public sealed class DesktopWallpaperSession
{
    private readonly string _art = Path.Combine(AppContext.BaseDirectory, "Resources", "ruins", "wallpaper.png");
    private readonly string _journal = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "zahlen-launcher", "ruins-wallpaper.json");
    private readonly string _backup = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "zahlen-launcher", "ruins-wallpaper-backup");

    public string? Recover() => Restore();

    public string? Activate()
    {
        if (File.Exists(_journal)) return "Hintergrund wurde nicht geändert: frühere Einstellungen müssen noch wiederhergestellt werden.";
        if (!File.Exists(_art)) return "Hintergrundbild der Ruinen fehlt.";
        IDesktopWallpaper? desktop = null;
        try
        {
            desktop = CreateDesktop();
            WallpaperSnapshot original = Capture(desktop);
            Save(original);
            Backup(original);
            Save(original);
            Check(desktop.SetWallpaper(null, _art));
            Check(desktop.SetPosition(4)); // DWPOS_FILL
            Check(desktop.GetBackgroundColor(out uint appliedColor));
            original.AppliedColor = appliedColor;
            foreach (string id in original.Monitors.Keys)
            {
                Check(desktop.GetWallpaper(id, out string applied));
                original.Applied[id] = applied;
            }
            Save(original);
            return null;
        }
        catch (Exception error) when (error is ExternalException or IOException or UnauthorizedAccessException or JsonException or InvalidDataException or ArgumentException)
        {
            string? rollback = Restore();
            return "Hintergrund wurde nicht geändert: " + error.Message + (rollback is null ? "" : " | " + rollback);
        }
        finally
        {
            if (desktop is not null) Marshal.ReleaseComObject(desktop);
        }
    }

    public string? Restore()
    {
        if (!File.Exists(_journal)) return null;
        IDesktopWallpaper? desktop = null;
        try
        {
            WallpaperSnapshot saved = JsonSerializer.Deserialize<WallpaperSnapshot>(File.ReadAllText(_journal))
                ?? throw new InvalidDataException("Leerer Hintergrund-Snapshot.");
            if (saved.Monitors is null || saved.Applied is null || saved.Backups is null || saved.SlideshowItems is null
                || saved.Monitors.Count == 0 || saved.Art != _art)
                throw new InvalidDataException("Ungültiger Hintergrund-Snapshot.");
            desktop = CreateDesktop();
            var owned = new List<string>();
            bool originalStillShown = true;
            foreach (string id in saved.Monitors.Keys)
            {
                Check(desktop.GetWallpaper(id, out string current));
                if (Same(current, saved.Applied.GetValueOrDefault(id)) || Same(current, saved.Art))
                    owned.Add(id);
                originalStillShown &= Same(current, saved.Monitors[id]);
            }
            if (owned.Count == 0)
            {
                if (originalStillShown)
                    RestorePresentation(desktop, saved);
                File.Delete(_journal);
                ClearBackup();
                return null;
            }
            bool allOwned = owned.Count == saved.Monitors.Count;
            if (allOwned && saved.SlideshowItems.Count > 0)
            {
                IShellItemArray items = CreateItems(saved.SlideshowItems);
                try
                {
                    Check(desktop.SetSlideshow(items));
                    Check(desktop.SetSlideshowOptions(saved.SlideshowOptions, saved.SlideshowTick));
                }
                finally { Marshal.ReleaseComObject(items); }
            }
            else
            {
                foreach (string id in owned)
                {
                    string path = OriginalImage(saved, id);
                    if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                        throw new InvalidDataException("Das vorherige Hintergrundbild ist nicht mehr verfügbar.");
                    Check(desktop.SetWallpaper(id, path));
                }
            }
            if (allOwned)
                RestorePresentation(desktop, saved);
            File.Delete(_journal);
            ClearBackup();
            return allOwned ? null : "Ein Hintergrund wurde außerhalb des Launchers geändert; seine Einstellungen blieben erhalten.";
        }
        catch (Exception error) when (error is ExternalException or IOException or UnauthorizedAccessException or JsonException or InvalidDataException or ArgumentException or OutOfMemoryException)
        {
            return "Hintergrund konnte nicht wiederhergestellt werden: " + error.Message;
        }
        finally
        {
            if (desktop is not null) Marshal.ReleaseComObject(desktop);
        }
    }

    private WallpaperSnapshot Capture(IDesktopWallpaper desktop)
    {
        var snapshot = new WallpaperSnapshot { Art = _art };
        Check(desktop.GetMonitorDevicePathCount(out uint count));
        if (count == 0) throw new InvalidDataException("Keine Monitore gefunden.");
        Check(desktop.GetStatus(out uint status));
        bool slideshow = (status & 2) != 0; // DSS_SLIDESHOW
        for (uint i = 0; i < count; i++)
        {
            Check(desktop.GetMonitorDevicePathAt(i, out string id));
            Check(desktop.GetWallpaper(id, out string path));
            if (string.IsNullOrWhiteSpace(id) || (!slideshow && (string.IsNullOrWhiteSpace(path) || !File.Exists(path))))
                throw new InvalidDataException("Der bisherige Hintergrund kann nicht sicher gespeichert werden.");
            snapshot.Monitors.Add(id, path);
        }
        Check(desktop.GetPosition(out uint position));
        Check(desktop.GetBackgroundColor(out uint color));
        snapshot.Position = position;
        snapshot.Color = color;
        snapshot.AppliedPosition = 4;
        snapshot.AppliedColor = color;
        if (slideshow)
        {
            Check(desktop.GetSlideshow(out IShellItemArray items));
            try
            {
                Check(items.GetCount(out uint itemCount));
                if (itemCount == 0) throw new InvalidDataException("Leere Diashow.");
                for (uint i = 0; i < itemCount; i++)
                {
                    Check(items.GetItemAt(i, out IShellItem item));
                    try
                    {
                        Check(item.GetDisplayName(0x80058000, out IntPtr name)); // SIGDN_FILESYSPATH
                        try
                        {
                            string path = Marshal.PtrToStringUni(name) ?? "";
                            if (!File.Exists(path) && !Directory.Exists(path))
                                throw new InvalidDataException("Diashow-Quelle ist nicht verfügbar.");
                            snapshot.SlideshowItems.Add(path);
                        }
                        finally { Marshal.FreeCoTaskMem(name); }
                    }
                    finally { Marshal.ReleaseComObject(item); }
                }
            }
            finally { Marshal.ReleaseComObject(items); }
            Check(desktop.GetSlideshowOptions(out uint options, out uint tick));
            snapshot.SlideshowOptions = options;
            snapshot.SlideshowTick = tick;
        }
        return snapshot;
    }

    private void Save(WallpaperSnapshot snapshot)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_journal)!);
        string temporary = _journal + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(snapshot));
            if (File.Exists(_journal)) File.Replace(temporary, _journal, null);
            else File.Move(temporary, _journal);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private void Backup(WallpaperSnapshot snapshot)
    {
        Directory.CreateDirectory(_backup);
        int index = 0;
        foreach (var (id, path) in snapshot.Monitors)
        {
            if (string.IsNullOrWhiteSpace(path)) continue;
            string copy = Path.Combine(_backup, $"original-{index++}{Path.GetExtension(path)}");
            File.Copy(path, copy, true);
            snapshot.Backups[id] = copy;
        }
    }

    private static void RestorePresentation(IDesktopWallpaper desktop, WallpaperSnapshot saved)
    {
        Check(desktop.GetPosition(out uint position));
        if (position == saved.AppliedPosition && position != saved.Position)
            Check(desktop.SetPosition(saved.Position));
        Check(desktop.GetBackgroundColor(out uint color));
        if (color == saved.AppliedColor && color != saved.Color)
            Check(desktop.SetBackgroundColor(saved.Color));
    }

    private string OriginalImage(WallpaperSnapshot saved, string id)
    {
        string path = saved.Monitors[id];
        if (!saved.Backups.TryGetValue(id, out string? copy) || !File.Exists(copy)) return path;
        string backupRoot = Path.GetFullPath(_backup) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(copy).StartsWith(backupRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Ungültiger Sicherungspfad.");
        if (!File.Exists(path)) return RecoveredCopy(copy);
        using var original = File.OpenRead(path);
        using var backup = File.OpenRead(copy);
        if (SHA256.HashData(original).SequenceEqual(SHA256.HashData(backup))) return path;
        return RecoveredCopy(copy);
    }

    private string RecoveredCopy(string copy)
    {
        string recovered = Path.Combine(Path.GetDirectoryName(_backup)!, "recovered-wallpaper-" + Guid.NewGuid().ToString("N") + ".png");
        using var image = System.Drawing.Image.FromFile(copy);
        image.Save(recovered, System.Drawing.Imaging.ImageFormat.Png);
        return recovered;
    }

    private void ClearBackup()
    {
        if (!Directory.Exists(_backup)) return;
        // Fixed application-owned directory, resolved before recursive removal.
        string root = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "zahlen-launcher"));
        string target = Path.GetFullPath(_backup);
        if (!target.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Ungültiges Sicherungsverzeichnis.");
        try { Directory.Delete(target, true); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            System.Diagnostics.Debug.WriteLine(error);
        }
    }

    private static IShellItemArray CreateItems(IEnumerable<string> paths)
    {
        var ids = new List<IntPtr>();
        try
        {
            foreach (string path in paths)
            {
                Check(SHParseDisplayName(path, IntPtr.Zero, out IntPtr id, 0, out _));
                ids.Add(id);
            }
            Check(SHCreateShellItemArrayFromIDLists((uint)ids.Count, ids.ToArray(), out IShellItemArray items));
            return items;
        }
        finally { foreach (IntPtr id in ids) Marshal.FreeCoTaskMem(id); }
    }

    private static IDesktopWallpaper CreateDesktop() =>
        (IDesktopWallpaper)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("C2CF3110-460E-4FC1-B9D0-8A1C0C9CC4BD"), throwOnError: true)!)!;
    private static bool Same(string? a, string? b) => !string.IsNullOrWhiteSpace(a) && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    private static void Check(int result) { if (result < 0) Marshal.ThrowExceptionForHR(result); }

    private sealed class WallpaperSnapshot
    {
        public WallpaperSnapshot() { }
        public string Art { get; set; } = "";
        public Dictionary<string, string> Monitors { get; set; } = new();
        public Dictionary<string, string> Applied { get; set; } = new();
        public Dictionary<string, string> Backups { get; set; } = new();
        public uint Position { get; set; }
        public uint Color { get; set; }
        public uint AppliedPosition { get; set; }
        public uint AppliedColor { get; set; }
        public List<string> SlideshowItems { get; set; } = new();
        public uint SlideshowOptions { get; set; }
        public uint SlideshowTick { get; set; }
    }

    [ComImport, Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDesktopWallpaper
    {
        [PreserveSig] int SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorId, [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);
        [PreserveSig] int GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorId, [MarshalAs(UnmanagedType.LPWStr)] out string wallpaper);
        [PreserveSig] int GetMonitorDevicePathAt(uint index, [MarshalAs(UnmanagedType.LPWStr)] out string id);
        [PreserveSig] int GetMonitorDevicePathCount(out uint count);
        [PreserveSig] int GetMonitorRect([MarshalAs(UnmanagedType.LPWStr)] string id, IntPtr rect);
        [PreserveSig] int SetBackgroundColor(uint color);
        [PreserveSig] int GetBackgroundColor(out uint color);
        [PreserveSig] int SetPosition(uint position);
        [PreserveSig] int GetPosition(out uint position);
        [PreserveSig] int SetSlideshow(IShellItemArray items);
        [PreserveSig] int GetSlideshow(out IShellItemArray items);
        [PreserveSig] int SetSlideshowOptions(uint options, uint tick);
        [PreserveSig] int GetSlideshowOptions(out uint options, out uint tick);
        [PreserveSig] int AdvanceSlideshow(IntPtr monitorId, uint direction);
        [PreserveSig] int GetStatus(out uint status);
    }

    [ComImport, Guid("B63EA76D-1F85-456F-A19C-48159EFA858B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemArray
    {
        void BindToHandler(IntPtr bindContext, IntPtr handler, IntPtr iid, IntPtr result);
        void GetPropertyStore(uint flags, IntPtr iid, IntPtr result);
        void GetPropertyDescriptionList(IntPtr key, IntPtr iid, IntPtr result);
        void GetAttributes(uint flags, uint mask, IntPtr attributes);
        [PreserveSig] int GetCount(out uint count);
        [PreserveSig] int GetItemAt(uint index, out IShellItem item);
    }

    [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
        void BindToHandler(IntPtr bindContext, IntPtr handler, IntPtr iid, IntPtr result);
        void GetParent(IntPtr parent);
        [PreserveSig] int GetDisplayName(uint nameType, out IntPtr name);
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHParseDisplayName(string name, IntPtr bindContext, out IntPtr itemId, uint flags, out uint attributes);
    [DllImport("shell32.dll")]
    private static extern int SHCreateShellItemArrayFromIDLists(uint count, [In] IntPtr[] itemIds, out IShellItemArray items);
}

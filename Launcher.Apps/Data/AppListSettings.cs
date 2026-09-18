namespace Launcher.Apps.Data;
// RU: Копия для сохранения, не изменяемое состояние списка. DE: Speicherkopie, kein Listenbesitz.
public sealed class AppListSettings
{
    public AppFilter Filter { get; set; } = new();
    public List<string> Favorites { get; set; } = new();
    public Dictionary<string, AppUsage> Usage { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

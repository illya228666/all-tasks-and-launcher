using Launcher.Domain;

namespace Launcher.Application;

/// <summary>
/// RU: Преобразует SortMode в отображаемый текст и обратно.
/// DE: Wandelt SortMode in Anzeige-Text und zurueck um.
/// </summary>
public static class SortModeText
{
    #region [RU] Константы | [DE] Konstanten

    public const string ByCategory = "Kategorie, Name";
    public const string ByName = "Name (A-Z)";
    public const string ByRecent = "Zuletzt gestartet";
    public const string ByMostUsed = "Am haeufigsten";
    public const string ByFavorites = "Favoriten zuerst";

    #endregion

    #region [RU] Бизнес-логика | [DE] Fachlogik

    /// <summary>
    /// RU: Преобразует enum в строку для UI.
    /// DE: Wandelt enum in UI-Text um.
    /// </summary>
    public static string ToDisplayText(SortMode mode)
    {
        return mode switch
        {
            SortMode.ByName => ByName,
            SortMode.ByRecent => ByRecent,
            SortMode.ByMostUsed => ByMostUsed,
            SortMode.ByFavorites => ByFavorites,
            _ => ByCategory
        };
    }

    /// <summary>
    /// RU: Пробует разобрать UI-строку в enum.
    /// DE: Versucht, UI-Text in enum zu parsen.
    /// </summary>
    public static bool TryParse(string? text, out SortMode mode)
    {
        mode = text switch
        {
            ByName => SortMode.ByName,
            ByRecent => SortMode.ByRecent,
            ByMostUsed => SortMode.ByMostUsed,
            ByFavorites => SortMode.ByFavorites,
            _ => SortMode.ByCategory
        };

        return !string.IsNullOrWhiteSpace(text);
    }

    #endregion
}

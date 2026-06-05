namespace Launcher.Domain;

/// <summary>
/// RU: Режимы сортировки списка приложений.
/// DE: Sortiermodi fuer die Anwendungsliste.
/// </summary>
public enum SortMode
{
    /// <summary>RU: По категории, затем по имени. DE: Nach Kategorie, dann Name.</summary>
    ByCategory = 0,

    /// <summary>RU: По имени (A-Z). DE: Nach Name (A-Z).</summary>
    ByName = 1,

    /// <summary>RU: Сначала недавно запущенные. DE: Zuletzt gestartete zuerst.</summary>
    ByRecent = 2,

    /// <summary>RU: Сначала часто запускаемые. DE: Am haeufigsten gestartete zuerst.</summary>
    ByMostUsed = 3,

    /// <summary>RU: Сначала избранные. DE: Favoriten zuerst.</summary>
    ByFavorites = 4
}

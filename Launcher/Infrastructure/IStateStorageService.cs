using Launcher.Domain;

namespace Launcher.Infrastructure;

/// <summary>
/// RU: Контракт чтения/записи состояния Launcher.
/// DE: Vertrag zum Lesen/Schreiben des Launcher-Zustands.
/// </summary>
public interface IStateStorageService
{
    /// <summary>
    /// RU: Читает состояние из файла. Если файл недоступен, возвращает состояние по умолчанию.
    /// DE: Liest den Zustand aus einer Datei. Bei Fehlern wird ein Default-Zustand geliefert.
    /// </summary>
    LauncherState Load(string statePath);

    /// <summary>
    /// RU: Сохраняет состояние в файл.
    /// DE: Speichert den Zustand in eine Datei.
    /// </summary>
    void Save(string statePath, LauncherState state);
}

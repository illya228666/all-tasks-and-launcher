using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Launcher.Domain;

namespace Launcher.Infrastructure;

/// <summary>
/// RU: JSON-реализация хранения состояния Launcher.
/// DE: JSON-Implementierung fuer die Speicherung des Launcher-Zustands.
/// </summary>
public sealed class JsonStateStorageService : IStateStorageService
{
    #region [RU] Поля | [DE] Felder

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    #endregion

    #region [RU] Бизнес-логика | [DE] Fachlogik

    /// <inheritdoc />
    public LauncherState Load(string statePath)
    {
        if (!File.Exists(statePath))
        {
            return new LauncherState();
        }

        try
        {
            LauncherState? state = JsonSerializer.Deserialize<LauncherState>(File.ReadAllText(statePath), JsonOptions);
            return state ?? new LauncherState();
        }
        catch
        {
            return new LauncherState();
        }
    }

    /// <inheritdoc />
    public void Save(string statePath, LauncherState state)
    {
        try
        {
            string? folderPath = Path.GetDirectoryName(statePath);
            if (!string.IsNullOrWhiteSpace(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            File.WriteAllText(statePath, JsonSerializer.Serialize(state, JsonOptions));
        }
        catch
        {
            // RU: Ошибки сохранения не должны падать в UI.
            // DE: Speicherfehler sollen die UI nicht abstuerzen lassen.
        }
    }

    #endregion
}

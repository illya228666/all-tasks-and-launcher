using System.Text.Json;
using System.Text.Json.Serialization;

namespace Launcher.Settings;
internal sealed class SettingsFile
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };
    private readonly string _path;
    private bool _canWrite = true;
    internal SettingsFile(string path) => _path = path;
    internal SettingsReadResult Read()
    {
        if (!File.Exists(_path))
            return new(new());
        try
        {
            LauncherSettings settings = JsonSerializer.Deserialize<LauncherSettings>(File.ReadAllText(_path), JsonOptions) ?? throw new JsonException("Leere Einstellungen.");
            Validate(settings);
            return new(settings);
        }
        catch (Exception error) when (error is JsonException or InvalidDataException)
        {
            string backup = _path + ".broken-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff");
            try
            {
                File.Copy(_path, backup);
            }
            catch (Exception copyError) when (copyError is IOException or UnauthorizedAccessException)
            {
                _canWrite = false;
                return new(new(), "Einstellungen sind beschaedigt. Original bleibt unveraendert; Speichern gesperrt: " + copyError.Message);
            }

            return new(new(), "Einstellungen wurden zurueckgesetzt. Original: " + backup);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            _canWrite = false;
            return new(new(), "Einstellungen konnten nicht gelesen werden; Speichern gesperrt: " + error.Message);
        }
    }

    private static void Validate(LauncherSettings settings)
    {
        if (settings.Apps is null || settings.Apps.Filter is null || settings.Apps.Filter.Search is null || !Enum.IsDefined(settings.Apps.Filter.Sort) || settings.Apps.Favorites is null || settings.Apps.Usage is null || settings.Apps.Favorites.Any(string.IsNullOrWhiteSpace) || settings.Apps.Usage.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || pair.Value is null || pair.Value.Count < 0) || settings.Apps.Usage.Keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != settings.Apps.Usage.Count)
            throw new InvalidDataException("Ungueltige Einstellungen.");
    }

    internal string? Write(LauncherSettings settings)
    {
        if (!_canWrite)
            return "Speichern gesperrt: Die urspruenglichen Einstellungen konnten nicht sicher gelesen werden.";
        string temporary = _path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(temporary, JsonSerializer.Serialize(settings, JsonOptions));
            if (File.Exists(_path))
                File.Replace(temporary, _path, null);
            else
                File.Move(temporary, _path);
            return null;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        {
            return "Einstellungen konnten nicht gespeichert werden: " + error.Message;
        }
        finally
        {
            try
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                System.Diagnostics.Debug.WriteLine(error);
            }
        }
    }
}

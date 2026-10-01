using System.Text.Json;
using Launcher.Pet.Life;

namespace Launcher.Pet.Windows.Life;

internal sealed class LifeSaveFile
{
    private readonly string _path;
    private bool _canWrite = true;
    internal LifeSaveFile(string path) => _path = path;
    internal PetLifeSave? Read(out string? warning)
    {
        warning = null;
        // File.Exists masks access errors; use an actual read so unreadable saves
        // cannot be mistaken for absent worlds and subsequently overwritten.
        try
        {
            var saved = JsonSerializer.Deserialize<PetLifeSave>(File.ReadAllText(_path)) ?? throw new JsonException("Empty life checkpoint.");
            Validate(saved);
            return saved;
        }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
        catch (Exception error) when (error is JsonException or InvalidDataException or NotSupportedException)
        {
            string backup = _path + ".broken-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff");
            try { File.Copy(_path, backup); warning = "Die gespeicherte Welt wurde gesichert: " + backup; }
            catch (Exception copyError) when (copyError is IOException or UnauthorizedAccessException)
            {
                _canWrite = false;
                warning = "Welt konnte nicht gesichert werden; Speichern gesperrt: " + copyError.Message;
            }
            return null;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            _canWrite = false;
            warning = "Welt konnte nicht gelesen werden; Speichern gesperrt: " + error.Message;
            return null;
        }
    }
    internal string? Write(PetLifeSave saved)
    {
        if (!_canWrite) return "Speichern der Welt gesperrt: Die ursprüngliche Datei konnte nicht sicher gelesen werden.";
        string temporary = _path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            Validate(saved);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
            File.WriteAllText(temporary, JsonSerializer.Serialize(saved));
            if (File.Exists(_path)) File.Replace(temporary, _path, null);
            else File.Move(temporary, _path);
            return null;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        { return "Welt konnte nicht gespeichert werden: " + error.Message; }
        finally
        {
            try { if (File.Exists(temporary)) File.Delete(temporary); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { System.Diagnostics.Debug.WriteLine(error); }
        }
    }
    private static void Validate(PetLifeSave saved)
    {
        if (saved.Version != 1 || saved.Geometry is null || saved.Life is null || saved.Life.Sites is null
            || saved.Life.Sites.Count != 12 || !float.IsFinite(saved.PetXFraction) || saved.PetXFraction is < 0 or > 1
            || string.IsNullOrEmpty(saved.PetPlatform)) throw new InvalidDataException("Invalid life checkpoint.");
        LifeGeometry.Validate(saved.Geometry);
        LifeWorld.Validate(saved.Life);
    }
}

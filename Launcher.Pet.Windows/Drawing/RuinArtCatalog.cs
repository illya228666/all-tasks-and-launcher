using System.Drawing.Imaging;
using System.Text.Json;

namespace Launcher.Pet.Windows.Drawing;

internal sealed record RuinArtDefinition(string Id, string File, float ContactY = 0, bool Modular = false);
internal sealed record RuinArt(Bitmap Image, float ContactY, bool Modular);
internal sealed class RuinArtCatalog : IDisposable
{
    private readonly Dictionary<string, RuinArt> _art = new();
    internal RuinArtCatalog(string? prefix = null)
    {
        string root = Path.Combine(AppContext.BaseDirectory, "Resources", "ruins", "gothic");
        string catalog = Path.Combine(root, "catalog.json");
        if (!File.Exists(catalog)) return;
        var definitions = JsonSerializer.Deserialize<RuinArtDefinition[]>(File.ReadAllText(catalog), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("Empty gothic art catalog.");
        try
        {
            foreach (var definition in definitions)
            {
                if (definition is null || string.IsNullOrWhiteSpace(definition.Id) || string.IsNullOrWhiteSpace(definition.File)
                    || !float.IsFinite(definition.ContactY) || definition.ContactY < 0)
                    throw new InvalidDataException("Invalid gothic asset definition.");
                if (prefix is not null && !definition.Id.StartsWith(prefix, StringComparison.Ordinal)) continue;
                string path = Path.GetFullPath(Path.Combine(root, definition.File));
                if (!path.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                    || _art.ContainsKey(definition.Id)) throw new InvalidDataException("Unsafe or duplicate gothic asset.");
                using var source = new Bitmap(path);
                if (definition.ContactY > source.Height) throw new InvalidDataException("Invalid asset contact line.");
                var bitmap = source.Clone(new Rectangle(Point.Empty, source.Size), PixelFormat.Format32bppPArgb);
                _art.Add(definition.Id, new(bitmap, definition.ContactY, definition.Modular));
            }
        }
        catch { Dispose(); throw; }
    }
    internal RuinArt? Find(string id) => _art.GetValueOrDefault(id);
    internal bool Available => _art.Count > 0;
    internal static void DrawModule(Graphics g, RuinArt art, float left, float contact, float width, float height, ImageAttributes? attributes = null)
    {
        var image = art.Image;
        float scale = Math.Min(height / image.Height, art.Modular ? width / (image.Width * .5f) : float.MaxValue);
        height = image.Height * scale;
        float capSource = image.Width * .25f;
        float capWidth = capSource * scale;
        float top = contact - art.ContactY * scale;
        void Draw(RectangleF destination, RectangleF source)
        {
            if (attributes is null) g.DrawImage(image, destination, source, GraphicsUnit.Pixel);
            else g.DrawImage(image, Rectangle.Round(destination), source.X, source.Y, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
        }
        if (!art.Modular) { Draw(new(left, top, width, height), new(0, 0, image.Width, image.Height)); return; }
        Draw(new(left, top, capWidth, height), new(0, 0, capSource, image.Height));
        Draw(new(left + width - capWidth, top, capWidth, height), new(image.Width - capSource, 0, capSource, image.Height));
        float repeatSource = image.Width * .25f, repeatWidth = repeatSource * scale;
        for (float x = left + capWidth; x < left + width - capWidth - .01f;)
        {
            float w = Math.Min(repeatWidth, left + width - capWidth - x);
            Draw(new(x, top, w, height), new(image.Width * .375f, 0, w / scale, image.Height));
            x += w;
        }
    }
    public void Dispose() { foreach (var art in _art.Values) art.Image.Dispose(); _art.Clear(); }
}

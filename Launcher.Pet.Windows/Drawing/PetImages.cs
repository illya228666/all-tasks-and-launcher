using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Launcher.Pet.Sprites;
using Launcher.Pet.Data;
using System.Text.Json;

namespace Launcher.Pet.Windows.Drawing;
internal sealed class PetImages : IDisposable
{
    private static readonly Size HatSize = new(Launcher.Pet.Hat.HatGeometry.Width, Launcher.Pet.Hat.HatGeometry.Height);
    internal Bitmap WithHat { get; private set; } = null!;
    internal Bitmap WithoutHat { get; private set; } = null!;
    internal SpritePixelMask WithHatMask { get; private set; } = null!;
    internal SpritePixelMask WithoutHatMask { get; private set; } = null!;
    internal Bitmap Hat { get; private set; } = null!;
    internal Bitmap[] HatFalling { get; private set; } = Array.Empty<Bitmap>();
    internal PetAppearance Appearance { get; private set; } = PetAppearance.Original;
    private const long CacheLimit = 128L * 1024 * 1024;
    private readonly Dictionary<string, (Bitmap Image, SpritePixelMask Mask, long Touch)> _frames = new();
    private long _touch, _cacheBytes;
    internal long CachedBytes => _cacheBytes;
    internal (Bitmap Image, Rectangle Source, SpritePixelMask Mask) Frame(PetScene scene)
    {
        if (!Appearance.UsesClipFiles)
            return (scene.HatAttached ? WithHat : WithoutHat, scene.Appearance.GetSourceRectangle(scene.Row, scene.Frame), scene.HatAttached ? WithHatMask : WithoutHatMask);
        var clip = Appearance.Clip(scene.Row) ?? Appearance.Clip("idle")!;
        int index = Math.Clamp(scene.Frame, 0, clip.Durations.Length - 1);
        string file = (scene.HatAttached ? clip.HatFrames : clip.NoHatFrames)[index];
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Resources", Appearance.Id)) + Path.DirectorySeparatorChar;
        string path = Path.GetFullPath(Path.Combine(root, file));
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Animation frame escapes its bundle.");
        if (!_frames.TryGetValue(path, out var entry))
        {
            var frame = Read(Path.Combine(Appearance.Id, file));
            if (frame.Size != Appearance.CellSize) { frame.Dispose(); throw new InvalidDataException("Animation frame has wrong dimensions."); }
            entry = (frame, new SpritePixelMask(frame), ++_touch);
            long bytes = frame.Width * (long)frame.Height * 5;
            while (_cacheBytes + bytes > CacheLimit && _frames.Count > 0)
            {
                var oldest = _frames.MinBy(pair => pair.Value.Touch);
                _cacheBytes -= oldest.Value.Image.Width * (long)oldest.Value.Image.Height * 5;
                oldest.Value.Image.Dispose(); _frames.Remove(oldest.Key);
            }
            _frames.Add(path, entry); _cacheBytes += bytes;
        }
        else { entry.Touch = ++_touch; _frames[path] = entry; }
        return (entry.Image, new(Point.Empty, entry.Image.Size), entry.Mask);
    }

    internal PetImages(string? petId = null)
    {
        try
        {
            ChangeAppearance(PetAppearance.Find(petId));
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>
    /// Load and validate the complete replacement before releasing live images.
    /// Classic clips and hats have their own resources; the second version keeps
    /// its existing atlas and hat. Validate replacements before releasing caches.
    /// </summary>
    internal void ChangeAppearance(PetAppearance appearance)
    {
        Bitmap? withHat = null, withoutHat = null;
        try
        {
            string bundlePath = Path.Combine(AppContext.BaseDirectory, "Resources", appearance.Id, "bundle.json");
            if (appearance.Id == "sumrak" && File.Exists(bundlePath))
            {
                var definition = JsonSerializer.Deserialize<PetAppearanceDefinition>(File.ReadAllText(bundlePath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? throw new InvalidDataException("Empty animation bundle.");
                var replacement = PetAppearance.FromDefinition(definition);
                foreach (var clip in replacement.Clips.Values)
                    foreach (var file in clip.HatFrames.Concat(clip.NoHatFrames))
                    {
                        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Resources", appearance.Id)) + Path.DirectorySeparatorChar;
                        string path = Path.GetFullPath(Path.Combine(root, file));
                        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
                            throw new InvalidDataException("Animation bundle contains an unavailable frame.");
                    }
                ReplaceHats(appearance.Id);
                DisposeAtlases(); ClearFrames(); Appearance = replacement;
                return;
            }
            if (appearance.UsesSeparateFrames)
            {
                (withHat, appearance) = ReadFrameAtlas(appearance);
                withoutHat = withHat;
            }
            else
            {
                using var originalHat = Read("spritesheet_sumrak_hat.png");
                using var originalNoHat = Read("spritesheet_sumrak_no_hat.png");
                ValidateAtlases(originalHat, originalNoHat);
                using var climbingHat = AddClimbing(originalHat, true);
                using var climbingNoHat = AddClimbing(originalNoHat, false);
                using var drag = Read("sumrak_drag.png");
                withHat = AddDragFrames(climbingHat, drag, true);
                withoutHat = AddDragFrames(climbingNoHat, drag, false);
            }
            var withHatMask = new SpritePixelMask(withHat);
            var withoutHatMask = ReferenceEquals(withHat, withoutHat) ? withHatMask : new SpritePixelMask(withoutHat);
            ReplaceHats(appearance.Id);
            DisposeAtlases(); ClearFrames();
            WithHat = withHat;
            WithoutHat = withoutHat;
            WithHatMask = withHatMask;
            WithoutHatMask = withoutHatMask;
            Appearance = appearance;
        }
        catch
        {
            if (!ReferenceEquals(withHat, withoutHat)) withoutHat?.Dispose();
            withHat?.Dispose();
            throw;
        }
    }

    private static (Bitmap Atlas, PetAppearance Appearance) ReadFrameAtlas(PetAppearance appearance)
    {
        // Only behaviors already present in the engine are authored here.
        // Leftward walking mirrors the rightward sequence, including its mask.
        // Legacy rows 6–8 are unused expressions, not new runtime behaviors.
        string?[] actions = { "idle", "walk", "walk", "wave", "jump", "failed",
            null, null, null, "look", "look", "climb", "drag" };
        var geometry = new PetFrameGeometry[appearance.AuthoredRows][];
        var atlas = new Bitmap(appearance.Columns * appearance.CellSize.Width,
            appearance.AuthoredRows * appearance.CellSize.Height, PixelFormat.Format32bppArgb);
        try
        {
            using var graphics = Graphics.FromImage(atlas);
            Configure(graphics);
            for (int row = 0; row < appearance.AuthoredRows; row++)
            {
                if (actions[row] is null)
                {
                    geometry[row] = new[] { geometry[0][0] };
                    continue;
                }
                geometry[row] = new PetFrameGeometry[appearance.GetFrameCount(row)];
                for (int index = 0; index < geometry[row].Length; index++)
                {
                    int sourceIndex = index + 1 + (row == 10 ? 8 : 0);
                    using var frame = Read(Path.Combine(appearance.Id, actions[row]!, $"{actions[row]}-{sourceIndex}.png"));
                    if (frame.Size != appearance.CellSize)
                        throw new InvalidDataException("Sprite frame dimensions do not match the selected pet.");
                    if (row == 2) frame.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    Rectangle visible = VisibleBounds(frame, new(Point.Empty, frame.Size));
                    if (visible.IsEmpty) throw new InvalidDataException("Das Sprite enthaelt keine sichtbaren Pixel.");
                    // ponytail: approximate head region from the silhouette; use
                    // authored per-frame head bounds if future pets need precision.
                    int headHeight = Math.Max(1, (int)Math.Round(visible.Height * .47));
                    int headWidth = Math.Min(visible.Width, (int)Math.Round(headHeight * 1.25));
                    int center = appearance.CellSize.Width / 2;
                    geometry[row][index] = new(center, 335, new(center, visible.Top + headHeight / 2),
                        new(center - headWidth / 2, visible.Top, headWidth, headHeight));
                    graphics.DrawImage(frame, appearance.GetSourceRectangle(row, index), new Rectangle(Point.Empty, frame.Size), GraphicsUnit.Pixel);
                }
            }
            return (atlas, appearance.WithGeometry(geometry));
        }
        catch { atlas.Dispose(); throw; }
    }

    private void DisposeAtlases()
    {
        if (!ReferenceEquals(WithoutHat, WithHat)) WithoutHat?.Dispose();
        WithHat?.Dispose();
        WithHat = WithoutHat = null!;
    }
    private void ClearFrames()
    {
        foreach (var entry in _frames.Values) entry.Image.Dispose();
        _frames.Clear(); _cacheBytes = 0;
    }
    private void ReplaceHats(string id)
    {
        string folder = id == "sumrak" && File.Exists(Path.Combine(AppContext.BaseDirectory,"Resources","sumrak","hat","hat","hat-1.png"))
            ? Path.Combine("sumrak","hat","hat") : "hat";
        Bitmap? neutral = null;
        Bitmap[] frames = Array.Empty<Bitmap>();
        try
        {
            using var source = Read(Path.Combine(folder, folder == "hat" ? "hat.png" : "hat-1.png"));
            neutral = Normalize(source,HatSize);
            if (folder == "hat") frames = ReadFrames(folder,"hat_falling_",Launcher.Pet.Hat.HatAnimation.FallingFrameCount,HatSize);
            else
            {
                var loaded = new List<Bitmap>();
                try { for (int i=2;i<=8;i++) { using var image=Read(Path.Combine(folder,$"hat-{i}.png")); loaded.Add(Normalize(image,HatSize)); } }
                catch { foreach (var image in loaded) image.Dispose(); throw; }
                frames = loaded.ToArray();
            }
        }
        catch { neutral?.Dispose(); foreach (var frame in frames) frame.Dispose(); throw; }
        Hat?.Dispose(); foreach (var frame in HatFalling) frame.Dispose();
        Hat=neutral; HatFalling=frames;
    }

    private static Bitmap AddDragFrames(Bitmap original, Bitmap drag, bool hat)
    {
        if (drag.Size != new Size(PetSpriteCatalog.AtlasColumns * PetSpriteCatalog.AtlasCellWidth, 2 * PetSpriteCatalog.AtlasCellHeight))
            throw new InvalidDataException("Drag sprite atlas dimensions do not match the authored geometry.");
        var atlas = new Bitmap(original.Width, original.Height + PetSpriteCatalog.AtlasCellHeight, PixelFormat.Format32bppArgb);
        try
        {
            using var graphics = Graphics.FromImage(atlas);
            Configure(graphics);
            graphics.DrawImage(original, new Rectangle(Point.Empty, original.Size), new Rectangle(Point.Empty, original.Size), GraphicsUnit.Pixel);
            graphics.DrawImage(drag, new Rectangle(0, original.Height, original.Width, PetSpriteCatalog.AtlasCellHeight),
                new Rectangle(0, hat ? 0 : PetSpriteCatalog.AtlasCellHeight, drag.Width, PetSpriteCatalog.AtlasCellHeight), GraphicsUnit.Pixel);
            return atlas;
        }
        catch { atlas.Dispose(); throw; }
    }

    private static Bitmap AddClimbing(Bitmap original, bool hat)
    {
        using var climbing = Read(Path.Combine("ruins", "climbing.png"));
        var atlas = new Bitmap(original.Width, original.Height + PetSpriteCatalog.AtlasCellHeight, PixelFormat.Format32bppArgb);
        try
        {
            using var graphics = Graphics.FromImage(atlas);
            Configure(graphics);
            // Atlas coordinates are pixels, irrespective of PNG DPI metadata.
            graphics.DrawImage(original, new Rectangle(Point.Empty, original.Size), new Rectangle(Point.Empty, original.Size), GraphicsUnit.Pixel);
            int w = climbing.Width / 4, h = climbing.Height / 2;
            for (int i = 0; i < 4; i++)
            {
                Rectangle visible = VisibleBounds(climbing, new(i * w, hat ? h : 0, w, h));
                Rectangle target = Fit(visible.Size, new(PetSpriteCatalog.AtlasCellWidth, PetSpriteCatalog.AtlasCellHeight), true);
                target.Offset(i * PetSpriteCatalog.AtlasCellWidth, original.Height);
                graphics.DrawImage(climbing, target, visible, GraphicsUnit.Pixel);
            }
            return atlas;
        }
        catch { atlas.Dispose(); throw; }
    }

    private static Bitmap Read(string name)
    {
        using var source = new Bitmap(Path.Combine(AppContext.BaseDirectory, "Resources", name));
        return source.Clone(new Rectangle(Point.Empty, source.Size), PixelFormat.Format32bppArgb);
    }

    private static Bitmap[] ReadFrames(string folder, string prefix, int count, Size target)
    {
        var frames = new Bitmap[count];
        try
        {
            for (int frame = 0; frame < count; frame++)
            {
                using var source = Read(Path.Combine(folder, $"{prefix}{frame + 1}.png"));
                frames[frame] = Normalize(source, target);
            }
            return frames;
        }
        catch
        {
            foreach (Bitmap? frame in frames)
                frame?.Dispose();
            throw;
        }
    }

    private static void ValidateAtlases(Bitmap withHat, Bitmap withoutHat)
    {
        var expected = new Size(PetSpriteCatalog.AtlasColumns * PetSpriteCatalog.AtlasCellWidth,
            PetSpriteCatalog.AtlasRows * PetSpriteCatalog.AtlasCellHeight);
        if (withHat.Size != expected || withoutHat.Size != expected)
            throw new InvalidDataException("Sprite atlas dimensions do not match the authored geometry.");
    }

    private static Bitmap Normalize(Bitmap source, Size target)
    {
        Rectangle bounds = VisibleBounds(source, new(Point.Empty, source.Size));
        if (bounds.IsEmpty)
            throw new InvalidDataException("Das Sprite enthaelt keine sichtbaren Pixel.");
        var result = new Bitmap(target.Width, target.Height, PixelFormat.Format32bppPArgb);
        try
        {
            using Graphics graphics = Graphics.FromImage(result);
            Configure(graphics);
            graphics.DrawImage(source, Fit(bounds.Size, target, bottomAligned: false), bounds, GraphicsUnit.Pixel);
            return result;
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }

    private static Rectangle VisibleBounds(Bitmap image, Rectangle area)
    {
        int left = area.Right, top = area.Bottom, right = area.Left - 1, bottom = area.Top - 1;
        BitmapData data = image.LockBits(area, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var pixels = new byte[area.Width * 4];
            for (int row = 0; row < area.Height; row++)
            {
                Marshal.Copy(IntPtr.Add(data.Scan0, row * data.Stride), pixels, 0, pixels.Length);
                for (int column = 0; column < area.Width; column++)
                {
                    if (pixels[column * 4 + 3] == 0)
                        continue;
                    int x = area.Left + column, y = area.Top + row;
                    left = Math.Min(left, x);
                    top = Math.Min(top, y);
                    right = Math.Max(right, x);
                    bottom = Math.Max(bottom, y);
                }
            }
        }
        finally
        {
            image.UnlockBits(data);
        }
        return right < left ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }

    private static Rectangle Fit(Size source, Size target, bool bottomAligned)
    {
        double scale = Math.Min((double)target.Width / source.Width, (double)target.Height / source.Height);
        int width = Math.Min(target.Width, Math.Max(1, (int)Math.Round(source.Width * scale)));
        int height = Math.Min(target.Height, Math.Max(1, (int)Math.Round(source.Height * scale)));
        return new((target.Width - width) / 2, bottomAligned ? target.Height - height : (target.Height - height) / 2, width, height);
    }

    private static void Configure(Graphics graphics) => graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

    public void Dispose()
    {
        ClearFrames();
        foreach (Bitmap? frame in HatFalling)
            frame?.Dispose();
        Hat?.Dispose();
        DisposeAtlases();
    }
}


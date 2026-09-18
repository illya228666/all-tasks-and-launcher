using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Launcher.Pet.Sprites;

namespace Launcher.Pet.Windows.Drawing;
internal sealed class PetImages : IDisposable
{
    private static readonly Size HatSize = new(109, 64);
    internal Bitmap WithHat { get; private set; } = null!;
    internal Bitmap WithoutHat { get; private set; } = null!;
    internal Bitmap Hat { get; private set; } = null!;
    internal Bitmap[] HatFalling { get; private set; } = Array.Empty<Bitmap>();

    internal PetImages()
    {
        try
        {
            using var withHat = Read("spritesheet_sumrak_hat.png");
            using var withoutHat = Read("spritesheet_sumrak_no_hat.png");
            (WithHat, WithoutHat) = NormalizeAtlases(withHat, withoutHat);
            using var hat = Read(Path.Combine("hat", "hat.png"));
            Hat = Normalize(hat, HatSize);
            HatFalling = ReadFrames("hat", "hat_falling_", HatSize);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    private static Bitmap Read(string name)
    {
        using var source = new Bitmap(Path.Combine(AppContext.BaseDirectory, "Resources", name));
        return source.Clone(new Rectangle(Point.Empty, source.Size), PixelFormat.Format32bppArgb);
    }

    private static Bitmap[] ReadFrames(string folder, string prefix, Size target)
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Resources", folder);
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException($"Der Sprite-Ordner wurde nicht gefunden: {directory}");

        var paths = new List<(int Index, string Path)>();
        foreach (string path in Directory.EnumerateFiles(directory, $"{prefix}*.png", SearchOption.TopDirectoryOnly))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            string suffix = name[prefix.Length..];
            if (int.TryParse(suffix, out int index) && index > 0)
                paths.Add((index, path));
        }

        paths.Sort((left, right) => left.Index.CompareTo(right.Index));
        if (paths.Count == 0)
            throw new InvalidDataException($"Keine nummerierten Frames '{prefix}*.png' in '{directory}' gefunden.");

        for (int index = 1; index < paths.Count; index++)
        {
            if (paths[index - 1].Index == paths[index].Index)
                throw new InvalidDataException($"Doppelter Frame-Index {paths[index].Index} fuer '{prefix}'.");
        }

        var frames = new Bitmap[paths.Count];
        try
        {
            for (int frame = 0; frame < paths.Count; frame++)
            {
                using var source = new Bitmap(paths[frame].Path);
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

    private static (Bitmap WithHat, Bitmap WithoutHat) NormalizeAtlases(Bitmap withHat, Bitmap withoutHat)
    {
        int columns = PetSpriteCatalog.AtlasColumns;
        int rows = PetSpriteCatalog.AtlasRows;
        if (withHat.Size != withoutHat.Size || withHat.Width < columns || withHat.Height < rows || withHat.Width % columns != 0 || withHat.Height % rows != 0)
            throw new InvalidDataException("Die Sprite-Atlanten muessen dieselbe durch das Raster teilbare Groesse haben.");

        int sourceWidth = withHat.Width / columns;
        int sourceHeight = withHat.Height / rows;
        int targetWidth = PetSpriteCatalog.AtlasCellWidth;
        int targetHeight = PetSpriteCatalog.AtlasCellHeight;
        var withResult = new Bitmap(columns * targetWidth, rows * targetHeight, PixelFormat.Format32bppPArgb);
        Bitmap? withoutResult = null;
        try
        {
            withoutResult = new Bitmap(withResult.Width, withResult.Height, PixelFormat.Format32bppPArgb);
            using Graphics withGraphics = Graphics.FromImage(withResult);
            using Graphics withoutGraphics = Graphics.FromImage(withoutResult);
            Configure(withGraphics);
            Configure(withoutGraphics);
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    var cell = new Rectangle(column * sourceWidth, row * sourceHeight, sourceWidth, sourceHeight);
                    Rectangle withBounds = VisibleBounds(withHat, cell);
                    Rectangle withoutBounds = VisibleBounds(withoutHat, cell);
                    Rectangle bounds = withBounds.IsEmpty ? withoutBounds : withoutBounds.IsEmpty ? withBounds : Rectangle.Union(withBounds, withoutBounds);
                    if (bounds.IsEmpty)
                        continue;
                    var destination = Fit(bounds.Size, new(targetWidth, targetHeight), bottomAligned: true);
                    destination.Offset(column * targetWidth, row * targetHeight);
                    withGraphics.DrawImage(withHat, destination, bounds, GraphicsUnit.Pixel);
                    withoutGraphics.DrawImage(withoutHat, destination, bounds, GraphicsUnit.Pixel);
                }
            }
            return (withResult, withoutResult);
        }
        catch
        {
            withResult.Dispose();
            withoutResult?.Dispose();
            throw;
        }
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
        foreach (Bitmap? frame in HatFalling)
            frame?.Dispose();
        Hat?.Dispose();
        WithoutHat?.Dispose();
        WithHat?.Dispose();
    }
}

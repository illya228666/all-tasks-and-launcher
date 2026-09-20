using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Launcher.Pet.Sprites;

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

    internal PetImages()
    {
        try
        {
            using var withHat = Read("spritesheet_sumrak_hat.png");
            using var withoutHat = Read("spritesheet_sumrak_no_hat.png");
            ValidateAtlases(withHat, withoutHat);
            WithHat = new Bitmap(withHat);
            WithoutHat = new Bitmap(withoutHat);
            WithHatMask = new(WithHat);
            WithoutHatMask = new(WithoutHat);
            using var hat = Read(Path.Combine("hat", "hat.png"));
            Hat = Normalize(hat, HatSize);
            HatFalling = ReadFrames("hat", "hat_falling_", Launcher.Pet.Hat.HatAnimation.FallingFrameCount, HatSize);
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
        foreach (Bitmap? frame in HatFalling)
            frame?.Dispose();
        Hat?.Dispose();
        WithoutHat?.Dispose();
        WithHat?.Dispose();
    }
}


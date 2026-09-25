using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Launcher.Pet.Animation;
using Launcher.Pet.Sprites;

namespace Launcher.Pet.Windows.Drawing;
internal sealed class PetImages : IDisposable
{
    private const int IdleFrameCount = 5;
    private const int RunFrameCount = 7;
    private static readonly Size FrameSize = new(PetSpriteCatalog.AtlasCellWidth, PetSpriteCatalog.AtlasCellHeight);
    private static readonly Size HatSize = new(Launcher.Pet.Hat.HatGeometry.Width, Launcher.Pet.Hat.HatGeometry.Height);

    private Bitmap[] Idle { get; set; } = Array.Empty<Bitmap>();
    private Bitmap[] RunRight { get; set; } = Array.Empty<Bitmap>();
    private Bitmap[] RunLeft { get; set; } = Array.Empty<Bitmap>();
    private SpritePixelMask[] IdleMasks { get; set; } = Array.Empty<SpritePixelMask>();
    private SpritePixelMask[] RunRightMasks { get; set; } = Array.Empty<SpritePixelMask>();
    private SpritePixelMask[] RunLeftMasks { get; set; } = Array.Empty<SpritePixelMask>();

    internal Bitmap Hat { get; private set; } = null!;
    internal Bitmap[] HatFalling { get; private set; } = Array.Empty<Bitmap>();

    internal PetImages()
    {
        try
        {
            // Main Sumrak animations use one authored PNG per frame, exactly like the existing hat-fall sequence.
            Idle = ReadFrames("sumrak", "idle_", IdleFrameCount, FrameSize, bottomAligned: true);
            RunRight = ReadFrames("sumrak", "run_", RunFrameCount, FrameSize, bottomAligned: true);
            RunLeft = MirrorFrames(RunRight);
            IdleMasks = CreateMasks(Idle);
            RunRightMasks = CreateMasks(RunRight);
            RunLeftMasks = CreateMasks(RunLeft);

            using var hat = Read(Path.Combine("hat", "hat.png"));
            Hat = Normalize(hat, HatSize, bottomAligned: false);
            HatFalling = ReadFrames("hat", "hat_falling_", Launcher.Pet.Hat.HatAnimation.FallingFrameCount, HatSize, bottomAligned: false);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    internal Bitmap GetFrame(int row, int frame)
    {
        return row switch
        {
            PetAnimationCatalog.IdleRow => Idle[Math.Clamp(frame, 0, Idle.Length - 1)],
            PetAnimationCatalog.MoveRightRow => RunRight[Math.Clamp(frame, 0, RunRight.Length - 1)],
            PetAnimationCatalog.MoveLeftRow => RunLeft[Math.Clamp(frame, 0, RunLeft.Length - 1)],
            // No new art yet: keep every unsupported pose visually static instead of showing the legacy atlas.
            _ => Idle[0]
        };
    }

    internal SpritePixelMask GetMask(int row, int frame)
    {
        return row switch
        {
            PetAnimationCatalog.IdleRow => IdleMasks[Math.Clamp(frame, 0, IdleMasks.Length - 1)],
            PetAnimationCatalog.MoveRightRow => RunRightMasks[Math.Clamp(frame, 0, RunRightMasks.Length - 1)],
            PetAnimationCatalog.MoveLeftRow => RunLeftMasks[Math.Clamp(frame, 0, RunLeftMasks.Length - 1)],
            _ => IdleMasks[0]
        };
    }

    private static SpritePixelMask[] CreateMasks(IEnumerable<Bitmap> frames) =>
        frames.Select(frame => new SpritePixelMask(frame)).ToArray();

    private static Bitmap[] MirrorFrames(Bitmap[] source)
    {
        var frames = new Bitmap[source.Length];
        try
        {
            for (int i = 0; i < source.Length; i++)
            {
                frames[i] = source[i].Clone(new Rectangle(Point.Empty, source[i].Size), PixelFormat.Format32bppArgb);
                frames[i].RotateFlip(RotateFlipType.RotateNoneFlipX);
            }
            return frames;
        }
        catch
        {
            DisposeFrames(frames);
            throw;
        }
    }

    private static Bitmap Read(string name)
    {
        using var source = new Bitmap(Path.Combine(AppContext.BaseDirectory, "Resources", name));
        return source.Clone(new Rectangle(Point.Empty, source.Size), PixelFormat.Format32bppArgb);
    }

    private static Bitmap[] ReadFrames(string folder, string prefix, int count, Size target, bool bottomAligned)
    {
        var frames = new Bitmap[count];
        try
        {
            for (int frame = 0; frame < count; frame++)
            {
                using var source = Read(Path.Combine(folder, $"{prefix}{frame + 1}.png"));
                frames[frame] = Normalize(source, target, bottomAligned);
            }
            return frames;
        }
        catch
        {
            DisposeFrames(frames);
            throw;
        }
    }

    private static Bitmap Normalize(Bitmap source, Size target, bool bottomAligned)
    {
        if (source.Size == target)
            return source.Clone(new Rectangle(Point.Empty, source.Size), PixelFormat.Format32bppArgb);

        Rectangle bounds = VisibleBounds(source, new(Point.Empty, source.Size));
        if (bounds.IsEmpty)
            throw new InvalidDataException("Das Sprite enthaelt keine sichtbaren Pixel.");

        var result = new Bitmap(target.Width, target.Height, PixelFormat.Format32bppArgb);
        try
        {
            using Graphics graphics = Graphics.FromImage(result);
            Configure(graphics);
            graphics.DrawImage(source, Fit(bounds.Size, target, bottomAligned), bounds, GraphicsUnit.Pixel);
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

    private static void DisposeFrames(IEnumerable<Bitmap?> frames)
    {
        foreach (Bitmap? frame in frames)
            frame?.Dispose();
    }

    public void Dispose()
    {
        DisposeFrames(HatFalling);
        Hat?.Dispose();
        DisposeFrames(RunLeft);
        DisposeFrames(RunRight);
        DisposeFrames(Idle);
    }
}

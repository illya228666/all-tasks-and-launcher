using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Launcher.Pet.Exploration;

namespace Launcher.Pet.Windows.Drawing;

internal sealed class RuinRenderer : IDisposable
{
    private readonly Bitmap[] _tiles;
    private Bitmap? _canvas, _static;
    private RuinScene? _scene;
    private readonly (float X, float Y, float Speed, float Phase)[] _particles;
    internal RuinRenderer()
    {
        using var atlas = new Bitmap(Path.Combine(AppContext.BaseDirectory, "Resources", "ruins", "environment.png"));
        Rectangle[] cells = { new(10, 120, 650, 380), new(665, 0, 465, 505), new(1240, 10, 235, 495),
            new(10, 650, 670, 330), new(800, 510, 190, 510), new(1100, 650, 435, 340) };
        _tiles = new Bitmap[cells.Length + 6];
        try
        {
            for (int i = 0; i < cells.Length; i++) _tiles[i] = atlas.Clone(cells[i], PixelFormat.Format32bppPArgb);
            using var islands = new Bitmap(Path.Combine(AppContext.BaseDirectory, "Resources", "ruins", "islands.png"));
            for (int i = 0; i < 6; i++) _tiles[i + 6] = islands.Clone(new Rectangle(i % 3 * 512, i / 3 * 512, 512, 512), PixelFormat.Format32bppPArgb);
        }
        catch { foreach (var tile in _tiles) tile?.Dispose(); throw; }
        var random = new Random(1701);
        _particles = Enumerable.Range(0, 32).Select(_ => (random.NextSingle(), random.NextSingle(), 0.015f + random.NextSingle() * 0.02f, random.NextSingle() * 6.28f)).ToArray();
    }
    internal Bitmap Render(RuinScene scene, Point awakening, float seconds)
    {
        if (_canvas is null || _canvas.Size != scene.Size || !ReferenceEquals(_scene, scene))
        {
            _canvas?.Dispose(); _static?.Dispose(); _static = null;
            _canvas = new Bitmap(Math.Max(1, scene.Size.Width), Math.Max(1, scene.Size.Height), PixelFormat.Format32bppPArgb);
            _scene = scene;
        }
        using (var g = Graphics.FromImage(_canvas))
        {
            g.Clear(Color.Transparent);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (seconds >= 3.5f && _static is null)
            {
                _static = new Bitmap(_canvas.Width, _canvas.Height, PixelFormat.Format32bppPArgb);
                using var background = Graphics.FromImage(_static);
                background.InterpolationMode = InterpolationMode.HighQualityBicubic;
                foreach (var decoration in scene.Decorations.Where(d => d.Tile != 5)) Draw(background, decoration, 1, 0);
            }
            if (_static is not null) g.DrawImageUnscaled(_static, 0, 0);
            foreach (var decoration in scene.Decorations)
            {
                if (_static is not null && decoration.Tile != 5) continue;
                float progress = Math.Clamp((seconds - decoration.RevealAt) / 0.85f, 0, 1);
                Draw(g, decoration, progress, seconds);
            }
            if (seconds < 3.5f)
            {
                float t = seconds / 3.5f;
                float radius = t * MathF.Sqrt(scene.Size.Width * (float)scene.Size.Width + scene.Size.Height * (float)scene.Size.Height);
                using var pen = new Pen(Color.FromArgb((int)(90 * (1 - t)), 137, 225, 241), 3);
                if (radius > 1) g.DrawEllipse(pen, awakening.X - radius, awakening.Y - radius, radius * 2, radius * 2);
            }
            foreach (var particle in _particles)
            {
                float fade = Math.Clamp(seconds / 3.5f, 0, 1);
                float x = particle.X * scene.Size.Width + MathF.Sin(seconds + particle.Phase) * 8;
                float y = (1 - (particle.Y + seconds * particle.Speed) % 1) * scene.Size.Height;
                using var brush = new SolidBrush(Color.FromArgb((int)(fade * (50 + 65 * (0.5f + 0.5f * MathF.Sin(seconds * 1.2f + particle.Phase)))), 150, 228, 242));
                g.FillEllipse(brush, x, y, 2.5f, 2.5f);
            }
        }
        return _canvas;
    }
    private void Draw(Graphics g, RuinDecoration decoration, float progress, float seconds)
    {
        if (progress <= 0) return;
        float ease = progress * progress * (3 - 2 * progress);
        RectangleF destination = decoration.Bounds;
        if (decoration.Tile != 4) destination.Y += (1 - ease) * (decoration.Tile >= 6 ? 12 : 22);
        if (decoration.Tile == 5) destination.X += MathF.Sin(seconds * 1.1f + decoration.Phase) * 1.4f;
        using var attributes = new ImageAttributes();
        attributes.SetColorMatrix(new ColorMatrix { Matrix33 = ease * decoration.Opacity });
        var tile = _tiles[decoration.Tile];
        Rectangle pixels = Rectangle.Round(destination);
        if (pixels.Width <= 0 || pixels.Height <= 0) return;
        if (decoration.Tile == 4)
        {
            var saved = g.Save();
            if (decoration.RootBottom is PointF bottom && decoration.RootTop is PointF top)
            {
                float dx = bottom.X - top.X, dy = bottom.Y - top.Y;
                float length = MathF.Sqrt(dx * dx + dy * dy);
                g.TranslateTransform(top.X, top.Y);
                g.RotateTransform(-MathF.Atan2(dx, dy) * 180 / MathF.PI);
                destination = new(-12, 0, 24, length);
            }
            float revealHeight = destination.Height * ease;
            g.SetClip(new RectangleF(destination.Left - 1, destination.Bottom - revealHeight, destination.Width + 2, revealHeight), CombineMode.Intersect);
            float segmentHeight = destination.Width * tile.Height / tile.Width;
            int index = 0;
            for (float y = destination.Top; y < destination.Bottom; y += segmentHeight - 3)
                DrawTile(g, tile, Rectangle.Round(new RectangleF(destination.Left, y, destination.Width, segmentHeight)), decoration.Mirrored ^ index++ % 2 == 0, attributes);
            g.Restore(saved);
        }
        else DrawTile(g, tile, pixels, decoration.Mirrored, attributes);
    }
    private static void DrawTile(Graphics g, Bitmap tile, Rectangle bounds, bool mirrored, ImageAttributes attributes)
    {
        Point[] corners = mirrored
            ? new[] { new Point(bounds.Right, bounds.Top), new Point(bounds.Left, bounds.Top), new Point(bounds.Right, bounds.Bottom) }
            : new[] { bounds.Location, new Point(bounds.Right, bounds.Top), new Point(bounds.Left, bounds.Bottom) };
        g.DrawImage(tile, corners, new Rectangle(Point.Empty, tile.Size), GraphicsUnit.Pixel, attributes);
    }
    public void Dispose()
    {
        _canvas?.Dispose(); _static?.Dispose(); foreach (var tile in _tiles) tile?.Dispose();
    }
}

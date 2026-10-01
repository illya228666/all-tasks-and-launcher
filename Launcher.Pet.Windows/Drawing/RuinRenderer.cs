using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Launcher.Pet.Exploration;
using Launcher.Pet.Life;

namespace Launcher.Pet.Windows.Drawing;

internal sealed class RuinRenderer : IDisposable
{
    private readonly Bitmap[] _tiles;
    private readonly RuinArtCatalog _art = new();
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
    internal Bitmap Render(RuinScene scene, Point awakening, float seconds, LifeScene? life = null)
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
                if (life is not null && decoration.Tile == 5) continue;
                if (_static is not null && decoration.Tile != 5) continue;
                float progress = Math.Clamp((seconds - decoration.RevealAt) / 0.85f, 0, 1);
                Draw(g, decoration, progress, seconds);
            }
            if (life is not null) DrawLife(g, life, seconds);
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
    private void DrawLife(Graphics g, LifeScene life, float seconds)
    {
        float scale = _scene!.WorldMetrics.MotionScale;
        foreach (var site in life.Sites)
        {
            // Soil is a physical patch, not an on-screen resource meter.
            float richness = Math.Clamp(site.Nutrients / 12000f, 0, 1);
            using var soil = new SolidBrush(Color.FromArgb(190, (int)(70 + richness * 50), (int)(47 + richness * 25), 43));
            var soilArt = _art.Find(richness > .5f ? "life:soil-rich" : "life:soil-poor");
            if (soilArt is not null)
            {
                float soilWidth = 44 * scale, soilHeight = soilWidth * soilArt.Image.Height / soilArt.Image.Width;
                g.DrawImage(soilArt.Image, new RectangleF(site.Position.X - soilWidth / 2, site.Position.Y - soilHeight * .8f, soilWidth, soilHeight));
            }
            else g.FillEllipse(soil, site.Position.X - 15 * scale, site.Position.Y - 4 * scale, 30 * scale, 4 * scale);
            if (life.Focus == site.Id && life.ActionSeconds > 0 && life.Activity == LifeTaskKind.Recycle)
            {
                if (DrawEffect(g, "decompose", site.Position, life.ActionSeconds / 2, scale)) { }
                else
                {
                    using var crumb = new SolidBrush(Color.FromArgb(210, 212, 165, 99));
                    for (int i = 0; i < 5; i++)
                    {
                        float t = (life.ActionSeconds * 1.5f + i * .2f) % 1;
                        g.FillEllipse(crumb, site.Position.X + (i - 2) * 4 * scale, site.Position.Y - (14 * (1 - t) + 3) * scale, 2 * scale, 2 * scale);
                    }
                }
            }
            if (site.Stage is null) continue;
            float fullness = Math.Clamp(site.Biomass / 6000f, 0.15f, 1);
            float width = (12 + fullness * 30) * scale;
            float height = (site.Stage == MossStage.Germinating ? 8 : 12 + fullness * 25) * scale;
            var bounds = new RectangleF(site.Position.X - width / 2, site.Position.Y - height, width, height + 2);
            using var color = new ImageAttributes();
            bool exhausted = site.Stage == MossStage.Exhausted;
            color.SetColorMatrix(exhausted ? new ColorMatrix(new[] {
                new[] { .65f, .25f, .10f, 0, 0 }, new[] { .45f, .30f, .12f, 0, 0 },
                new[] { .25f, .15f, .08f, 0, 0 }, new[] { 0f, 0, 0, .8f, 0 }, new[] { .1f, .04f, 0, 0, 1 } })
                : new ColorMatrix { Matrix33 = site.Stage == MossStage.Germinating ? .65f : 1 });
            var mossArt = _art.Find("life:" + site.Stage.Value.ToString().ToLowerInvariant());
            if (mossArt is not null)
            {
                color.ClearColorMatrix();
                float authoredHeight = width * mossArt.Image.Height / mossArt.Image.Width;
                float factor = Math.Min(1, _scene.WorldMetrics.CharacterHeight * .45f / authoredHeight);
                bounds = new(site.Position.X - width * factor / 2, site.Position.Y - authoredHeight * factor, width * factor, authoredHeight * factor);
                DrawTile(g, mossArt.Image, Rectangle.Round(bounds), false, color);
            }
            else DrawTile(g, _tiles[5], Rectangle.Round(bounds), false, color);
            if (site.Stage == MossStage.Mature)
            {
                using var glow = new SolidBrush(Color.FromArgb((int)(22 + 12 * Math.Sin(seconds * 1.2f + site.Position.X)), 128, 236, 190));
                g.FillEllipse(glow, bounds.X - 3, bounds.Y, bounds.Width + 6, bounds.Height);
            }
        }
        foreach (var signal in life.Signals ?? Array.Empty<LifeSignalView>())
        {
            string? effect = signal.Kind switch { LifeEventKind.Released => "release", LifeEventKind.Established => "plant", LifeEventKind.Decomposed => "decompose", _ => null };
            double age = signal.Age + life.Fraction;
            if (effect is not null && age < 1.28 && DrawEffect(g, effect, signal.Position, (float)(age / 1.28), scale)) continue;
            if (effect is not null && _art.Available) continue;
            using var brush = new SolidBrush(Color.FromArgb((int)(150 * (1 - signal.Age / 5f)), signal.Kind == LifeEventKind.Released ? Color.Aquamarine : Color.Goldenrod));
            for (int i = 0; i < 4; i++)
                g.FillEllipse(brush, signal.Position.X + (i - 1.5f) * (3 + signal.Age * 2), signal.Position.Y - 9 - signal.Age * 4, 2, 3);
        }
    }
    private bool DrawEffect(Graphics g, string name, PointF position, float progress, float scale)
    {
        var effect = _art.Find($"fx:{name}:{Math.Clamp((int)(progress * 8), 0, 7)}");
        if (effect is null) return false;
        float size = 48 * scale;
        g.DrawImage(effect.Image, new RectangleF(position.X - size / 2, position.Y - size * 480 / 512, size, size));
        return true;
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
        var asset = decoration.AssetKey is null ? null : _art.Find(decoration.AssetKey);
        if (_art.Available && decoration.Tile >= 6)
        {
            var platform = _scene!.Platforms.FirstOrDefault(p => p.Id == decoration.PlatformId)
                ?? _scene.Platforms.Where(p => p.Id != "ruin:floor").OrderBy(p => Math.Abs(p.Left - decoration.Bounds.Left)).FirstOrDefault();
            asset ??= _art.Find($"platform:{decoration.Tile - 6}");
            if (asset is not null && platform is not null)
            {
                RuinArtCatalog.DrawModule(g, asset, platform.Left, platform.Y + (1 - ease) * 22, platform.Right - platform.Left,
                    _scene.WorldMetrics.CharacterHeight * .72f, attributes);
                return;
            }
        }
        var tile = asset?.Image ?? _tiles[decoration.Tile];
        Rectangle pixels = Rectangle.Round(destination);
        if (pixels.Width <= 0 || pixels.Height <= 0) return;
        if (decoration.Tile == 4 && decoration.RootBottom is PointF bottom && decoration.RootTop is PointF top)
        {
            float segmentHeight = Math.Max(1, (24f * tile.Height / tile.Width - 3) * _scene!.WorldMetrics.MotionScale);
            int index = 0;
            for (float y = bottom.Y - (bottom.Y - top.Y) * ease; y < bottom.Y;)
            {
                float nextY = Math.Min(bottom.Y, y + segmentHeight);
                float x = RuinMotion.ClimbX(top, bottom, y, _scene!.WorldMetrics) + (_scene.Metrics is null ? 0 : _scene.WorldMetrics.CharacterHeight * .22f);
                float dx = RuinMotion.ClimbX(top, bottom, nextY, _scene.WorldMetrics) - RuinMotion.ClimbX(top, bottom, y, _scene.WorldMetrics);
                float dy = nextY - y;
                var saved = g.Save();
                g.TranslateTransform(x, y);
                g.RotateTransform(-MathF.Atan2(dx, dy) * 180 / MathF.PI);
                float rootScale = _scene.WorldMetrics.MotionScale;
                float nativeScale = 24 * rootScale / tile.Width;
                float drawnHeight = Math.Min(tile.Height * nativeScale, MathF.Sqrt(dx * dx + dy * dy) + 3);
                bool mirrored = decoration.Mirrored ^ index++ % 2 == 0;
                PointF[] corners = mirrored
                    ? new[] { new PointF(12 * rootScale, 0), new PointF(-12 * rootScale, 0), new PointF(12 * rootScale, drawnHeight) }
                    : new[] { new PointF(-12 * rootScale, 0), new PointF(12 * rootScale, 0), new PointF(-12 * rootScale, drawnHeight) };
                // Crop the final segment, rather than compressing a whole root into it.
                g.DrawImage(tile, corners, new RectangleF(0, 0, tile.Width, drawnHeight / nativeScale), GraphicsUnit.Pixel, attributes);
                g.Restore(saved);
                y = nextY;
            }
        }
        else if (decoration.Tile == 3 && asset is not null)
            RuinArtCatalog.DrawModule(g, asset, destination.Left, destination.Top, destination.Width, destination.Height, attributes);
        else
        {
            if (asset is not null)
            {
                float scale = Math.Min(destination.Width / tile.Width, destination.Height / tile.Height);
                pixels = Rectangle.Round(new RectangleF(destination.Left + (destination.Width - tile.Width * scale) / 2,
                    destination.Bottom - tile.Height * scale, tile.Width * scale, tile.Height * scale));
            }
            DrawTile(g, tile, pixels, decoration.Mirrored, attributes);
        }
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
        _art.Dispose(); _canvas?.Dispose(); _static?.Dispose(); foreach (var tile in _tiles) tile?.Dispose();
    }
}

using System.Drawing;
namespace Launcher.Pet.Exploration;

// Snapshot geometry and the variation seed survive layout changes.
public sealed class RuinBuilder
{
    private readonly DesktopSeed[] _seed;
    private readonly Size _originalSize;
    private readonly int _variation;
    private int _revision;
    public RuinBuilder(Size size, IEnumerable<DesktopSeed> seed, int? variation = null)
    {
        _originalSize = size;
        _seed = seed.ToArray();
        _variation = variation ?? Random.Shared.Next();
    }

    public RuinScene Build(Size size, Point awakening)
    {
        var random = new Random(_variation);
        float sx = (float)size.Width / Math.Max(1, _originalSize.Width);
        float sy = (float)size.Height / Math.Max(1, _originalSize.Height);
        RectangleF Scale(Rectangle r) => new(r.X * sx, r.Y * sy, r.Width * sx, r.Height * sy);
        var exclusions = _seed.SelectMany(item => item.IsIcon
            ? new[] { RectangleF.Inflate(Scale(item.Bounds), 8, 6), RectangleF.Inflate(Scale(item.LabelBounds), 6, 6) }
            : new[] { RectangleF.Inflate(Scale(item.Bounds), -32, -40) }).Where(r => r.Width > 0 && r.Height > 0).ToArray();
        var platforms = new List<RuinPlatform> { new("ruin:floor", 0, size.Width, size.Height, 0) };
        var decorations = new List<RuinDecoration>();
        int identity = 1;
        float Reveal(float x, float y) => 2.35f * Math.Clamp(MathF.Sqrt((x - awakening.X) * (x - awakening.X) + (y - awakening.Y) * (y - awakening.Y))
            / Math.Max(1, MathF.Sqrt(size.Width * (float)size.Width + size.Height * (float)size.Height)), 0, 1);
        bool Add(float center, float y, float width)
        {
            if (size.Width < 180 || y < 140 || y > size.Height - 70 || platforms.Count >= 30) return false;
            width = Math.Min(width, size.Width - 32);
            float left = Math.Clamp(center - width / 2, 16, size.Width - width - 16);
            if (platforms.Skip(1).Any(p => Math.Abs(p.Y - y) < 135 && left < p.Right + 22 && left + width > p.Left - 22)) return false;
            platforms.Add(new($"ruin:{identity++}", left, left + width, y, Reveal(left + width / 2, y)));
            return true;
        }
        // Small groups around desktop icons, not one stretched shelf per column.
        foreach (var item in _seed.Where(s => s.IsIcon).OrderBy(s => s.Bounds.Top))
        {
            RectangleF r = Scale(item.Bounds);
            float y = r.Top - 58;
            if (y < 140) y = Math.Max(150, Scale(item.LabelBounds).Bottom + 42);
            Add(r.Left + r.Width / 2, y, 155 + random.Next(35));
        }
        // Asymmetric corners retain the relationship to the original windows.
        foreach (var item in _seed.Where(s => !s.IsIcon))
        {
            RectangleF r = Scale(item.Bounds);
            if (r.Width < 180 || r.Height < 120) continue;
            Add(r.Left + 85, r.Top - 18, 205 + random.Next(45));
            Add(r.Right - 60, r.Top + 150, 165 + random.Next(40));
            Add(r.Right - 100, Math.Min(size.Height - 100, r.Bottom + 18), 215 + random.Next(50));
        }
        int lanes = Math.Clamp(size.Width / 360, 2, 5);
        for (int row = 0; row < 5; row++)
        {
            float y = size.Height - 120 - row * 180;
            if (y < 150) break;
            for (int lane = 0; lane < lanes; lane++)
            {
                float nominal = (lane + 0.5f) * size.Width / lanes;
                float width = 165 + random.Next(65);
                float offset = random.Next(-58, 59) + (row % 2 == 0 ? -22 : 22);
                float[] choices = { nominal + offset, nominal - 70, nominal + 70 };
                float center = choices.OrderBy(x => exclusions.Sum(r => Overlap(r, new(x - width / 2, y - 18, width, 85)))).First();
                Add(center, y + random.Next(-16, 17), width);
            }
        }
        // Bridge long vertical gaps with real stepping islands; never stretch a root across the screen.
        foreach (var upper in platforms.Skip(1).ToArray().OrderBy(p => p.Y))
        {
            var current = upper;
            while (true)
            {
                var lower = Lower(current);
                if (lower.Y - current.Y <= 230) break;
                if (!Add(current.Center + random.Next(-18, 19), current.Y + 180, 145 + random.Next(35))) break;
                current = platforms[^1];
            }
        }
        float Gap(RuinPlatform a, RuinPlatform b) => Math.Max(0, Math.Max(a.Left - b.Right, b.Left - a.Right));
        RuinPlatform Lower(RuinPlatform upper) => platforms.Where(p => p.Y > upper.Y + 80 && Gap(p, upper) < 160)
            .OrderBy(p => p.Y - upper.Y + Gap(p, upper) * 0.6f).FirstOrDefault() ?? platforms[0];
        var links = new List<RuinLink>();
        foreach (var upper in platforms.Skip(1))
        {
            var lower = Lower(upper);
            float upperX = Math.Clamp(lower.Center, upper.Left + 28, upper.Right - 28);
            float lowerX = Math.Clamp(upperX, lower.Left + 28, lower.Right - 28);
            float min = Math.Max(lower.Left + 28, upper.Left + 28);
            float max = Math.Min(lower.Right - 28, upper.Right - 28);
            if (min <= max)
            {
                upperX = new[] { min, max }.OrderBy(x => exclusions.Sum(r => Overlap(r, new(x - 10, upper.Y, 20, lower.Y - upper.Y)))).First();
                lowerX = upperX;
            }
            links.Add(new(lower.Id, upper.Id, lowerX, true, upperX));
            links.Add(new(upper.Id, lower.Id, upperX, true, lowerX));
            decorations.Add(new(4, new(Math.Min(upperX, lowerX) - 12, upper.Y, Math.Abs(upperX - lowerX) + 24, lower.Y - upper.Y),
                Math.Max(lower.RevealAt, upper.RevealAt) - 0.2f, random.Next(2) == 0, 0.85f, random.NextSingle() * 6.28f,
                new(lowerX, lower.Y + 3), new(upperX, upper.Y - 2)));
            foreach (var target in platforms.Where(p => p.Id != upper.Id && p.Id != "ruin:floor"))
                if (RuinMotion.CanJump(upper, target, platforms)) links.Add(new(upper.Id, target.Id, upper.Center, false));
        }
        int variant = random.Next(6);
        foreach (var platform in platforms.Skip(1))
        {
            float width = platform.Right - platform.Left;
            // The new atlas uses square cells, with authored top contact heights.
            int tile = 6 + variant++ % 6;
            float scale = width / 470f;
            float topContact = tile < 9 ? 234 : 688 - 512;
            decorations.Add(new(tile, new(platform.Left - 20 * scale, platform.Y - topContact * scale, 512 * scale, 512 * scale), platform.RevealAt, random.Next(2) == 0));
            if (random.Next(3) == 0)
            {
                float plantWidth = 32 + random.Next(20);
                decorations.Add(new(5, new(platform.Right - plantWidth - 8, platform.Y - 25, plantWidth, 28), platform.RevealAt + 0.15f, random.Next(2) == 0, 0.88f, random.NextSingle() * 6.28f));
            }
            var arch = new RectangleF(platform.Left + width * 0.2f, platform.Y - 128, 110, 130);
            if (random.Next(5) == 0 && !exclusions.Any(r => r.IntersectsWith(arch)) && !platforms.Any(p => p.Id != platform.Id && p.Y < platform.Y && p.Y > arch.Top && p.Right > arch.Left && p.Left < arch.Right))
                decorations.Add(new(1, arch, platform.RevealAt + 0.1f, random.Next(2) == 0, 0.78f));
        }
        return new(size, platforms, links, decorations, ++_revision, _variation);
    }
    private static float Overlap(RectangleF a, RectangleF b)
    {
        RectangleF overlap = RectangleF.Intersect(a, b);
        return Math.Max(0, overlap.Width) * Math.Max(0, overlap.Height);
    }
}

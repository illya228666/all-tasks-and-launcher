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
        var windows = _seed.Where(s => !s.IsIcon).Select(s => Scale(s.Bounds)).ToArray();
        var platforms = new List<RuinPlatform> { new("ruin:floor", 0, size.Width, size.Height, 0) };
        var bridges = new List<RuinBridge>();
        var decorations = new List<RuinDecoration>();
        int identity = 1;
        float Reveal(float x, float y) => 2.1f * Math.Clamp(MathF.Sqrt((x - awakening.X) * (x - awakening.X) + (y - awakening.Y) * (y - awakening.Y))
            / Math.Max(1, MathF.Sqrt(size.Width * (float)size.Width + size.Height * (float)size.Height)), 0, 1);
        float Left(float center, float width) => Math.Clamp(center - width / 2, 16, size.Width - width - 16);
        bool Fits(float center, float y, float width)
        {
            if (size.Width < 180 || y < 85 || y > size.Height - 65 || platforms.Count >= 30) return false;
            width = Math.Min(width, size.Width - 32);
            float left = Left(center, width);
            return !platforms.Skip(1).Any(p => Math.Abs(p.Y - y) < 100 && left < p.Right + 16 && left + width > p.Left - 16);
        }
        RuinPlatform? Add(float center, float y, float width)
        {
            if (!Fits(center, y, width)) return null;
            width = Math.Min(width, size.Width - 32);
            float left = Left(center, width);
            var platform = new RuinPlatform($"ruin:{identity++}", left, left + width, y, Reveal(left + width / 2, y));
            platforms.Add(platform);
            return platform;
        }

        // Icon anchors belong inside the ruins rather than in exclusion zones.
        foreach (var icon in _seed.Where(s => s.IsIcon).OrderBy(s => s.Bounds.Top).Take(10))
        {
            RectangleF r = Scale(icon.Bounds);
            float iconY = Math.Clamp(r.Top + r.Height * 0.35f, 85, size.Height - 65);
            Add(r.Left + r.Width / 2, iconY, 95 + random.Next(46));
        }

        // Large fragments are scattered freely; only icon fragments stay tied to desktop positions.
        int largeCount = Math.Clamp((int)((long)size.Width * size.Height / 230_000), 4, 9);
        for (int placed = 0, attempt = 0; placed < largeCount && attempt < largeCount * 30; attempt++)
        {
            float width = Math.Min(190 + random.Next(101), Math.Max(95, size.Width * 0.3f));
            float center = 32 + random.NextSingle() * Math.Max(1, size.Width - 64);
            float y = placed == 0 ? size.Height - random.Next(110, 180)
                : 100 + random.NextSingle() * Math.Max(1, size.Height - 190);
            if (Add(center, y, width) is not null) placed++;
        }

        float Gap(RuinPlatform a, RuinPlatform b) => Math.Max(0, Math.Max(a.Left - b.Right, b.Left - a.Right));
        RuinPlatform Lower(RuinPlatform upper) => platforms.Where(p => p.Y > upper.Y + 65 && Gap(p, upper) < 260)
            .OrderBy(p => p.Y - upper.Y + Gap(p, upper) * 0.7f).FirstOrDefault() ?? platforms[0];

        var links = new List<RuinLink>();
        void Connect(RuinPlatform upper, RuinPlatform lower)
        {
            float upperX = Math.Clamp(lower.Center, upper.Left + RuinMotion.HalfBody, upper.Right - RuinMotion.HalfBody);
            float lowerX = Math.Clamp(upperX, lower.Left + RuinMotion.HalfBody, lower.Right - RuinMotion.HalfBody);
            float min = Math.Max(lower.Left + RuinMotion.HalfBody, upper.Left + RuinMotion.HalfBody);
            float max = Math.Min(lower.Right - RuinMotion.HalfBody, upper.Right - RuinMotion.HalfBody);
            if (min <= max) upperX = lowerX = new[] { min, max }.OrderBy(x => windows.Sum(w => Overlap(w, new(x - 10, upper.Y, 20, lower.Y - upper.Y)))).First();
            links.Add(new(lower.Id, upper.Id, lowerX, RuinLinkKind.Climb, upperX));
            links.Add(new(upper.Id, lower.Id, upperX, RuinLinkKind.Climb, lowerX));
            decorations.Add(new(4, new(Math.Min(upperX, lowerX) - 75, upper.Y, Math.Abs(upperX - lowerX) + 150, lower.Y - upper.Y),
                Math.Max(lower.RevealAt, upper.RevealAt) - 0.2f, random.Next(2) == 0, 0.85f, random.NextSingle() * 6.28f,
                new(lowerX, lower.Y), new(upperX, upper.Y)));
        }
        foreach (var upper in platforms.Skip(1))
        {
            var lower = Lower(upper);
            Connect(upper, lower);
            if (random.Next(2) == 0)
            {
                var second = platforms.Where(p => p.Id != lower.Id && p.Id != upper.Id && p.Id != "ruin:floor"
                    && p.Y > upper.Y + 65 && p.Y - upper.Y < 280 && Gap(p, upper) < 220)
                    .OrderBy(p => p.Y - upper.Y + Gap(p, upper) * 0.7f).FirstOrDefault();
                if (second is not null) Connect(upper, second);
            }
            foreach (var target in platforms.Where(p => p.Id != upper.Id && p.Id != "ruin:floor"))
                if (RuinMotion.CanJump(upper, target, platforms)) links.Add(new(upper.Id, target.Id, upper.Center, RuinLinkKind.Jump));
        }
        foreach (var platform in platforms.Skip(1))
        {
            float width = platform.Right - platform.Left;
            int tile = 6 + random.Next(6);
            float scale = width / 470f;
            float topContact = tile < 9 ? 234 : 688 - 512;
            decorations.Add(new(tile, new(platform.Left - 20 * scale, platform.Y - topContact * scale, 512 * scale, 512 * scale),
                platform.RevealAt, random.Next(2) == 0));
            if (random.Next(4) == 0)
            {
                float plantWidth = 25 + random.Next(16);
                decorations.Add(new(5, new(platform.Right - plantWidth - 8, platform.Y - 22, plantWidth, 25),
                    platform.RevealAt + 0.15f, random.Next(2) == 0, 0.88f, random.NextSingle() * 6.28f));
            }
            var arch = new RectangleF(platform.Left + width * 0.2f, platform.Y - 96, 80, 98);
            if (random.Next(7) == 0 && !windows.Any(r => r.IntersectsWith(arch)))
                decorations.Add(new(1, arch, platform.RevealAt + 0.1f, random.Next(2) == 0, 0.78f));
        }
        return new(size, platforms, links, bridges, decorations, ++_revision, _variation);
    }
    private static float Overlap(RectangleF a, RectangleF b)
    {
        RectangleF overlap = RectangleF.Intersect(a, b);
        return Math.Max(0, overlap.Width) * Math.Max(0, overlap.Height);
    }
}

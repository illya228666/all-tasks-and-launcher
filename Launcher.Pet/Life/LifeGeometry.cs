using System.Drawing;
using Launcher.Pet.Exploration;

namespace Launcher.Pet.Life;

public static class LifeGeometry
{
    public static void Validate(RuinScene scene)
    {
        bool Finite(float value) => float.IsFinite(value) && Math.Abs(value) <= 100000;
        if (scene is null || scene.Size.Width is < 64 or > 20000 || scene.Size.Height is < 64 or > 20000
            || scene.Metrics is { } metrics && (!float.IsFinite(metrics.CharacterHeight) || metrics.CharacterHeight is < 1 or > 4000)
            || scene.Platforms is null || scene.Links is null || scene.Bridges is null || scene.Decorations is null
            || scene.Platforms.Count is < 1 or > 256 || scene.Links.Count > 2048 || scene.Bridges.Count > 256 || scene.Decorations.Count > 2048
            || scene.Platforms.Any(p => p is null) || scene.Links.Any(l => l is null) || scene.Bridges.Any(b => b is null) || scene.Decorations.Any(d => d is null)
            || scene.Platforms.Select(p => p.Id).Distinct().Count() != scene.Platforms.Count
            || !scene.Platforms.Any(p => p.Id == "ruin:floor" && p.Left == 0 && p.Right == scene.Size.Width && p.Y == scene.Size.Height)
            || scene.Platforms.Any(p => string.IsNullOrEmpty(p.Id) || !Finite(p.Left) || !Finite(p.Right) || !Finite(p.Y)
                || p.Left < 0 || p.Right > scene.Size.Width || p.Right <= p.Left || p.Y < 0 || p.Y > scene.Size.Height || !Finite(p.RevealAt)))
            throw new InvalidDataException("Invalid saved ruins geometry.");
        var ids = scene.Platforms.Select(p => p.Id).ToHashSet();
        if (scene.Links.Any(l => !ids.Contains(l.From) || !ids.Contains(l.To) || !Finite(l.X)
                || l.EndX is float x && !Finite(x) || !Enum.IsDefined(typeof(RuinLinkKind), l.Kind))
            || scene.Bridges.Any(b => !ids.Contains(b.From) || !ids.Contains(b.To) || !Finite(b.Left) || !Finite(b.Right) || !Finite(b.Y) || !Finite(b.RevealAt))
            || scene.Decorations.Any(d => d.Tile is < 0 or > 11 || !Finite(d.Bounds.X) || !Finite(d.Bounds.Y)
                || !Finite(d.Bounds.Width) || !Finite(d.Bounds.Height) || !Finite(d.RevealAt) || !Finite(d.Phase) || !Finite(d.Opacity)
                || d.RootBottom is PointF bottom && (!Finite(bottom.X) || !Finite(bottom.Y))
                || d.RootTop is PointF top && (!Finite(top.X) || !Finite(top.Y))))
            throw new InvalidDataException("Invalid saved ruins links or decorations.");
    }

    public static RuinScene Reflow(RuinScene scene, Size size, Launcher.Pet.Exploration.DesktopWorldMetrics? metrics = null)
    {
        metrics ??= scene.Metrics is null ? null : DesktopWorldMetrics.ForWorkArea(size);
        if (scene.Size == size && scene.Metrics == metrics) return scene;
        if (metrics is not null) return ReflowForCharacter(scene, size, metrics);
        float sx = (float)size.Width / scene.Size.Width, sy = (float)size.Height / scene.Size.Height;
        PointF? Point(PointF? p) => p is PointF point ? new(point.X * sx, point.Y * sy) : null;
        var platforms = scene.Platforms.Select(p => p with { Left = p.Left * sx, Right = p.Right * sx, Y = p.Y * sy })
            .Where(p => p.Id == "ruin:floor" || p.Right - p.Left >= 48).ToArray();
        var ids = platforms.Select(p => p.Id).ToHashSet();
        var links = scene.Links.Where(l => ids.Contains(l.From) && ids.Contains(l.To))
            .Select(l => l with { X = l.X * sx, EndX = l.EndX * sx })
            .Where(l => l.Kind != RuinLinkKind.Jump || RuinMotion.CanJump(platforms.First(p => p.Id == l.From), platforms.First(p => p.Id == l.To), platforms)).ToArray();
        return scene with { Size = size, Platforms = platforms, Links = links,
            Bridges = scene.Bridges.Where(b => ids.Contains(b.From) && ids.Contains(b.To)).Select(b => b with { Left = b.Left * sx, Right = b.Right * sx, Y = b.Y * sy }).ToArray(),
            Decorations = scene.Decorations.Select(d => d with { Bounds = new(d.Bounds.X * sx, d.Bounds.Y * sy, d.Bounds.Width * sx, d.Bounds.Height * sy), RootBottom = Point(d.RootBottom), RootTop = Point(d.RootTop) }).ToArray(),
            Revision = scene.Revision + 1 };
    }

    private static RuinScene ReflowForCharacter(RuinScene scene, Size size, DesktopWorldMetrics metrics)
    {
        float sx = (float)size.Width / scene.Size.Width, sy = (float)size.Height / scene.Size.Height;
        float enlargement = metrics.MotionScale / scene.WorldMetrics.MotionScale;
        float half = metrics.HalfBody;
        var platforms = scene.Platforms.Select(p =>
        {
            if (p.Id == "ruin:floor") return p with { Left = 0, Right = size.Width, Y = size.Height };
            float width = Math.Min(size.Width, Math.Max(half * 2 + metrics.ItemDiameter + 8, (p.Right - p.Left) * enlargement));
            float left = Math.Clamp(p.Center * sx - width / 2, 0, Math.Max(0, size.Width - width));
            float y = Math.Clamp(p.Y * sy, Math.Min(size.Height, metrics.CharacterHeight + 12), size.Height);
            return p with { Left = left, Right = left + width, Y = y };
        }).ToArray();
        var placed = new List<RuinPlatform>();
        int columns = Math.Max(1, (int)(size.Width / (metrics.CharacterHeight * 2.4f)));
        for (int i = 0; i < platforms.Length; i++)
        {
            var platform = platforms[i];
            if (platform.Id == "ruin:floor") continue;
            float width = Math.Min(platform.Right - platform.Left, Math.Min(size.Width, metrics.CharacterHeight * 2.2f));
            float center = Math.Clamp(platform.Center, width / 2, size.Width - width / 2);
            platform = platform with { Left = center - width / 2, Right = center + width / 2 };
            bool Fits(RuinPlatform candidate) => candidate.Y >= metrics.CharacterHeight + 12 && candidate.Y <= size.Height - metrics.CharacterHeight * .35f
                && !placed.Any(p => Math.Abs(p.Y - candidate.Y) < metrics.CharacterHeight * .75f
                    && candidate.Left < p.Right + metrics.CharacterHeight * .12f && candidate.Right > p.Left - metrics.CharacterHeight * .12f);
            if (!Fits(platform))
            {
                bool found = false;
                for (int row = 0; row < 6 && !found; row++)
                    for (int col = 0; col < columns && !found; col++)
                    {
                        float x = size.Width * (col + .5f) / columns;
                        var candidate = platform with { Left = x - width / 2, Right = x + width / 2, Y = size.Height - metrics.CharacterHeight * (.55f + row * .95f) };
                        if (Fits(candidate)) { platform = candidate; found = true; }
                    }
                if (!found) platform = platform with { Y = size.Height };
            }
            platforms[i] = platform;
            if (platform.Y < size.Height) placed.Add(platform);
        }
        var bridges = new List<RuinBridge>();
        foreach (var left in platforms.Where(p => p.Id != "ruin:floor" && p.Y < size.Height).OrderBy(p => p.Left))
        {
            var right = platforms.Where(p => p.Left > left.Right + metrics.ItemDiameter && p.Y < size.Height
                && Math.Abs(p.Y - left.Y) < metrics.CharacterHeight * .08f && p.Left - left.Right < metrics.CharacterHeight * 2)
                .OrderBy(p => p.Left).FirstOrDefault();
            if (right is null || bridges.Count >= 3) continue;
            int index = Array.FindIndex(platforms, p => p.Id == right.Id);
            platforms[index] = right = right with { Y = left.Y };
            bridges.Add(new($"bridge:{left.Id}:{right.Id}", left.Id, right.Id, left.Right - half, right.Left + half, left.Y, Math.Max(left.RevealAt, right.RevealAt)));
        }
        var links = new List<RuinLink>();
        foreach (var bridge in bridges)
        {
            links.Add(new(bridge.From, bridge.To, bridge.Left, RuinLinkKind.Bridge, bridge.Right));
            links.Add(new(bridge.To, bridge.From, bridge.Right, RuinLinkKind.Bridge, bridge.Left));
        }
        foreach (var upper in platforms.Where(p => p.Id != "ruin:floor" && p.Y < size.Height))
        {
            var lower = platforms.Where(p => p.Y > upper.Y + metrics.CharacterHeight * .5f)
                .OrderBy(p => p.Y - upper.Y + Math.Abs(p.Center - upper.Center) * .35f).FirstOrDefault() ?? platforms.First(p => p.Id == "ruin:floor");
            float topX = Math.Clamp(lower.Center, upper.Left + half, upper.Right - half);
            float bottomX = Math.Clamp(topX, lower.Left + half, lower.Right - half);
            links.Add(new(lower.Id, upper.Id, bottomX, RuinLinkKind.Climb, topX));
            links.Add(new(upper.Id, lower.Id, topX, RuinLinkKind.Climb, bottomX));
            foreach (var target in platforms.Where(p => p.Id != upper.Id && p.Id != "ruin:floor"))
                if (RuinMotion.CanJump(upper, target, platforms, metrics)) links.Add(new(upper.Id, target.Id, upper.Center, RuinLinkKind.Jump));
        }
        // Modules keep their platform identities; flora and items are reanchored by LifeWorld.
        var decorations = new List<RuinDecoration>();
        foreach (var platform in platforms.Where(p => p.Id != "ruin:floor" && p.Y < size.Height))
        {
            var old = scene.Platforms.First(p => p.Id == platform.Id);
            var art = scene.Decorations.FirstOrDefault(d => d.Tile >= 6 && d.PlatformId == platform.Id)
                ?? scene.Decorations.Where(d => d.Tile >= 6 && d.PlatformId is null
                    && Math.Abs(d.Bounds.X-old.Left)<Math.Max(30,old.Right-old.Left))
                    .OrderBy(d => Math.Abs(d.Bounds.X-old.Left)+Math.Abs(d.Bounds.Bottom-old.Y)*.25f).FirstOrDefault();
            float width = platform.Right - platform.Left, scale = width / 470f;
            int tile = art?.Tile ?? 6 + StableVariant(scene.Variation, platform.Id, 6);
            float contact = tile < 9 ? 234 : 176;
            decorations.Add(new(tile, new(platform.Left - 20 * scale, platform.Y - contact * scale, 512 * scale, 512 * scale), platform.RevealAt)
            { AssetKey = art?.AssetKey ?? $"platform:{StableVariant(scene.Variation, platform.Id, 12)}", PlatformId = platform.Id });
        }
        foreach (var bridge in bridges)
            decorations.Add(new(3, new(bridge.Left, bridge.Y, bridge.Right - bridge.Left, metrics.CharacterHeight * .25f), bridge.RevealAt)
                { AssetKey = $"bridge:{StableVariant(scene.Variation, bridge.Id, 3)}" });
        foreach (var link in links.Where(l => l.Kind == RuinLinkKind.Climb && platforms.First(p => p.Id == l.From).Y > platforms.First(p => p.Id == l.To).Y))
        {
            var lower = platforms.First(p => p.Id == link.From); var upper = platforms.First(p => p.Id == link.To);
            var top = new PointF(link.EndX ?? link.X, upper.Y); var bottom = new PointF(link.X, lower.Y);
            decorations.Add(new(4, new(Math.Min(top.X, bottom.X) - 75 * metrics.MotionScale, top.Y,
                Math.Abs(top.X - bottom.X) + 150 * metrics.MotionScale, bottom.Y - top.Y), Math.Max(upper.RevealAt, lower.RevealAt), RootBottom: bottom, RootTop: top)
                { AssetKey = $"root:{StableVariant(scene.Variation, upper.Id, 3)}" });
        }
        foreach (var platform in platforms.Where(p => p.Id != "ruin:floor" && p.Y < size.Height))
        {
            int variant = StableVariant(scene.Variation, platform.Id + ":ornament", 8);
            float height = metrics.CharacterHeight * (variant < 2 ? .9f : .25f);
            float width = metrics.CharacterHeight * (variant < 2 ? .42f : .28f);
            if (variant < 2)
                decorations.Add(new(1, new(platform.Right - width, platform.Y - height, width, height), platform.RevealAt)
                    { AssetKey = $"arch:{StableVariant(scene.Variation, platform.Id, 4)}", PlatformId = platform.Id });
            else
                decorations.Add(new(2, new(platform.Left, platform.Y - height, width, height), platform.RevealAt)
                    { AssetKey = $"prop:{variant}", PlatformId = platform.Id });
        }
        for (int i=0;i<2;i++)
        {
            float height=metrics.CharacterHeight*.33f,width=height*.75f;
            decorations.Add(new(2,new(size.Width*(i==0?.08f:.92f)-width/2,size.Height-height,width,height),.9f)
                { AssetKey=$"prop:{i}",PlatformId="ruin:floor" });
        }
        return scene with { Size = size, Metrics = metrics, Platforms = platforms, Links = links, Bridges = bridges,
            Decorations = decorations, Revision = scene.Revision + 1 };
    }

    public static int StableVariant(int seed, string identity, int count)
    {
        uint hash = unchecked((uint)seed) ^ 2166136261;
        foreach (char c in identity) hash = unchecked((hash ^ c) * 16777619);
        return (int)(hash % (uint)count);
    }
}

using System.Drawing;
using System.Text.Json;
using Launcher.Pet.Exploration;
using Launcher.Pet.Life;

internal static class DesktopGraphicsChecks
{
    internal static void Run()
    {
        foreach (var size in new[] { new Size(1920, 1040), new Size(2560, 1400), new Size(3840, 2080), new Size(900, 1200) })
        {
            var metrics = DesktopWorldMetrics.ForWorkArea(size);
            Require(metrics.CharacterHeight == size.Height * .2f, "Desktop height must use physical work-area pixels.");
            var old = new RuinBuilder(new(1920, 1040), Array.Empty<DesktopSeed>(), 731).Build(new(1920, 1040), new(500, 1040));
            var life = LifeWorld.Create(old, 731);
            life.State.Learning.Releases = 2;
            life.State.Learning.TransferOutcomes = 3;
            long witnessed = life.State.Sites.First(s => s.Colony is not null).Colony!.Id;
            life.State.Learning.Deaths.Add(witnessed); life.State.Learning.Decompositions.Add(witnessed);
            string learning = JsonSerializer.Serialize(life.State.Learning);
            long matter = life.Matter;
            var random = (life.State.EcologyRandom, life.State.BehaviorRandom);
            var siteIds = life.State.Sites.Select(s => s.Id).ToArray();
            var scene = LifeGeometry.Reflow(old, size, metrics);
            LifeGeometry.Validate(scene);
            life.Reanchor(scene);
            Require(life.Matter == matter && (life.State.EcologyRandom, life.State.BehaviorRandom) == random
                && life.State.Sites.Select(s => s.Id).SequenceEqual(siteIds), "Graphics migration changed resources, random streams or sites.");
            Require(scene.Platforms.All(p => p.Right - p.Left >= metrics.HalfBody * 2), "Platform narrower than the body.");
            var reachable = LifeWorld.Reachable(scene, "ruin:floor");
            Require(life.State.Sites.All(s => reachable.Contains(s.Platform)), "Reflow stranded a life site.");
            Require(ReferenceEquals(scene, LifeGeometry.Reflow(scene, size, metrics)), "Stable geometry recreated on every tick.");
            var restored = JsonSerializer.Deserialize<RuinScene>(JsonSerializer.Serialize(scene))!;
            Require(restored.Metrics == metrics && restored.Decorations.Select(d => d.AssetKey).SequenceEqual(scene.Decorations.Select(d => d.AssetKey)), "Visual identities lost across saving.");
            foreach (int dpi in new[] { 96, 120, 144, 192 })
            {
                // WinForms is PerMonitorV2: the input area is already physical.
                // Convert drawing to DIPs once and back; never scale the world twice.
                float dipHeight = metrics.CharacterHeight * 96 / dpi;
                Require(Math.Abs(dipHeight * dpi / 96 - size.Height * .2f) < .01f, "DPI conversion changed the physical character height.");
            }
            var back = LifeGeometry.Reflow(scene, new(1920, 1040), DesktopWorldMetrics.ForWorkArea(new(1920, 1040)));
            life.Reanchor(back);
            Require(life.Matter == matter && life.State.Sites.Select(s => s.Id).SequenceEqual(siteIds)
                && JsonSerializer.Serialize(life.State.Learning) == learning, "Monitor changes lost world matter, learning or identities.");
            var variants = scene.Decorations.Where(d => d.Tile >= 6).ToDictionary(d => d.PlatformId!, d => d.AssetKey);
            Require(back.Decorations.Where(d => d.Tile >= 6 && variants.ContainsKey(d.PlatformId!)).All(d => variants[d.PlatformId!] == d.AssetKey),
                "Monitor change assigned another platform's artwork.");
        }
        Console.WriteLine("Adaptive desktop scale, shared physics and conservation during visual migration passed.");
    }
    private static void Require(bool result, string message) { if (!result) throw new Exception(message); }
}

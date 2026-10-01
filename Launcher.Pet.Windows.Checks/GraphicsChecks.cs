using System.Drawing;
using System.Text.Json;
using Launcher.Pet;
using Launcher.Pet.Data;
using Launcher.Pet.Exploration;
using Launcher.Pet.Life;
using Launcher.Pet.Sprites;
using Launcher.Pet.Windows.Drawing;

internal static class GraphicsChecks
{
    private static void Require(bool test, string message) { if (!test) throw new Exception(message); }
    internal static void Run()
    {
        using var images = new PetImages();
        var appearance = images.Appearance;
        Require(appearance.UsesClipFiles && appearance.Id == "sumrak", "Classic must load its named animation bundle.");
        var counts = new Dictionary<string, int>
        {
            ["idle"] = 12,
            ["idle-curious"] = 12,
            ["wave"] = 12,
            ["look-upper"] = 8,
            ["look-lower"] = 8,
            ["walk-right"] = 16,
            ["walk-left"] = 16,
            ["jump"] = 16,
            ["grab"] = 4,
            ["climb"] = 12,
            ["pull-up"] = 8,
            ["drag"] = 12,
            ["fall"] = 8,
            ["failed"] = 16,
            ["recovery"] = 16,
            ["hat-pickup"] = 12,
            ["observe"] = 12,
            ["pickup"] = 12,
            ["carry-right"] = 16,
            ["carry-left"] = 16,
            ["carry-climb"] = 12,
            ["place"] = 12,
            ["plant"] = 16,
            ["recycle"] = 16
        };
        Require(appearance.Clips.Count == counts.Count, "Unexpected animation set.");
        var paths = new HashSet<string>();
        foreach (var (name, count) in counts)
        {
            var clip = appearance.Clip(name)!;
            Require(clip.Durations.Length == count && clip.NoHatGeometry.Length == count, "Wrong frame count: " + name);
            for (int index = 0; index < count; index++) foreach (bool hat in new[] { true, false })
            {
                Require(paths.Add((hat ? clip.HatFrames : clip.NoHatFrames)[index]), "Repeated frame path.");
                var frame = images.Frame(new(PetMode.Idle, clip.Row, index, Rectangle.Empty, null, Point.Empty, hat, null!, null, 0) { Appearance = appearance });
                Require(frame.Image.Size == appearance.CellSize && !frame.Mask.Contains(0, 0), "Frame size or transparent hit area is wrong.");
                var geometry = appearance.GetFrameGeometry(clip.Row, index, hat);
                Require(geometry.ItemAnchor is { } hand && hand.X >= 0 && hand.X < frame.Image.Width && hand.Y >= 0 && hand.Y < frame.Image.Height,
                    "Missing hand landmark: " + name);
                Require(images.CachedBytes <= 128L * 1024 * 1024, "Frame cache escaped its memory bound.");
            }
        }
        Require(paths.Count == 600, "Incomplete production frames.");
        CheckWorld(images);
        images.ChangeAppearance(PetAppearance.Chibi);
        Require(!images.Appearance.UsesClipFiles && images.WithHat.Width > 0, "Second version no longer loads.");
        images.ChangeAppearance(PetAppearance.Original);
        Require(images.Appearance.UsesClipFiles && images.CachedBytes == 0, "Appearance replacement retained stale frames.");
        using var art = new RuinArtCatalog();
        foreach (var (prefix, count) in new[] { ("platform:", 12), ("arch:", 4), ("bridge:", 3), ("root:", 3), ("prop:", 8) })
            for (int i = 0; i < count; i++) Require(art.Find(prefix + i) is not null, "Missing gothic resource.");
        Console.WriteLine("Graphics: 600 frames, named phases, alpha hit areas, bounded cache, both characters, physical sizes and carried hand landmarks passed.");
    }
    private static void CheckWorld(PetImages images)
    {
        var old = new RuinBuilder(new(1920, 1040), Array.Empty<DesktopSeed>(), 731).Build(new(1920, 1040), new(500, 1040));
        var geometry = LifeGeometry.Reflow(old, old.Size, DesktopWorldMetrics.ForWorkArea(old.Size));
        // Force vertical traversal in this fixture; ordinary worlds may choose jumps instead.
        geometry = geometry with { Links = geometry.Links.Where(l => l.Kind != RuinLinkKind.Jump).ToArray() };
        var life = LifeWorld.Create(geometry, 731);
        var world = new PetWorld(new Random(731)); world.Start(0); world.SetAppearance(images.Appearance, 0);
        world.RestoreLife(new() { Geometry = geometry, Life = life.State, PetXFraction = .5f }, 0);
        PetEnvironment Env() => new(Point.Empty, geometry.Size.Width, geometry.Size.Height - 200, geometry.Size.Width,
            new(Point.Empty, geometry.Size), false, Point.Empty, Array.Empty<Rectangle>(), Array.Empty<Launcher.Pet.Hat.HatSurface>(),
            "ruin:floor", geometry.WorldMetrics.RenderScale, geometry, 10);
        var first = world.Update(0, Env());
        Require(Math.Abs(first.SpriteBounds.Height * 448f / 768 - geometry.WorldMetrics.CharacterHeight) < 2, "Standing physical height is not 20% of work area.");
        long matter = life.Matter; bool carry = false;
        for (long ms = 20; ms <= 3600000; ms += 20)
        {
            var scene = world.Update(ms, Env());
            var held = scene.Life!.Items.FirstOrDefault(i => i.Holder == LifeHolder.Pet);
            if (held is not null)
            {
                carry = true;
                var hand = scene.Appearance.GetFrameGeometry(scene.Row, scene.Frame, scene.HatAttached).ItemAnchor!.Value;
                var expected = new PointF(scene.SpriteBounds.Left + hand.X * scene.SpriteBounds.Width / (float)scene.Appearance.CellSize.Width,
                    scene.SpriteBounds.Top + hand.Y * scene.SpriteBounds.Height / (float)scene.Appearance.CellSize.Height);
                Require(Math.Abs(held.Position.X - expected.X) < 1 && Math.Abs(held.Position.Y - expected.Y) < 1, "Carried item detached from its authored hand.");
            }
            if (ms % 10000 == 0)
            {
                var state = world.CaptureLife()!.Life;
                Require(new LifeWorld(state).Matter == matter, "Visual actions changed matter.");
                if (ms > 600000 && carry && state.Learning.Plant && state.Learning.Recycle) break;
            }
        }
        var checkpoint = world.CaptureLife();
        Require(carry && checkpoint!.Life.Learning.Plant && checkpoint.Life.Learning.Recycle,
            $"Modern autonomy failed: carry={carry}, learning={JsonSerializer.Serialize(checkpoint!.Life.Learning)}.");
        Require(checkpoint is not null, "Modern world cannot be saved.");
        var roundtrip = JsonSerializer.Deserialize<PetLifeSave>(JsonSerializer.Serialize(checkpoint))!;
        Require(roundtrip.Life.Learning.Plant && roundtrip.Life.Learning.Recycle, "Saving lost modern learning.");
        CheckClimbing(images);
    }
    private static void CheckClimbing(PetImages images)
    {
        var geometry = new RuinScene(new(1920, 1040), new[] { new RuinPlatform("ruin:floor", 0, 1920, 1040, 0), new RuinPlatform("shelf", 650, 1150, 600, 0) },
            new[] { new RuinLink("ruin:floor", "shelf", 850, RuinLinkKind.Climb, 850), new RuinLink("shelf", "ruin:floor", 850, RuinLinkKind.Climb, 850) },
            Array.Empty<RuinBridge>(), Array.Empty<RuinDecoration>(), 1, 731)
        { Metrics = DesktopWorldMetrics.ForWorkArea(new(1920, 1040)) };
        var life = LifeWorld.Create(geometry, 731);
        var source = life.State.Sites.First(s => s.Platform == "ruin:floor");
        var destination = life.State.Sites.First(s => s.Platform == "shelf" && s.Colony is null);
        source.Nutrients -= 1000;
        var item = new LifeItem { Id = life.State.NextId++, Site = source.Id, Kind = LifeItemKind.Spore, Holder = LifeHolder.Pet, Mass = 1000, OriginalMass = 1000, Duration = 900 };
        life.State.Items.Add(item);
        life.State.Learning.Releases = 2;
        life.State.Intent = new() { Kind = LifeTaskKind.Carry, Site = source.Id, Destination = destination.Id, Item = item.Id, PickedUp = true };
        long matter = life.Matter;
        var world = new PetWorld(new Random(731)); world.Start(0); world.SetAppearance(images.Appearance, 0);
        world.RestoreLife(new() { Geometry = geometry, Life = life.State, PetXFraction = .44f }, 0);
        var env = new PetEnvironment(Point.Empty, 1920, 840, 1920, new(Point.Empty, geometry.Size), false, Point.Empty, Array.Empty<Rectangle>(),
            Array.Empty<Launcher.Pet.Hat.HatSurface>(), "ruin:floor", geometry.WorldMetrics.RenderScale, geometry, 10);
        PetScene? climbing = null;
        for (long ms = 0; ms < 30000; ms += 20)
        {
            var scene = world.Update(ms, env);
            if (scene.Appearance.Clip(scene.Row)?.Name == "carry-climb" && scene.Life!.Items.Any(i => i.Holder == LifeHolder.Pet)) { climbing = scene; break; }
        }
        Require(climbing is not null, "Carried item cannot use the climbing clip.");
        var hand = climbing!.Appearance.GetFrameGeometry(climbing.Row, climbing.Frame, climbing.HatAttached).ItemAnchor!.Value;
        var held = climbing.Life!.Items.Single(i => i.Holder == LifeHolder.Pet);
        Require(Math.Abs(held.Position.X - climbing.SpriteBounds.Left - hand.X * climbing.SpriteBounds.Width / 768f) < 1, "Climbing detached the held item.");
        Require(!world.BeginWorldItemDrag(item.Id, Point.Round(held.Position)), "User took an item out of the character's hands.");
        Require(world.BeginPetDrag(Point.Round(held.Position), 30000), "Cannot interrupt carried climb.");
        var state = world.CaptureLife()!.Life;
        Require(new LifeWorld(state).Matter == matter && !state.Items.Any(i => i.Holder == LifeHolder.Pet), "Interrupting climb lost the carried matter.");
        world.DropPet(30000); world.Update(30020, env);
        Require(world.BeginWorldItemDrag(item.Id, new(400, 400)), "Released matter cannot be moved by the user.");
        world.MoveWorldItem(new(100, 100)); world.DropWorldItem();
        Require(new LifeWorld(world.CaptureLife()!.Life).Matter == matter, "User movement duplicated or removed matter.");
    }
}

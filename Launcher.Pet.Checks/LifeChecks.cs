using System.Drawing;
using System.Text.Json;
using Launcher.Pet;
using Launcher.Pet.Data;
using Launcher.Pet.Exploration;
using Launcher.Pet.Life;

internal static class LifeChecks
{
    private static string Json<T>(T state) => JsonSerializer.Serialize(state);
    private static T Clone<T>(T state) => JsonSerializer.Deserialize<T>(Json(state))!;
    private static void Require(bool test, string message) { if (!test) throw new Exception(message); }
    internal static RuinScene Scene() => new(new(800, 650), new[] { new RuinPlatform("ruin:floor", 0, 800, 650, 0),
        new RuinPlatform("shelf", 230, 540, 410, 0) }, new[] { new RuinLink("ruin:floor", "shelf", 380, RuinLinkKind.Climb, 380),
        new RuinLink("shelf", "ruin:floor", 380, RuinLinkKind.Climb, 380) }, Array.Empty<RuinBridge>(), Array.Empty<RuinDecoration>(), 1, 731);
    private static PetEnvironment Environment(RuinScene scene) => new(Point.Empty, scene.Size.Width, scene.Size.Height - PetLogicalGeometry.Height,
        scene.Size.Width, new(Point.Empty, scene.Size), false, Point.Empty, Array.Empty<Rectangle>(), Array.Empty<Launcher.Pet.Hat.HatSurface>(), "ruin:floor", .5f, scene, 10);

    internal static void Check()
    {
        CheckDeterminismAndMatter();
        CheckExtinction();
        CheckLearning();
        CheckObservationTiming();
        CheckWorldAndInterruptions();
        CheckInterruptedCarry();
        CheckAutonomy();
        CheckInvalidStates();
        Console.WriteLine("Desktop life: deterministic ecology, conserved matter, renewal, learning, interruption, persistence and autonomy checks passed.");
    }
    private static void CheckDeterminismAndMatter()
    {
        var a = LifeWorld.Create(Scene(), 42);
        var b = LifeWorld.Create(Scene(), 42);
        Require(Json(a.State) == Json(b.State), "Same seed must create the same world.");
        Require(Json(a.State) != Json(LifeWorld.Create(Scene(), 43).State), "Different seeds must vary the world.");
        long matter = a.Matter;
        for (int second = 0; second < 21600; second++)
        {
            a.Advance(1);
            b.Advance(.25); b.Advance(.75);
            Require(a.Matter == matter, $"Matter changed at {second}.");
            Require(a.State.Items.Count <= a.State.Profile.MaxItems + 12 && a.State.Sites.Count == 12, "Entities escaped their bounds.");
            if (second % 300 == 0)
            {
                Require(Json(a.State) == Json(b.State), "Fixed-step ecology depends on rendering cadence.");
                LifeWorld.Validate(a.State);
            }
        }
        var saved = new LifeWorld(Clone(a.State));
        for (int i = 0; i < 600; i++) { a.Advance(1); saved.Advance(1); }
        Require(Json(a.State) == Json(saved.State), "Saved ecology must resume exactly, including RNG state.");
    }
    private static void CheckExtinction()
    {
        var world = LifeWorld.Create(Scene(), 5);
        long matter = world.Matter;
        world.State.Learning.Releases = 2;
        foreach (var site in world.State.Sites) if (site.Colony is { } colony) colony.Lifespan = 1;
        world.Advance(1);
        Require(world.State.Sites.All(s => s.Colony is null), "Forced extinction did not remove all colonies.");
        world.Advance(119);
        Require(world.State.Generation == 2 && world.State.Sites.Count(s => s.Colony is not null) == 1,
            "Extinction must renew after the quiet period without a user.");
        Require(world.Matter == matter && world.State.Learning.Carry, "Renewal reset matter or learning.");
    }
    private static void CheckLearning()
    {
        var world = LifeWorld.Create(Scene(), 6);
        var s = world.State;
        long Add(LifeEventKind kind, long related = 0)
        {
            long id = s.NextId++;
            s.Events.Add(new() { Id = id, Kind = kind, Site = s.Sites[0].Id, Related = related });
            return id;
        }
        long release = Add(LifeEventKind.Released);
        Require(!s.Learning.Carry && world.Observe(release) && !world.Observe(release) && !s.Learning.Carry,
            "One release must not unlock carrying or count twice.");
        world.Observe(Add(LifeEventKind.Released));
        Require(s.Learning.Carry && !s.Learning.Plant, "Carrying needs two distinct releases.");
        world.Observe(Add(LifeEventKind.Established));
        Require(s.Learning.TransferOutcomes == 0, "Natural colonization is not a personal transfer.");
        for (int i = 0; i < 3; i++) world.Observe(Add(i % 2 == 0 ? LifeEventKind.Established : LifeEventKind.Failed, s.NextId++));
        Require(s.Learning.Plant && !s.Learning.Recycle, "Planting needs three transfer outcomes.");
        for (int i = 0; i < 2; i++)
        {
            long death = s.NextId++;
            world.Observe(Add(LifeEventKind.Died, death));
            world.Observe(Add(LifeEventKind.Decomposed, death));
            Require(s.Learning.Recycle == (i == 1), "Recycling needs two paired death/decomposition observations.");
        }
        Require(Clone(s).Learning.Recycle, "Learning was lost in serialization.");
    }
    private static PetWorld RestoredWorld(RuinScene scene, LifeWorld life)
    {
        var world = new PetWorld(new Random(9));
        world.Start(0);
        world.RestoreLife(new() { Geometry = scene, Life = life.State, PetPlatform = "ruin:floor", PetXFraction = .3f }, 0);
        world.Update(0, Environment(scene));
        return world;
    }
    private static void CheckObservationTiming()
    {
        var scene = Scene();
        var life = LifeWorld.Create(scene, 3);
        var site = life.State.Sites[0];
        long id = life.State.NextId++;
        life.State.Events.Add(new() { Id = id, Site = site.Id, Kind = LifeEventKind.Released });
        life.State.Intent = new() { Kind = LifeTaskKind.Observe, Site = site.Id, Event = id };
        var world = new PetWorld(new Random(3)); world.Start(0);
        world.RestoreLife(new() { Geometry = scene, Life = life.State, PetXFraction = LifeWorld.Position(site, scene).X / scene.Size.Width }, 0);
        var env = Environment(scene);
        world.Update(0, env);
        for (long ms = 20; ms <= 1800; ms += 20) world.Update(ms, env);
        Require(world.CaptureLife()!.Life.Learning.Releases == 0, "Observation was counted before two seconds nearby.");
        for (long ms = 1820; ms <= 2200; ms += 20) world.Update(ms, env);
        Require(world.CaptureLife()!.Life.Learning.Releases == 1, "Completed two-second observation was not counted.");
        for (long ms = 2220; ms <= 8000; ms += 20) world.Update(ms, env);
        Require(world.CaptureLife()!.Life.Learning.Releases == 1, "The same event was learned repeatedly.");
    }
    private static void CheckWorldAndInterruptions()
    {
        var scene = Scene();
        var life = LifeWorld.Create(scene, 7);
        var site = life.State.Sites[0];
        site.Nutrients -= 500;
        var item = new LifeItem { Id = life.State.NextId++, Site = site.Id, Kind = LifeItemKind.Spore, Mass = 500, OriginalMass = 500, Duration = 60 };
        life.State.Items.Add(item);
        var world = RestoredWorld(scene, life);
        Require(world.BeginWorldItemDrag(item.Id, new(300, 300)), "World item could not be dragged.");
        Require(!world.BeginPetDrag(new(300, 300), 0) && !world.BeginHatDrag(new(300, 300)), "Concurrent drags were allowed.");
        world.MoveWorldItem(new(5000, -5000)); world.DropWorldItem();
        var saved = world.CaptureLife()!;
        Require(saved.Life.Items.Single(i => i.Id == item.Id).Holder == LifeHolder.World
            && new LifeWorld(saved.Life).Matter == life.Matter, "Dropping lost or duplicated matter.");
        Require(world.Location == PetLocation.Desktop, "Restoring life repeated launcher departure.");
        world.Update(20000, Environment(scene));
        Require(world.CaptureLife()!.Life.Seconds == saved.Life.Seconds, "A suspended computer advanced life.");
        world.Stop(20000);
        world.Update(8000000, Environment(scene));
        Require(world.CaptureLife()!.Life.Seconds == saved.Life.Seconds, "Stopped life advanced.");
        world.Start(8000000); world.Update(8001000, Environment(scene));
        Require(world.CaptureLife()!.Life.Seconds == saved.Life.Seconds + 1, "Resume did not continue the paused clock.");
        var smaller = LifeGeometry.Reflow(scene, new(320, 240));
        world.Update(8001010, Environment(smaller));
        var reflowed = world.CaptureLife()!;
        Require(new LifeWorld(reflowed.Life).Matter == life.Matter, "Reflow changed matter.");
        LifeGeometry.Validate(reflowed.Geometry);
        var restored = new PetWorld(new Random(1)); restored.Start(999999999);
        restored.RestoreLife(Clone(reflowed), 999999999); restored.Update(999999999, Environment(smaller));
        Require(restored.CaptureLife()!.Life.Seconds == reflowed.Life.Seconds, "Offline time leaked into restore.");
    }
    private static void CheckAutonomy()
    {
        foreach (ulong seed in new ulong[] { 1, 12, 42 })
        {
            var scene = seed == 42 ? new RuinBuilder(new(1920, 1080), Array.Empty<DesktopSeed>(), 42).Build(new(1920, 1080), new(900, 1080)) : Scene();
            var world = RestoredWorld(scene, LifeWorld.Create(scene, seed));
            var env = Environment(scene);
            long firstCarry = 0;
            for (long ms = 20; ms <= 3600000; ms += 20)
            {
                var frame = world.Update(ms, env);
                if (firstCarry == 0 && frame.Life!.Items.Any(i => i.Holder == LifeHolder.Pet && i.Kind == LifeItemKind.Spore)) firstCarry = ms;
            }
            var saved = world.CaptureLife()!;
            Require(firstCarry is > 0 and <= 600000, $"No visible carrying in the first ten minutes for seed {seed}: {firstCarry} ms.");
            Require(saved.Life.Learning.Recycle, $"Autonomous pet failed to learn all skills for seed {seed}: " + Json(saved.Life.Learning));
            Require(saved.Life.TotalMatter == new LifeWorld(saved.Life).Matter, "Character actions changed the matter budget.");
            Console.WriteLine($"  Autonomous hour, seed {seed}: first carry at {firstCarry / 1000}s; all skills learned; {saved.Life.Items.Count} items, {saved.Life.Generation} generations.");
        }
    }
    private static void CheckInterruptedCarry()
    {
        var scene = Scene();
        var life = LifeWorld.Create(scene, 8);
        var source = life.State.Sites[0];
        source.Nutrients -= 500;
        var item = new LifeItem { Id = life.State.NextId++, Site = source.Id, Kind = LifeItemKind.Spore, Mass = 500, OriginalMass = 500, Duration = 60, Holder = LifeHolder.Pet };
        life.State.Items.Add(item);
        life.State.Learning.Releases = 2;
        life.State.Intent = new() { Kind = LifeTaskKind.Carry, Site = source.Id, Destination = life.State.Sites[2].Id, Item = item.Id, PickedUp = true };
        var world = RestoredWorld(scene, life);
        var env = Environment(scene);
        var frame = world.Update(10, env);
        Require(frame.Life!.Items.Single(i => i.Id == item.Id).Holder == LifeHolder.Pet, "Restoring a carried item lost ownership.");
        Require(world.BeginPetDrag(new(300, 600), 10), "Pet drag could not interrupt a life activity.");
        var saved = world.CaptureLife()!;
        var dropped = saved.Life.Items.Single(i => i.Id == item.Id);
        Require(saved.Life.Intent is null && dropped.Holder == LifeHolder.World && dropped.Transfer == 0
            && new LifeWorld(saved.Life).Matter == life.Matter, "Cancelled transfer granted a result or lost matter.");
        world.DropPet(20);
        world.Update(40, env);
        Require(world.CaptureLife()!.Life.Intent is null, "Physical recovery must not start a new life task.");
    }
    private static void CheckInvalidStates()
    {
        var state = LifeWorld.Create(Scene(), 99).State;
        state.Sites[0].Nutrients++;
        try { LifeWorld.Validate(state); throw new Exception("Corrupt budget was accepted."); }
        catch (InvalidDataException) { }
        var bad = LifeWorld.Create(Scene(), 99).State;
        bad.Profile.DecompositionMin = 0;
        try { LifeWorld.Validate(bad); throw new Exception("Invalid profile was accepted."); }
        catch (InvalidDataException) { }
    }
}

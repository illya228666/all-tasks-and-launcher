using System.Drawing;
using Launcher.Pet;
using Launcher.Pet.Data;
using Launcher.Pet.Exploration;
using Launcher.Pet.Hat;

CheckLauncher();
CheckLanding(250, true);
CheckLanding(500, false);
CheckRuinLayout();
CheckHatRoute();
Console.WriteLine("Pet checks passed.");

static void CheckLauncher()
{
    var world = new PetWorld(new Random(1));
    world.Start(0);
    if (world.BeginPetDrag(Point.Empty, 0) || !world.BeginHatDrag(Point.Empty))
        throw new Exception("Launcher must allow hat dragging only.");
    world.Stop(0);
}

static void CheckLanding(int expectedY, bool withHat)
{
    var world = new PetWorld(new Random(1));
    var platforms = new List<RuinPlatform> { new("ruin:floor", 0, 500, 500, 0) };
    if (expectedY != 500)
        platforms.Add(new("shelf", 0, 500, expectedY, 0));
    var ruins = new RuinScene(new Size(500, 500), platforms, Array.Empty<RuinLink>(), Array.Empty<RuinBridge>(), Array.Empty<RuinDecoration>(), 1);
    var environment = new PetEnvironment(Point.Empty, 500, 500 - PetLogicalGeometry.Height, 500,
        new Rectangle(0, 0, 500, 500), false, Point.Empty, Array.Empty<Rectangle>(),
        Array.Empty<HatSurface>(), "ruin:floor", 0.5f, ruins, 3);
    var launcher = environment with { Scale = 1, Ruins = null };
    world.Start(0);
    world.Update(0, launcher);
    if (!world.LeaveLauncher(0))
        throw new Exception("Could not leave Launcher.");

    long now = 0;
    while (world.Location != PetLocation.Desktop && now < 5000)
        world.Update(now += 10, environment);
    if (world.Location != PetLocation.Desktop)
        throw new Exception("Pet never reached the desktop.");
    world.Update(now += 10, environment);
    if (!withHat)
    {
        world.BeginHatDrag(Point.Empty);
        world.DropHat(false);
    }

    Rectangle bounds = world.Scene!.SpriteBounds;
    Point press = new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
    if (!world.BeginPetDrag(press, now))
        throw new Exception("Could not begin pet drag.");
    world.MovePet(new Point(press.X, press.Y - 400));
    world.Update(now += 10, environment);
    if (!world.IsPetDragging || world.Scene!.Mode != PetMode.Dragging || world.Scene.HatAttached != withHat)
        throw new Exception("Dragging state was not shown.");
    world.DropPet(now);

    while (world.Scene!.Mode != PetMode.Recovering && now < 8000)
        world.Update(now += 10, environment);
    if (world.Scene!.Mode != PetMode.Recovering || Math.Abs(world.Scene.SpriteBounds.Bottom - expectedY) > 1)
        throw new Exception($"Expected landing at {expectedY}, got {world.Scene.SpriteBounds.Bottom}.");
    world.Update(now + 1300, environment);
    if (world.Scene!.Mode != PetMode.Idle)
        throw new Exception("Pet did not stand up after landing.");
    world.Stop(now + 1300);
}

static void CheckRuinLayout()
{
    var size = new Size(1280, 760);
    var icon = new Rectangle(36, 200, 40, 40);
    var seed = new[] { new DesktopSeed(icon, new(25, 245, 65, 28), true) };
    RuinScene scene = new RuinBuilder(size, seed, 731).Build(size, new(640, 760));
    if (scene.Bridges.Count != 0 || scene.Platforms.Skip(1).Count(p => p.Right - p.Left >= 180) < 4
        || !scene.Platforms.Skip(1).Any(p => p.Right - p.Left <= 140 && p.Left < icon.Right && p.Right > icon.Left && Math.Abs(p.Y - icon.Top) < 60))
        throw new Exception("Ruins need scattered large fragments and a small icon fragment without stretched bridges.");
    var reached = new HashSet<string> { "ruin:floor" };
    var queue = new Queue<string>();
    queue.Enqueue("ruin:floor");
    while (queue.TryDequeue(out string? from))
        foreach (var link in scene.Links.Where(l => l.From == from && reached.Add(l.To)))
            queue.Enqueue(link.To);
    if (reached.Count != scene.Platforms.Count)
        throw new Exception("A ruin fragment is unreachable from the floor.");
    PointF top = new(120, 180), bottom = new(240, 410);
    if (Math.Abs(RuinMotion.ClimbX(top, bottom, top.Y) - top.X) > 0.01f
        || Math.Abs(RuinMotion.ClimbX(top, bottom, bottom.Y) - bottom.X) > 0.01f
        || Math.Abs(RuinMotion.ClimbX(top, bottom, 260) - (top.X + (bottom.X - top.X) * 80 / 230)) < 5)
        throw new Exception("Climbing root must bend while meeting both platforms.");
}

static void CheckHatRoute()
{
    var size = new Size(600, 500);
    var platforms = new[] { new RuinPlatform("ruin:floor", 0, 600, 500, 0), new RuinPlatform("upper", 250, 380, 300, 0) };
    var links = new[] { new RuinLink("ruin:floor", "upper", 315, RuinLinkKind.Climb, 315),
        new RuinLink("upper", "ruin:floor", 315, RuinLinkKind.Climb, 315) };
    var ruins = new RuinScene(size, platforms, links, Array.Empty<RuinBridge>(), Array.Empty<RuinDecoration>(), 1);
    var surfaces = platforms.Select(p => new HatSurface(p.Id, HatSurfaceKind.Ruin,
        new Rectangle((int)p.Left, (int)p.Y, (int)(p.Right - p.Left), 1))).ToArray();
    var environment = new PetEnvironment(Point.Empty, 600, 500 - PetLogicalGeometry.Height, 600,
        new Rectangle(Point.Empty, size), false, Point.Empty, Array.Empty<Rectangle>(), surfaces,
        "ruin:floor", 0.5f, ruins, 10);
    var world = new PetWorld(new Random(2));
    world.Start(0);
    world.Update(0, environment with { Scale = 1, Ruins = null });
    if (!world.LeaveLauncher(0)) throw new Exception("Could not leave Launcher for hat route check.");
    long now = 0;
    while (world.Location != PetLocation.Desktop && now < 5000) world.Update(now += 10, environment);
    if (world.Location != PetLocation.Desktop || !world.BeginHatDrag(new(315, 285)))
        throw new Exception("Could not drop hat onto upper ruin.");
    world.DropHat(false);
    bool routed = false;
    for (int i = 0; i < 300 && !routed; i++)
    {
        world.Update(now += 10, environment);
        routed = world.PlannedHatTarget is not null && world.PlannedRoute.Any(link => link.To == "upper");
    }
    if (!routed) throw new Exception("Pet did not plan a route to the resting hat.");
    world.Stop(now);
}

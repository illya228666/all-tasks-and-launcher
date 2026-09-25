using System.Drawing;
using Launcher.Pet;
using Launcher.Pet.Data;
using Launcher.Pet.Exploration;
using Launcher.Pet.Hat;

CheckLauncher();
CheckLanding(250, true);
CheckLanding(500, false);
Console.WriteLine("Pet drag/drop checks passed.");

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
    var ruins = new RuinScene(new Size(500, 500), platforms, Array.Empty<RuinLink>(), Array.Empty<RuinDecoration>(), 1);
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

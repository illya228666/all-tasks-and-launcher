using System.Drawing;
using Launcher.Pet;
using Launcher.Pet.Activities;
using Launcher.Pet.Data;
using Launcher.Pet.Exploration;
using Launcher.Pet.Hat;
using Launcher.Pet.Speech;

internal static class ActivityChecks
{
    internal static void Check()
    {
        CheckEarthquake();
        CheckAirborneInterruptions();
        CheckComposition(false);
        CheckComposition(true);
        string[] forbidden = { "System.Windows.Forms", "System.Drawing.Common" };
        if (typeof(PetWorld).Assembly.GetReferencedAssemblies().Any(a => forbidden.Contains(a.Name)))
            throw new Exception("The domain acquired a Windows rendering dependency.");
        Console.WriteLine("Activity composition, interruption and platform-boundary checks passed.");
    }

    private static PetEnvironment Environment(RuinScene? ruins = null) => new(Point.Empty, 800, 650 - PetLogicalGeometry.Height, 800,
        new Rectangle(0, 0, 800, 650), false, Point.Empty, Array.Empty<Rectangle>(), Array.Empty<HatSurface>(),
        "ruin:floor", ruins is null ? 1 : 0.5f, ruins, 10);

    private static void CheckEarthquake()
    {
        var world = new PetWorld(new Random(3));
        world.Start(0);
        var env = Environment();
        world.Update(0, env);
        if (!world.TryStartEarthquake(100) || world.TryStartEarthquake(101)
            || world.BeginHatDrag(Point.Empty) || world.Scene!.HatAttached
            || world.Scene.HatInteractionEnabled || !world.Scene.ShakeWindow)
            throw new Exception("Earthquake entry, hat interaction or rendering policy changed.");
        var last = world.Update(5099, env);
        var done = world.Update(5100, env);
        if (last.Mode != PetMode.Earthquake || !last.ShakeWindow || done.Mode == PetMode.Earthquake
            || done.ShakeWindow || !done.HatInteractionEnabled || done.WindowShake != Point.Empty)
            throw new Exception("Earthquake must end at exactly 5000 ms and clear its rendering effects.");
    }

    private static void CheckAirborneInterruptions()
    {
        foreach (PetMode mode in new[] { PetMode.Grabbing, PetMode.Climbing, PetMode.PullingUp, PetMode.Traversing })
        {
            var random = new Random(4);
            var actor = new PetActor(random, new HatWorld(), new PetSpeech(random));
            var ruins = new RuinScene(new(800, 650), new[] { new RuinPlatform("ruin:floor", 0, 800, 650, 0),
                new RuinPlatform("upper", 250, 550, 400, 0) }, Array.Empty<RuinLink>(), Array.Empty<RuinBridge>(), Array.Empty<RuinDecoration>(), 1);
            var env = Environment(ruins);
            actor.Location = PetLocation.Desktop;
            actor.Body.X = 350;
            actor.Update(new(actor, env, 0, 0));
            actor.Navigation.FootY = 500;
            var link = new RuinLink("ruin:floor", "upper", 400, RuinLinkKind.Climb, 400);
            PetActivity activity = mode switch
            {
                PetMode.Grabbing => new GrabActivity(link),
                PetMode.Climbing => new ClimbingActivity(link),
                PetMode.PullingUp => new PullUpActivity(link),
                _ => new FlightActivity(10, -100)
            };
            actor.ContinueWith(activity, 100);
            actor.Update(new(actor, env, 110, 0.01f));
            float lift = actor.Body.JumpLift;
            if (!EarthquakeActivity.TryStart(new(actor, env, 111, 0), new Point(400, 450))
                || actor.Activity is not FlightActivity || actor.Hat.Attached || actor.Shake != Point.Empty
                || actor.Body.JumpLift != lift)
                throw new Exception($"Earthquake must knock off the hat and replace {mode} with a fresh fall without teleporting.");
        }
    }

    private static void CheckComposition(bool desktop)
    {
        var random = new Random(7);
        var actor = new PetActor(random, new HatWorld(), new PetSpeech(random));
        var env = Environment();
        if (desktop)
        {
            actor.Location = PetLocation.Desktop;
            var ruins = new RuinScene(new(800, 650), new[] { new RuinPlatform("ruin:floor", 0, 800, 650, 0) },
                Array.Empty<RuinLink>(), Array.Empty<RuinBridge>(), Array.Empty<RuinDecoration>(), 1);
            env = Environment(ruins);
        }
        actor.Body.X = 300;
        actor.Reset(0);
        actor.Update(new(actor, env, 0, 0));
        var next = new ProbeActivity(null);
        var first = new ProbeActivity(next);
        actor.Change(first, 100);
        // This legacy label is intentionally unrelated to the activity. It must
        // never influence dispatch, interruption policy or Windows presentation.
        actor.Body.Mode = PetMode.Earthquake;
        actor.Update(new(actor, env, 150, 0.05f));
        if (first.Ticks != 1 || actor.Body.Frame != 2 || !actor.Activity.AllowsHatDrag || actor.Activity.ShakeWindow)
            throw new Exception("A new activity required central mode dispatch.");
        actor.Update(new(actor, env, 200, 0.05f));
        if (!ReferenceEquals(actor.Activity, next) || next.Ticks != 0)
            throw new Exception("Continuation must run on the next tick, not recursively in the current tick.");
        actor.Update(new(actor, env, 250, 0.05f));
        if (next.Ticks != 1 || actor.Body.Frame != 2)
            throw new Exception("A composed activity was not dispatched.");
    }

    // Test-only mechanic: no PetMode member, coordinator branch or platform code.
    private sealed class ProbeActivity : PetActivity
    {
        private readonly PetActivity? _next;
        internal ProbeActivity(PetActivity? next) => _next = next;
        internal int Ticks { get; private set; }
        internal override void Update(PetActivityContext c)
        {
            Ticks++;
            c.Body.Row = 0;
            c.Body.Frame = 2;
            if (c.Now - StartedAtMs >= 100 && _next is not null) c.Actor.ContinueWith(_next, c.Now);
        }
    }
}

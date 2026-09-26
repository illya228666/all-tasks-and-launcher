using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Launcher.Pet;
using Launcher.Pet.Data;
using Launcher.Pet.Exploration;
using Launcher.Pet.Hat;

// Captured from the pre-refactor implementation. Includes every rendered frame,
// geometry, hat/speech output, route, command result and shared Random call.
internal static class BehaviorTraces
{
    internal static void Check(string[] args)
    {
        using var stream = typeof(BehaviorTraces).Assembly.GetManifestResourceStream("Launcher.Pet.Checks.BehaviorTraces.sha256")!;
        using var reader = new StreamReader(stream);
        var expected = reader.ReadToEnd().Split('\n').Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#')).Select(line => line.Split(' '))
            .ToDictionary(parts => parts[0], parts => parts[1]);
        var failures = new List<string>();
        foreach (var (scenario, baseline) in expected)
        {
            string trace = Run(scenario).Replace("\r\n", "\n");
            string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(trace)));
            if (args.Length == 2 && args[0] == "--trace-directory")
            {
                Directory.CreateDirectory(args[1]);
                File.WriteAllText(Path.Combine(args[1], scenario + ".txt"), trace);
            }
            if (hash != baseline) failures.Add($"{scenario}: expected {baseline}, got {hash}");
        }
        if (failures.Count > 0)
            throw new Exception("Behavior trace regression (use --trace-directory <path> to inspect frames):\n" + string.Join('\n', failures));
        Console.WriteLine($"{expected.Count} pre-refactor behavior traces matched, including shared Random calls.");
    }

    private static string Run(string scenario)
    {
        var log = new StringBuilder();
        var random = new TracedRandom(731, log);
        var world = new PetWorld(random);
        var size = new Size(800, 650);
        var platforms = new[] { new RuinPlatform("ruin:floor", 0, 800, 650, 0),
            new RuinPlatform("upper", 260, 510, 400, 0), new RuinPlatform("side", 520, 780, 400, 2) };
        var kind = scenario == "jump" ? RuinLinkKind.Jump : RuinLinkKind.Climb;
        var links = new[] { new RuinLink("ruin:floor", "upper", 390, kind, 390),
            new RuinLink("upper", "ruin:floor", 390, kind, 390),
            new RuinLink("upper", "side", 480, RuinLinkKind.Bridge, 570),
            new RuinLink("side", "upper", 570, RuinLinkKind.Bridge, 480) };
        var bridges = new[] { new RuinBridge("bridge", "upper", "side", 480, 570, 400, 2) };
        var ruins = new RuinScene(size, platforms, links, bridges, Array.Empty<RuinDecoration>(), 731);
        var surfaces = platforms.Select(p => new HatSurface(p.Id, HatSurfaceKind.Ruin,
            new Rectangle((int)p.Left + 20, (int)p.Y + 30, (int)(p.Right - p.Left), 1))).ToArray();
        var env = new PetEnvironment(new(20, 30), 800, 650 - PetLogicalGeometry.Height, 800,
            new Rectangle(20, 30, 800, 650), false, Point.Empty, Array.Empty<Rectangle>(), surfaces, "ruin:floor");
        if (scenario == "failed-jump") env = env with { ObstaclesLocal = new[] { new Rectangle(0, 200, 800, 50) } };
        world.Start(0);
        world.Update(0, env);
        bool desktop = scenario is not ("launcher" or "failed-jump");
        if (desktop)
        {
            log.AppendLine("leave:" + world.LeaveLauncher(0));
            env = env with { Ruins = ruins, Scale = 0.5f, AwakeningSeconds = 10 };
        }
        var interrupted = new HashSet<PetMode>();
        bool droppedHat = false;
        bool interruptedRecovery = false;
        for (long now = 10; now <= 90000; now += 10)
        {
            if (!desktop)
            {
                env = env with { CanTrackCursor = now >= 30000 && now < 32000,
                    CursorScreenPosition = new(320, 480) };
                if (now == 45000) log.AppendLine("quake:" + world.TryStartEarthquake(now));
                if (now == 60000)
                {
                    log.AppendLine("hat:" + world.BeginHatDrag(new(200, 660)));
                    world.DropHat(false);
                }
            }
            if (desktop && scenario == "hat" && world.Location == PetLocation.Desktop && !droppedHat)
            {
                droppedHat = true;
                log.AppendLine("hat:" + world.BeginHatDrag(new(410, 400)));
                world.DropHat(false);
            }
            if (desktop && scenario == "interruptions" && world.Location == PetLocation.Desktop
                && world.Scene!.Mode is PetMode.Grabbing or PetMode.Climbing or PetMode.PullingUp or PetMode.Traversing or PetMode.Walking
                && interrupted.Add(world.Scene.Mode))
                log.AppendLine("interrupt:" + world.Scene.Mode + ":" + world.TryStartEarthquake(now));
            if (scenario is "drag" or "recovery-interrupt")
            {
                if (now is 5000 or 15000 or 25000)
                {
                    var bounds = world.Scene!.SpriteBounds;
                    Point press = new(bounds.Left + bounds.Width / 2 + 20, bounds.Top + bounds.Height / 2 + 30);
                    log.AppendLine("drag:" + world.BeginPetDrag(press, now));
                    world.MovePet(new(press.X + 70, press.Y - 400));
                    log.AppendLine("drag-quake:" + world.TryStartEarthquake(now));
                }
                if (now is 5400 or 15400 or 25400) world.DropPet(now);
                if (now == 26000) log.AppendLine("fall-quake:" + world.TryStartEarthquake(now));
            }
            if (scenario == "resize" && now == 16000)
                env = env with { Ruins = new RuinScene(size, platforms, links, bridges, Array.Empty<RuinDecoration>(), 732) };
            if (scenario == "stop-departure")
            {
                if (now is 200 or 900) world.Stop(now);
                if (now is 300 or 1100) world.Start(now);
            }
            if (scenario == "recovery-interrupt" && world.Scene!.Mode == PetMode.Recovering && !interruptedRecovery)
            {
                interruptedRecovery = true;
                log.AppendLine("recovery-quake:" + world.TryStartEarthquake(now));
            }
            if (scenario == "cursor-desktop")
                env = env with { CanTrackCursor = now % 5000 < 1300, CursorScreenPosition = new(440, 550) };
            if (scenario == "unrevealed") env = env with { AwakeningSeconds = now / 10000f };
            if (scenario == "hat-move")
            {
                if (now is 6000 or 9000 or 12500 or 30000)
                {
                    log.AppendLine("move-hat:" + world.BeginHatDrag(new(now == 6000 ? 410 : 680, now == 6000 ? 400 : 650)));
                    world.DropHat(false);
                }
                if (now == 32000) world.RestoreHat();
            }
            if (now == 70000) world.Stop(now);
            if (now == 71000) world.Start(now);
            if (scenario == "timing" && now % 170 != 0 && now % 230 != 0) continue;
            var scene = world.Update(now, env);
            // Explicit fields keep future additive presentation properties out of this legacy contract.
            log.AppendLine(JsonSerializer.Serialize(new { now, scene.Mode, scene.Row, scene.Frame, scene.SpriteBounds,
                scene.HeadScreenPosition, scene.WindowShake, scene.HatAttached, scene.Hat, scene.Speech,
                scene.VisibleLetters, scene.Scale, world.Location, world.HasLanded, world.IsPetDragging,
                Route = world.PlannedRoute, Start = world.PlannedRouteStart, Target = world.PlannedHatTarget }));
        }
        return log.ToString();
    }

    private sealed class TracedRandom : Random
    {
        private readonly StringBuilder log;
        internal TracedRandom(int seed, StringBuilder log) : base(seed) => this.log = log;
        public override int Next(int minValue, int maxValue)
        {
            int value = base.Next(minValue, maxValue);
            log.AppendLine($"random:{minValue}:{maxValue}:{value}");
            return value;
        }
        public override int Next(int maxValue)
        {
            int value = base.Next(maxValue);
            log.AppendLine($"random:{maxValue}:{value}");
            return value;
        }
        public override double NextDouble()
        {
            double value = base.NextDouble();
            log.AppendLine("random:" + value.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            return value;
        }
    }
}

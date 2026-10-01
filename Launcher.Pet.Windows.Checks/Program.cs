using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text.Json;
using Launcher.Pet;
using Launcher.Pet.Exploration;
using Launcher.Pet.Life;
using Launcher.Pet.Windows;
using Launcher.Pet.Windows.Desktop;
using Launcher.Pet.Windows.Drawing;
using Launcher.Pet.Windows.Life;
using Launcher.Pet.Windows.Windows;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        string root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "pet-life-checks-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        try
        {
            var geometry = new RuinBuilder(new(1000, 700), Array.Empty<DesktopSeed>(), 731).Build(new(1000, 700), new(500, 700));
            var saved = new PetLifeSave { Geometry = geometry, Life = LifeWorld.Create(geometry, 731).State, PetXFraction = .5f };
            CheckFiles(root, saved);
            CheckSession(root, saved);
            GraphicsChecks.Run();
            if (args.Length == 2 && args[0] == "--render") Render(Path.GetFullPath(args[1]), LifeGeometry.Reflow(geometry, geometry.Size, DesktopWorldMetrics.ForWorkArea(geometry.Size)));
            Console.WriteLine("Windows life checks passed: JSON round-trip, safe corruption recovery, locked files, persistent ruins and wallpaper lifecycle.");
        }
        finally
        {
            string temporaryRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!root.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(root).StartsWith("pet-life-checks-", StringComparison.Ordinal))
                throw new InvalidOperationException("Unsafe test cleanup path.");
            Directory.Delete(root, true);
        }
    }
    private static void Require(bool test, string message) { if (!test) throw new Exception(message); }
    private static void CheckFiles(string root, PetLifeSave saved)
    {
        string path = Path.Combine(root, "life.json");
        var file = new LifeSaveFile(path);
        Require(file.Read(out var warning) is null && warning is null, "Absent save was not treated as a new world.");
        Require(file.Write(saved) is null, "Could not save life.");
        var loaded = file.Read(out warning);
        Require(loaded is not null && warning is null && JsonSerializer.Serialize(saved) == JsonSerializer.Serialize(loaded),
            "Geometry, ecology or RNG did not survive JSON round-trip.");
        File.WriteAllText(path, "{broken");
        Require(file.Read(out warning) is null && warning is not null && Directory.GetFiles(root, "life.json.broken-*").Length == 1,
            "Corrupt save was not backed up.");
        Require(file.Write(saved) is null, "Recovery after a successful backup was blocked.");
        string original = File.ReadAllText(path);
        using (var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            var blocked = new LifeSaveFile(path);
            Require(blocked.Read(out warning) is null && warning is not null && blocked.Write(saved) is not null,
                "Unreadable save must block writes.");
        }
        Require(File.ReadAllText(path) == original && Directory.GetFiles(root, "*.tmp-*").Length == 0, "Saving damaged the original or left temporary files.");
        // Malformed elements must be classified as corrupt data, not crash the host.
        File.WriteAllText(path, original.Replace("\"Platforms\":[", "\"Platforms\":[null,"));
        Require(new LifeSaveFile(path).Read(out warning) is null && warning is not null, "Null geometry was accepted.");
    }
    private static void CheckSession(string root, PetLifeSave saved)
    {
        string path = Path.Combine(root, "session.json");
        Require(new LifeSaveFile(path).Write(saved) is null, "Session fixture failed.");
        using var form = new Form { ClientSize = new(800, 650) };
        using var area = new PetArea { Size = new(800, 650) };
        form.Controls.Add(area);
        var wallpaper = new FakeWallpaper();
        var world = new PetWorld(new Random(3));
        using var session = new PetWindowsSession(form, area, world, wallpaper, lifeFilePath: path);
        var problems = new List<string>(); session.Problem += problems.Add;
        session.Start();
        Require(world.HasLife && world.Location == Launcher.Pet.Data.PetLocation.Desktop && wallpaper.Activated == 1,
            "Restoring a world must skip departure and activate its wallpaper.");
        form.WindowState = FormWindowState.Minimized;
        form.ClientSize = new(780, 620);
        form.WindowState = FormWindowState.Normal;
        form.ClientSize = new(800, 650);
        form.WindowState = FormWindowState.Minimized;
        form.ClientSize = new(790, 640);
        Require(wallpaper.Restored == 0 && wallpaper.Activated == 1 && world.HasLife,
            "Restoring/minimizing Launcher destroyed ruins or restarted their wallpaper.");
        session.Stop();
        var checkpoint = new LifeSaveFile(path).Read(out var warning);
        Require(wallpaper.Restored == 1 && problems.Count == 0 && checkpoint is not null,
            $"Stopping must save life and restore wallpaper. Restores={wallpaper.Restored}; problems={string.Join(" | ", problems)}; read={warning}");
    }
    private static void Render(string output, RuinScene geometry)
    {
        Directory.CreateDirectory(output);
        var life = LifeWorld.Create(geometry, 731);
        using var renderer = new RuinRenderer();
        using var wallpaper = new Bitmap(Path.Combine(AppContext.BaseDirectory, "Resources", "ruins", "wallpaper.png"));
        int[] times = { 0, 120, 300, 420 };
        using var storyboard = new Bitmap(1000, 700 * times.Length, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(storyboard);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        int prior = 0;
        for (int frame = 0; frame < times.Length; frame++)
        {
            life.Advance(times[frame] - prior); prior = times[frame];
            var snapshot = life.Snapshot(geometry, new(500, 620));
            using var image = new Bitmap(1000, 700);
            using (var g = Graphics.FromImage(image))
            {
                g.DrawImage(wallpaper, new Rectangle(0, 0, 1000, 700));
                g.DrawImageUnscaled(renderer.Render(geometry, new(500, 700), 4 + times[frame], snapshot), 0, 0);
                foreach (var item in snapshot.Items) LifeDrawing.DrawItem(g, item.Kind,
                    new(item.Position.X, item.Position.Y - geometry.WorldMetrics.ItemDiameter * (item.Kind == LifeItemKind.Spore ? .8f : .22f)), item.Mass, times[frame], geometry.WorldMetrics.ItemDiameter);
            }
            image.Save(Path.Combine(output, $"life-{times[frame]:000}.png"));
            graphics.DrawImageUnscaled(image, 0, frame * 700);
        }
        storyboard.Save(Path.Combine(output, "life-storyboard.png"));
        RenderActivities(output, geometry, wallpaper);
        RenderSequences(output, geometry, wallpaper);
        Console.WriteLine("Visual ecology frames: " + output);
    }
    private static void RenderActivities(string output, RuinScene geometry, Bitmap wallpaper)
    {
        using var images = new PetImages();
        using var renderer = new RuinRenderer();
        var world = new PetWorld(new Random(731)); world.Start(0);
        world.SetAppearance(images.Appearance, 0);
        world.RestoreLife(new() { Geometry = geometry, Life = LifeWorld.Create(geometry, 731).State, PetXFraction = .5f }, 0);
        var env = new Launcher.Pet.Data.PetEnvironment(Point.Empty, geometry.Size.Width, geometry.Size.Height - PetLogicalGeometry.Height,
            geometry.Size.Width, new(Point.Empty, geometry.Size), false, Point.Empty, Array.Empty<Rectangle>(), Array.Empty<Launcher.Pet.Hat.HatSurface>(), "ruin:floor", geometry.WorldMetrics.RenderScale, geometry, 10);
        var captured = new HashSet<LifeTaskKind>();
        using var sheet = new Bitmap(720, 480);
        using var sheetGraphics = Graphics.FromImage(sheet);
        for (long ms = 0; ms < 3600000 && captured.Count < 4; ms += 20)
        {
            var scene = world.Update(ms, env);
            var life = scene.Life!;
            if (life.Activity is not { } activity || captured.Contains(activity)) continue;
            bool held = life.Items.Any(i => i.Holder == LifeHolder.Pet);
            if (activity == LifeTaskKind.Carry ? !held || scene.Appearance.Clip(scene.Row)?.Name is not ("carry-left" or "carry-right")
                : life.ActionSeconds is < 1.8f or > 1.9f || activity != LifeTaskKind.Observe && !held) continue;
            using var image = new Bitmap(geometry.Size.Width, geometry.Size.Height);
            using (var g = Graphics.FromImage(image))
            {
                g.DrawImage(wallpaper, new Rectangle(Point.Empty, image.Size));
                g.DrawImageUnscaled(renderer.Render(geometry, new(500, 700), 4 + ms / 1000f, life), 0, 0);
                var sprite = images.Frame(scene);
                g.DrawImage(sprite.Image, scene.SpriteBounds, sprite.Source, GraphicsUnit.Pixel);
                foreach (var item in life.Items)
                {
                    float lift = item.Holder == LifeHolder.World ? geometry.WorldMetrics.ItemDiameter * (item.Kind == LifeItemKind.Spore ? .8f : .22f) : 0;
                    LifeDrawing.DrawItem(g, item.Kind, new(item.Position.X, item.Position.Y - lift), item.Mass, ms / 1000f, geometry.WorldMetrics.ItemDiameter);
                }
            }
            int x = Math.Clamp(scene.SpriteBounds.Left + scene.SpriteBounds.Width / 2 - 180, 0, image.Width - 360);
            int y = Math.Clamp(scene.SpriteBounds.Bottom - 190, 0, image.Height - 240);
            using var crop = image.Clone(new Rectangle(x, y, 360, 240), PixelFormat.Format32bppArgb);
            crop.Save(Path.Combine(output, "activity-" + activity.ToString().ToLowerInvariant() + ".png"));
            sheetGraphics.DrawImageUnscaled(crop, (int)activity % 2 * 360, (int)activity / 2 * 240);
            captured.Add(activity);
            Console.WriteLine($"  Captured {activity} at {ms / 1000}s.");
        }
        Require(captured.Count == 4, "Autonomous renderer did not reach all life activities.");
        sheet.Save(Path.Combine(output, "activities.png"));
    }
    private static void RenderSequences(string output, RuinScene geometry, Bitmap wallpaper)
    {
        using var images = new PetImages(); using var renderer = new RuinRenderer();
        var world = new PetWorld(new Random(731)); world.Start(0); world.SetAppearance(images.Appearance, 0);
        world.RestoreLife(new() { Geometry = geometry, Life = LifeWorld.Create(geometry, 731).State, PetXFraction = .5f }, 0);
        var env = new Launcher.Pet.Data.PetEnvironment(Point.Empty, geometry.Size.Width, geometry.Size.Height - 200, geometry.Size.Width,
            new(Point.Empty, geometry.Size), false, Point.Empty, Array.Empty<Rectangle>(), Array.Empty<Launcher.Pet.Hat.HatSurface>(),
            "ruin:floor", geometry.WorldMetrics.RenderScale, geometry, 10);
        var captured = new HashSet<LifeTaskKind>(); LifeTaskKind? active = null; long begin = 0; int index = 0;
        var history = new List<object>();
        for (long ms = 0; ms < 3600000 && captured.Count < 4; ms += 20)
        {
            var scene = world.Update(ms, env); var life = scene.Life!;
            if (active is null && life.Activity is { } task && !captured.Contains(task))
            {
                string? clip = scene.Appearance.Clip(scene.Row)?.Name;
                bool useful = task == LifeTaskKind.Carry ? clip is "carry-left" or "carry-right"
                    : life.ActionSeconds is > 0 and < .1f && clip == (task == LifeTaskKind.Observe ? "observe" : task == LifeTaskKind.Plant ? "plant" : "recycle");
                if (useful) { active = task; begin = ms; index = 0; history.Clear(); Directory.CreateDirectory(Path.Combine(output, "motion-" + task.ToString().ToLowerInvariant())); }
            }
            if (active is not { } kind || (ms - begin) % 100 != 0) continue;
            using var image = new Bitmap(geometry.Size.Width, geometry.Size.Height);
            using (var g = Graphics.FromImage(image))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(wallpaper, new Rectangle(Point.Empty, image.Size));
                g.DrawImageUnscaled(renderer.Render(geometry, new(500, 700), 4 + ms / 1000f, life), 0, 0);
                var frame = images.Frame(scene); g.DrawImage(frame.Image, scene.SpriteBounds, frame.Source, GraphicsUnit.Pixel);
                foreach (var item in life.Items)
                {
                    float lift = item.Holder == LifeHolder.World ? geometry.WorldMetrics.ItemDiameter * (item.Kind == LifeItemKind.Spore ? .8f : .22f) : 0;
                    LifeDrawing.DrawItem(g, item.Kind, new(item.Position.X, item.Position.Y - lift), item.Mass, ms / 1000f, geometry.WorldMetrics.ItemDiameter,
                        item.Holder == LifeHolder.Pet && life.Activity == LifeTaskKind.Recycle ? life.ActionSeconds / 2 : 0);
                }
            }
            int x = Math.Clamp(scene.SpriteBounds.Left + scene.SpriteBounds.Width / 2 - 180, 0, image.Width - 360);
            int y = Math.Clamp(scene.SpriteBounds.Bottom - 190, 0, image.Height - 240);
            using var crop = image.Clone(new Rectangle(x, y, 360, 240), PixelFormat.Format32bppArgb);
            string folder = Path.Combine(output, "motion-" + kind.ToString().ToLowerInvariant());
            crop.Save(Path.Combine(folder, $"frame-{index++:000}.png"));
            history.Add(new { ms, scene.Row, scene.Frame, life.Activity, life.ActionSeconds, held = life.Items.Count(i => i.Holder == LifeHolder.Pet) });
            if (ms - begin >= 2600) { File.WriteAllText(Path.Combine(folder, "history.json"), JsonSerializer.Serialize(history)); captured.Add(kind); active = null; }
        }
        Require(captured.Count == 4, "Runtime motion capture missed a life action.");
    }
    private sealed class FakeWallpaper : IDesktopWallpaperSession
    {
        internal int Activated, Restored;
        public string? Activate() { Activated++; return null; }
        public string? Restore() { Restored++; return null; }
    }
}

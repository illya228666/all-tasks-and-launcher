using System.Drawing;
using Launcher.Pet.Activities;
using Launcher.Pet.Data;

namespace Launcher.Pet.Exploration;

// Owns navigation knowledge, never the active behavior or animation clock.
internal sealed class PetNavigation
{
    private readonly Random _random;
    internal PetNavigation(Random random) => _random = random;
    private readonly Dictionary<string, int> _visits = new();
    private RuinScene? _scene;
    internal string Support { get; private set; } = "ruin:floor";
    internal RuinLink? Link { get; set; }
    internal string? Goal { get; private set; }
    internal bool SeekingHat { get; private set; }
    internal float FootY { get; set; }
    internal long NextTrip { get; set; }
    internal bool Initialized => _scene is not null;
    internal Dictionary<string, RuinPlatform> Available { get; private set; } = new();

    internal void CancelRoute()
    {
        Link = null;
        Goal = null;
        SeekingHat = false;
    }

    internal void Prepare(PetActivityContext c)
    {
        RuinScene scene = c.Environment.Ruins!;
        Available = scene.Platforms.Where(p => p.Id == "ruin:floor" || c.Environment.AwakeningSeconds >= p.RevealAt + 0.85f).ToDictionary(p => p.Id);
        if (ReferenceEquals(_scene, scene)) return;
        bool changed = Initialized;
        _scene = scene;
        Support = "ruin:floor";
        FootY = changed ? Math.Clamp(FootY, 0, scene.Size.Height) : scene.Size.Height;
        CancelRoute();
        c.Body.X = Math.Clamp(c.Body.X, 0, Math.Max(0, scene.Size.Width - c.HalfWidth * 2));
        c.Actor.Reset(c.Now);
        if (changed && FootY < scene.Size.Height - 1) c.Actor.ContinueWith(new FlightActivity(), c.Now);
        NextTrip = c.Now + 1200;
    }

    internal RuinPlatform? HatPlatform(PetActivityContext c)
    {
        Point? point = c.Actor.Hat.RestingPoint(c.Environment.Surfaces);
        if (point is null) return null;
        float x = point.Value.X - c.Environment.AreaScreenPosition.X;
        float y = point.Value.Y - c.Environment.AreaScreenPosition.Y;
        var platform = Available.Values.FirstOrDefault(p => Math.Abs(p.Y - y) <= 3 && x >= p.Left + c.Metrics.HalfBody && x <= p.Right - c.Metrics.HalfBody);
        if (platform is not null) return platform;
        var bridge = c.Environment.Ruins!.Bridges.FirstOrDefault(b => c.Environment.AwakeningSeconds >= b.RevealAt + 0.85f
            && Math.Abs(b.Y - y) <= 3 && x >= b.Left && x <= b.Right);
        return bridge is null ? null : Available[x < (bridge.Left + bridge.Right) / 2 ? bridge.From : bridge.To];
    }

    // Ground-level goals may preempt a routine or an approach. Airborne, held and
    // recovery activities bypass this policy and own their tick entirely.
    internal bool UpdateGround(PetActivityContext c)
    {
        var actor = c.Actor;
        RuinPlatform support = Available[Support];
        FootY = support.Y;
        RuinPlatform? hatPlatform = HatPlatform(c);
        if (c.Environment.CanTrackCursor || actor.Activity.SuppressNavigation)
        {
            CancelRoute();
            if (actor.Activity is ApproachActivity or SeekRuinHatActivity or WalkActivity)
                actor.Reset(c.Now);
            c.Body.JumpLift = 0;
            actor.Routine.Update(c with { Environment = c.Environment with { PetZoneTopY = (int)FootY - PetLogicalGeometry.Height } });
            return true;
        }
        if (hatPlatform?.Id == Support)
        {
            Goal = hatPlatform.Id;
            SeekingHat = true;
            Link = null;
            if (actor.Activity is not SeekRuinHatActivity) actor.ContinueWith(new SeekRuinHatActivity(), c.Now);
            actor.Activity.Update(c);
            return true;
        }
        bool reconsider = false;
        if (hatPlatform is not null && (!SeekingHat || Goal != hatPlatform.Id))
        {
            Goal = hatPlatform.Id;
            SeekingHat = true;
            Link = null;
            reconsider = true;
        }
        else if (hatPlatform is null && SeekingHat)
        {
            CancelRoute();
            reconsider = true;
        }
        if (hatPlatform is null && actor.Life is not null)
        {
            if (actor.Life.UpdateGround(c)) return true;
            if (actor.Activity is ApproachActivity or LifeActivity) actor.Reset(c.Now);
            c.Body.JumpLift = 0;
            return false;
        }
        if ((reconsider || actor.Activity is not ApproachActivity) && (Goal is not null || c.Now >= NextTrip))
        {
            Goal ??= Available.Values.Where(p => p.Id != Support).OrderBy(p => _visits.GetValueOrDefault(p.Id) + _random.NextDouble()).Select(p => p.Id).FirstOrDefault();
            Link = Goal is null ? null : FindRoute(c.Environment.Ruins!, Available, Support, Goal, c.Environment.AwakeningSeconds).FirstOrDefault();
            if (Link is not null)
            {
                actor.ContinueWith(new ApproachActivity(Link), c.Now);
                reconsider = false;
                actor.Speech.Reset(c.Now);
            }
            else { Goal = null; NextTrip = c.Now + 1500; }
        }
        if (!reconsider && actor.Activity is ApproachActivity)
        {
            actor.Activity.Update(c);
            return true;
        }
        if (actor.Activity is ApproachActivity or SeekRuinHatActivity or WalkActivity) actor.Reset(c.Now);
        c.Body.JumpLift = 0;
        return false;
    }

    internal void Land(PetActivityContext c, RuinPlatform platform)
    {
        Support = platform.Id;
        FootY = platform.Y;
        Link = null;
        _visits[Support] = _visits.GetValueOrDefault(Support) + 1;
        if (Goal == Support) Goal = null;
        c.Actor.Reset(c.Now);
        NextTrip = c.Now + _random.Next(3500, 8000);
    }

    internal bool CanReach(PetActivityContext c, string platform) => Available.ContainsKey(platform)
        && (platform == Support || FindRoute(c.Environment.Ruins!, Available, Support, platform, c.Environment.AwakeningSeconds).Length > 0);

    internal void RestoreSupport(string platform, RuinScene scene)
    {
        Support = Available.ContainsKey(platform) ? platform : "ruin:floor";
        FootY = Available[Support].Y;
        CancelRoute();
    }

    internal bool ApproachGoal(PetActivityContext c, string platform)
    {
        Goal = platform;
        SeekingHat = false;
        var route = FindRoute(c.Environment.Ruins!, Available, Support, platform, c.Environment.AwakeningSeconds);
        if (route.Length == 0) return false;
        if (Link != route[0] || c.Actor.Activity is not ApproachActivity)
        {
            Link = route[0];
            c.Actor.ContinueWith(new ApproachActivity(Link), c.Now);
        }
        c.Actor.Activity.Update(c);
        return true;
    }

    internal IReadOnlyList<RuinLink> PlannedRoute(RuinScene scene, float seconds, PetActivity activity)
    {
        if (Goal is null || !ReferenceEquals(_scene, scene) || !activity.ShowsRoute) return Array.Empty<RuinLink>();
        var available = scene.Platforms.Where(p => p.Id == "ruin:floor" || seconds >= p.RevealAt + 0.85f).ToDictionary(p => p.Id);
        if (Link is not null && Link.From == Support && available.ContainsKey(Link.To))
            return new[] { Link }.Concat(FindRoute(scene, available, Link.To, Goal, seconds)).ToArray();
        return FindRoute(scene, available, Support, Goal, seconds);
    }
    private static RuinLink[] FindRoute(RuinScene scene, Dictionary<string, RuinPlatform> available, string start, string goal, float seconds)
    {
        if (start == goal || !available.ContainsKey(start) || !available.ContainsKey(goal)) return Array.Empty<RuinLink>();
        var queue = new Queue<string>();
        var parents = new Dictionary<string, RuinLink>();
        queue.Enqueue(start);
        var visited = new HashSet<string> { start };
        while (queue.TryDequeue(out string? from))
            foreach (var edge in scene.Links.Where(e => e.From == from && available.ContainsKey(e.To)
                && (e.Kind != RuinLinkKind.Bridge || scene.Bridges.Any(b =>
                    ((b.From == e.From && b.To == e.To) || (b.From == e.To && b.To == e.From))
                    && seconds >= b.RevealAt + 0.85f))))
            {
                if (!visited.Add(edge.To)) continue;
                parents[edge.To] = edge;
                if (edge.To == goal)
                {
                    var route = new List<RuinLink>();
                    for (string current = goal; current != start; current = parents[current].From)
                        route.Add(parents[current]);
                    route.Reverse();
                    return route.ToArray();
                }
                queue.Enqueue(edge.To);
            }
        return Array.Empty<RuinLink>();
    }
}

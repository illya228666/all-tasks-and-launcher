using System.Drawing;
using Launcher.Pet.Exploration;

namespace Launcher.Pet.Life;

public sealed class LifeWorld
{
    public LifeState State { get; }
    public LifeWorld(LifeState state) { Validate(state); State = state; }

    public static LifeWorld Create(RuinScene scene, ulong seed)
    {
        var state = new LifeState { EcologyRandom = seed == 0 ? 1UL : seed, BehaviorRandom = seed ^ 0xd1b54a32d192ed03UL };
        if (state.BehaviorRandom == 0) state.BehaviorRandom = 2;
        var world = new LifeWorld(state);
        var reachable = Reachable(scene, "ruin:floor");
        var platforms = scene.Platforms.Where(p => reachable.Contains(p.Id) && p.Right - p.Left >= 48)
            .OrderBy(p => p.Id == "ruin:floor" ? "" : p.Id, StringComparer.Ordinal).ToArray();
        if (platforms.Length == 0) throw new InvalidDataException("Life needs a reachable floor.");
        for (int i = 0; i < 12; i++)
        {
            var platform = platforms[i % platforms.Length];
            int count = (12 + platforms.Length - 1 - i % platforms.Length) / platforms.Length;
            int index = i / platforms.Length;
            state.Sites.Add(new() { Id = $"life:{i}", Platform = platform.Id,
                Fraction = (index + 1f) / (count + 1), Nutrients = world.Random(6000, 12000) });
        }
        foreach (var site in state.Sites.Where((_, i) => i is 0 or 4 or 8)) world.Seed(site, 2000);
        state.TotalMatter = world.Matter;
        return world;
    }

    public long Matter => State.Reserve + State.Sites.Sum(s => (long)s.Nutrients + (s.Colony?.Mass ?? 0)) + State.Items.Sum(i => (long)i.Mass);
    private int Random(int min, int max)
    {
        ulong stream = State.EcologyRandom;
        int value = LifeRandom.Range(ref stream, min, max);
        State.EcologyRandom = stream;
        return value;
    }
    internal double Choice()
    {
        ulong stream = State.BehaviorRandom;
        ulong value = LifeRandom.Next(ref stream);
        State.BehaviorRandom = stream;
        return (value >> 11) * (1.0 / (1UL << 53));
    }

    public void Advance(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds <= 0) return;
        State.Remainder += seconds;
        while (State.Remainder >= 1 - 1e-9)
        {
            State.Remainder = Math.Max(0, State.Remainder - 1);
            Tick();
        }
    }

    private void Tick()
    {
        State.Seconds++;
        var p = State.Profile;
        foreach (var site in State.Sites)
        {
            var colony = site.Colony;
            if (colony is null) continue;
            colony.Age++;
            if (colony.Age == colony.Germination)
            {
                colony.Stage = MossStage.Growing;
                Event(LifeEventKind.Established, site.Id, colony.Transfer);
            }
            if (colony.Age >= colony.Germination)
            {
                int growth = Math.Min(site.Nutrients, Math.Min(p.GrowthPerSecond, Math.Max(0, p.MaxBiomass - colony.Mass)));
                site.Nutrients -= growth;
                colony.Mass += growth;
                // Shed biomass becomes visible litter before returning to soil.
                int spent = Math.Min(p.MaintenancePerSecond, colony.Mass);
                colony.Mass -= spent;
                var litter = State.Items.FirstOrDefault(i => i.Site == site.Id && i.Kind == LifeItemKind.Remains && i.Death == 0 && i.Holder == LifeHolder.World);
                if (litter is null && spent > 0)
                {
                    litter = new() { Id = State.NextId++, Site = site.Id, Kind = LifeItemKind.Remains, Duration = p.DecompositionMax };
                    State.Items.Add(litter);
                }
                if (litter is not null) { litter.Mass += spent; litter.OriginalMass += spent; }
                colony.Stage = site.Nutrients < p.GrowthPerSecond ? MossStage.Exhausted
                    : colony.Age >= colony.Maturity ? MossStage.Mature : MossStage.Growing;
                if (colony.Age >= colony.Maturity && colony.Age >= colony.NextRelease
                    && colony.Mass >= 1500 && State.Items.Count < p.MaxItems - 24)
                {
                    colony.Mass -= 500;
                    var target = DispersalTarget(site);
                    State.Items.Add(new() { Id = State.NextId++, Site = target.Id, Kind = LifeItemKind.Spore,
                        Mass = 500, OriginalMass = 500, Duration = Random(p.GerminationMin, p.GerminationMax) });
                    Event(LifeEventKind.Released, site.Id, 0);
                    colony.NextRelease = colony.Age + Random(p.ReleaseMin, p.ReleaseMax);
                }
            }
            if (colony.Age >= colony.Lifespan || colony.Mass <= 250)
            {
                long death = State.NextId++;
                // A reserve slot is kept for each colony's remains.
                if (colony.Mass > 0) State.Items.Add(new() { Id = State.NextId++, Site = site.Id, Kind = LifeItemKind.Remains,
                    Mass = colony.Mass, OriginalMass = colony.Mass, Duration = Random(p.DecompositionMin, p.DecompositionMax), Death = death });
                if (colony.Age < colony.Germination) Event(LifeEventKind.Failed, site.Id, colony.Transfer);
                site.Colony = null;
                Event(LifeEventKind.Died, site.Id, death);
            }
        }
        foreach (var item in State.Items.ToArray())
        {
            item.Age++;
            if (item.Holder != LifeHolder.World) continue;
            var site = Site(item.Site)!;
            if (item.Kind == LifeItemKind.Spore)
            {
                if (item.Age < item.Duration) continue;
                if (site.Colony is null && site.Nutrients >= 1500)
                {
                    int nutrients = Math.Min(1500, site.Nutrients);
                    site.Nutrients -= nutrients;
                    site.Colony = NewColony(item.Mass + nutrients, item.Transfer);
                    State.Items.Remove(item);
                }
                else
                {
                    Event(LifeEventKind.Failed, site.Id, item.Transfer);
                    item.Kind = LifeItemKind.Remains;
                    item.Age = 0;
                    item.Duration = Random(p.DecompositionMin, p.DecompositionMax);
                }
            }
            else
            {
                int amount = Math.Min(item.Mass, Math.Max(1, (item.OriginalMass + item.Duration - 1) / item.Duration) * (item.Accelerated ? 3 : 1));
                item.Mass -= amount;
                int banking = Math.Min(amount / 10, Math.Max(0, 1000 - State.Reserve));
                State.Reserve += banking;
                site.Nutrients += amount - banking;
                if (item.Mass == 0)
                {
                    if (item.Death != 0) Event(LifeEventKind.Decomposed, site.Id, item.Death);
                    State.Items.Remove(item);
                }
            }
        }
        bool extinct = State.Sites.All(s => s.Colony is null) && State.Items.All(i => i.Kind != LifeItemKind.Spore);
        State.QuietSeconds = extinct ? State.QuietSeconds + 1 : 0;
        if (State.QuietSeconds >= p.RenewalDelay && State.Reserve >= 250)
        {
            var candidates = State.Sites.Where(s => s.Colony is null && s.Nutrients >= 1750).ToArray();
            if (candidates.Length > 0)
            {
                var site = candidates[Random(0, candidates.Length - 1)];
                State.Reserve -= 250;
                site.Nutrients -= 1750;
                site.Colony = NewColony(2000, 0);
                State.Generation++;
                State.QuietSeconds = 0;
            }
        }
        State.Events.RemoveAll(e => State.Seconds - e.AtSecond > 900 && e.Id != State.Intent?.Event);
        if (State.Events.Count > 192) State.Events.RemoveRange(0, State.Events.Count - 192);
    }

    private LifeSite DispersalTarget(LifeSite origin)
    {
        // Most dispersal is local. The remainder lets new areas become inhabited.
        var local = State.Sites.Where(s => s.Platform == origin.Platform).OrderBy(s => Math.Abs(s.Fraction - origin.Fraction)).Take(3).ToArray();
        return Random(0, 9) < 8 ? local[Random(0, local.Length - 1)] : State.Sites[Random(0, State.Sites.Count - 1)];
    }
    private void Seed(LifeSite site, int amount)
    {
        site.Nutrients -= amount;
        site.Colony = NewColony(amount, 0);
    }
    private MossColony NewColony(int mass, long transfer)
    {
        var p = State.Profile;
        int maturity = Random(p.MaturityMin, p.MaturityMax);
        return new() { Id = State.NextId++, Mass = mass, Germination = Random(p.GerminationMin, p.GerminationMax),
            Maturity = maturity, Lifespan = Random(p.LifespanMin, p.LifespanMax),
            NextRelease = maturity + Random(p.ReleaseMin, p.ReleaseMax), Transfer = transfer };
    }
    private void Event(LifeEventKind kind, string site, long related) => State.Events.Add(new()
        { Id = State.NextId++, Kind = kind, Site = site, Related = related, AtSecond = State.Seconds });

    public LifeSite? Site(string id) => State.Sites.FirstOrDefault(s => s.Id == id);
    public LifeItem? Item(long id) => State.Items.FirstOrDefault(i => i.Id == id);
    public bool Observe(long id)
    {
        var e = State.Events.FirstOrDefault(e => e.Id == id);
        if (e is null || e.Observed) return false;
        e.Observed = true;
        var learning = State.Learning;
        switch (e.Kind)
        {
            case LifeEventKind.Released: learning.Releases = Math.Min(2, learning.Releases + 1); break;
            case LifeEventKind.Established:
            case LifeEventKind.Failed:
                if (e.Related != 0) learning.TransferOutcomes = Math.Min(3, learning.TransferOutcomes + 1);
                break;
            case LifeEventKind.Died:
                if (learning.Deaths.Count < 2 && !learning.Deaths.Contains(e.Related)) learning.Deaths.Add(e.Related);
                break;
            case LifeEventKind.Decomposed:
                if (learning.Deaths.Contains(e.Related) && !learning.Decompositions.Contains(e.Related)) learning.Decompositions.Add(e.Related);
                break;
        }
        return true;
    }

    public bool PickUp(long id, LifeHolder holder)
    {
        var item = Item(id);
        if (holder == LifeHolder.World || item is null || item.Holder != LifeHolder.World
            || State.Items.Any(i => i.Holder == holder)) return false;
        item.Holder = holder;
        return true;
    }
    public bool Place(long id, string siteId, bool deliberate = false, bool recycle = false, bool completedByPet = true)
    {
        var item = Item(id);
        var site = Site(siteId);
        if (item is null || site is null) return false;
        bool pet = item.Holder == LifeHolder.Pet;
        item.Site = site.Id;
        item.Holder = LifeHolder.World;
        if (pet && completedByPet && item.Kind == LifeItemKind.Spore)
        {
            item.Transfer = State.NextId++;
            item.Age = 0;
            if (deliberate) item.Duration = Math.Min(item.Duration, 30);
        }
        if (pet && completedByPet && recycle && item.Kind == LifeItemKind.Remains) item.Accelerated = true;
        return true;
    }

    public static PointF Position(LifeSite site, RuinScene scene)
    {
        var platform = scene.Platforms.FirstOrDefault(p => p.Id == site.Platform) ?? scene.Platforms.First(p => p.Id == "ruin:floor");
        float margin = Math.Min(scene.Metrics is null ? 28 : scene.WorldMetrics.HalfBody + scene.WorldMetrics.CharacterHeight * .22f, (platform.Right - platform.Left) / 2);
        return new(platform.Left + margin + (platform.Right - platform.Left - margin * 2) * site.Fraction, platform.Y);
    }
    public LifeScene Snapshot(RuinScene scene, PointF carried, bool authoredHand = false)
    {
        var sites = State.Sites.Select(s => new LifeSiteView(s.Id, Position(s, scene), s.Nutrients, s.Colony?.Mass ?? 0, s.Colony?.Stage)).ToArray();
        var intent = State.Intent;
        string? focus = intent is null ? null : intent.PickedUp ? intent.Destination : intent.Site;
        float progress = intent?.ActionSeconds ?? 0;
        if (!authoredHand && intent?.PickedUp == true && progress > 0 && Site(intent.Destination) is { } destination)
        {
            var target = Position(destination, scene);
            float t = Math.Clamp(progress / 2, 0, 1);
            carried = new(carried.X + (target.X - carried.X) * t, carried.Y + (target.Y - 4 - carried.Y) * t);
        }
        var items = State.Items.Select(i => new LifeItemView(i.Id, i.Kind,
            i.Holder == LifeHolder.Pet ? carried : i.Holder == LifeHolder.User ? new(i.X, i.Y) : Position(Site(i.Site)!, scene), i.Holder, i.Mass)).ToArray();
        var signals = State.Events.Where(e => State.Seconds - e.AtSecond < 5 && e.Kind is LifeEventKind.Released or LifeEventKind.Decomposed)
            .Select(e => new LifeSignalView(e.Kind, Position(Site(e.Site)!, scene), State.Seconds - e.AtSecond)).ToArray();
        return new(sites, items, State.Seconds, State.Generation, intent?.Kind, focus, progress, signals, State.Remainder);
    }
    public void Reanchor(RuinScene scene)
    {
        var reachable = Reachable(scene, "ruin:floor");
        foreach (var site in State.Sites)
            if (!reachable.Contains(site.Platform)) site.Platform = "ruin:floor";
    }
    public static HashSet<string> Reachable(RuinScene scene, string start)
    {
        var found = new HashSet<string>(StringComparer.Ordinal) { start };
        var queue = new Queue<string>(); queue.Enqueue(start);
        while (queue.TryDequeue(out var from))
            foreach (var link in scene.Links.Where(l => l.From == from))
                if (found.Add(link.To)) queue.Enqueue(link.To);
        return found;
    }

    public static void Validate(LifeState s)
    {
        static void Require([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool condition) { if (!condition) throw new InvalidDataException("Invalid saved desktop life."); }
        Require(s is not null && s.Version == 1 && s.Profile is not null && s.Learning is not null
            && s.Sites is not null && s.Items is not null && s.Events is not null && s.RecentSites is not null);
        var p = s.Profile;
        Require(p.GerminationMin > 0 && p.GerminationMax >= p.GerminationMin && p.GerminationMax <= 3600
            && p.MaturityMin >= p.GerminationMax && p.MaturityMax >= p.MaturityMin && p.MaturityMax <= 3600
            && p.ReleaseMin > 0 && p.ReleaseMax >= p.ReleaseMin && p.ReleaseMax <= 3600
            && p.LifespanMin > p.MaturityMax && p.LifespanMax >= p.LifespanMin && p.LifespanMax <= 86400
            && p.DecompositionMin > 0 && p.DecompositionMax >= p.DecompositionMin && p.DecompositionMax <= 3600
            && p.RenewalDelay > 0 && p.RenewalDelay <= 3600 && p.GrowthPerSecond is > 0 and <= 1000
            && p.MaintenancePerSecond is > 0 and <= 1000 && p.MaxBiomass is >= 2000 and <= 100000 && p.MaxItems is >= 48 and <= 192);
        Require(s.Sites.Count is 0 or 12 && s.Items.Count <= p.MaxItems + 12 && s.Events.Count <= 192
            && s.Reserve is >= 0 and <= 1000 && s.QuietSeconds >= 0 && s.Generation > 0 && s.Seconds >= 0
            && double.IsFinite(s.Remainder) && s.Remainder >= 0 && s.Remainder < 1
            && s.NextId > 0 && s.EcologyRandom != 0 && s.BehaviorRandom != 0);
        Require(s.Sites.All(site => site is not null && !string.IsNullOrEmpty(site.Id) && !string.IsNullOrEmpty(site.Platform)
            && float.IsFinite(site.Fraction) && site.Fraction is >= 0 and <= 1 && site.Nutrients is >= 0 and <= 1000000
            && (site.Colony is null || site.Colony.Mass is > 0 and <= 100000 && site.Colony.Id > 0
                && site.Colony.Age >= 0 && site.Colony.Germination > 0 && site.Colony.Maturity >= site.Colony.Germination
                && site.Colony.Lifespan > site.Colony.Maturity && Enum.IsDefined(typeof(MossStage), site.Colony.Stage))));
        Require(s.Sites.Select(site => site.Id).Distinct().Count() == s.Sites.Count);
        var ids = s.Sites.Select(site => site.Id).ToHashSet();
        Require(s.Items.All(i => i is not null && i.Id > 0 && ids.Contains(i.Site) && i.Mass is > 0 and <= 100000
            && i.OriginalMass >= i.Mass && i.Duration > 0 && i.Age >= 0 && Enum.IsDefined(typeof(LifeItemKind), i.Kind)
            && Enum.IsDefined(typeof(LifeHolder), i.Holder) && float.IsFinite(i.X) && float.IsFinite(i.Y)));
        Require(s.Items.Select(i => i.Id).Distinct().Count() == s.Items.Count
            && s.Items.Count(i => i.Holder == LifeHolder.Pet) <= 1 && s.Items.Count(i => i.Holder == LifeHolder.User) <= 1);
        Require(s.Events.All(e => e is not null && e.Id > 0 && ids.Contains(e.Site) && e.AtSecond <= s.Seconds
            && e.AtSecond >= 0 && Enum.IsDefined(typeof(LifeEventKind), e.Kind)) && s.Events.Select(e => e.Id).Distinct().Count() == s.Events.Count);
        Require(s.Learning.Releases is >= 0 and <= 2 && s.Learning.TransferOutcomes is >= 0 and <= 3
            && s.Learning.Deaths is not null && s.Learning.Decompositions is not null
            && s.Learning.Deaths.Count <= 2 && s.Learning.Decompositions.Count <= 2
            && s.Learning.Deaths.All(id => id > 0) && s.Learning.Deaths.Distinct().Count() == s.Learning.Deaths.Count
            && s.Learning.Decompositions.All(s.Learning.Deaths.Contains) && s.Learning.Decompositions.Distinct().Count() == s.Learning.Decompositions.Count
            && s.RecentSites.Count <= 6 && s.RecentSites.All(ids.Contains) && s.TotalMatter is >= 0 and <= 10000000);
        if (s.Intent is { } intent) Require(ids.Contains(intent.Site) && (intent.Destination == "" || ids.Contains(intent.Destination))
            && Enum.IsDefined(typeof(LifeTaskKind), intent.Kind) && float.IsFinite(intent.ActionSeconds) && intent.ActionSeconds >= 0);
        Require(s.NextId > s.Items.Select(i => i.Id).Concat(s.Events.Select(e => e.Id)).Concat(s.Sites.Where(site => site.Colony is not null).Select(site => site.Colony!.Id))
            .Concat(s.Learning.Deaths).DefaultIfEmpty(0).Max());
        Require(s.Sites.Count == 0 || s.TotalMatter == s.Reserve + s.Sites.Sum(site => (long)site.Nutrients + (site.Colony?.Mass ?? 0)) + s.Items.Sum(i => (long)i.Mass));
    }
}

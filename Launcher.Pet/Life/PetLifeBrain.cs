using Launcher.Pet.Activities;
using Launcher.Pet.Data;
using Launcher.Pet.Exploration;

namespace Launcher.Pet.Life;

internal sealed class PetLifeBrain
{
    internal LifeWorld World { get; }
    internal PetLifeBrain(LifeWorld world) => World = world;

    internal bool UpdateGround(PetActivityContext c)
    {
        var state = World.State;
        if (c.Actor.Navigation.HatPlatform(c) is not null) { Cancel(); return false; }
        if (state.Intent is null && state.Seconds >= state.RestUntil) Select(c);
        var intent = state.Intent;
        if (intent is null) return false;
        var site = World.Site(intent.PickedUp ? intent.Destination : intent.Site);
        var item = World.Item(intent.Item);
        bool observing = intent.Kind == LifeTaskKind.Observe;
        if (site is null || !c.Actor.Navigation.Available.ContainsKey(site.Platform)
            || (observing && intent.Event != 0 && !state.Events.Any(e => e.Id == intent.Event && !e.Observed))
            || (!observing && (item is null || item.Holder == LifeHolder.User
                || (intent.PickedUp && item.Holder != LifeHolder.Pet)
                || (!intent.PickedUp && (item.Holder != LifeHolder.World || item.Site != intent.Site))
                || (intent.Kind != LifeTaskKind.Recycle && item.Kind != LifeItemKind.Spore))))
        {
            Cancel(); c.Actor.Navigation.CancelRoute(); c.Actor.Reset(c.Now); return true;
        }
        if (site.Platform != c.Actor.Navigation.Support)
        {
            intent.ActionSeconds = 0;
            if (!c.Actor.Navigation.ApproachGoal(c, site.Platform))
            {
                Cancel(); c.Actor.Reset(c.Now);
            }
            return true;
        }
        c.Actor.Navigation.CancelRoute();
        if (c.Actor.Activity is not LifeActivity)
        {
            LifeActivity next = intent.Kind switch
            {
                LifeTaskKind.Observe => new ObserveLifeActivity(),
                LifeTaskKind.Carry => new TransferLifeActivity(),
                LifeTaskKind.Plant => new PlantLifeActivity(),
                _ => new RecycleLifeActivity()
            };
            c.Actor.Change(next, c.Now);
        }
        c.Actor.Activity.Update(c);
        return true;
    }

    private void Select(PetActivityContext c)
    {
        var s = World.State;
        var nav = c.Actor.Navigation;
        var sites = s.Sites.Where(site => nav.CanReach(c, site.Platform)).ToArray();
        if (sites.Length == 0) return;
        var held = s.Items.FirstOrDefault(i => i.Holder == LifeHolder.Pet);
        if (held is not null)
        {
            var target = Destination(sites, held.Kind == LifeItemKind.Remains, s.Learning.Plant);
            if (target is null) { World.Place(held.Id, held.Site, completedByPet: false); return; }
            s.Intent = new() { Kind = held.Kind == LifeItemKind.Remains ? LifeTaskKind.Recycle : s.Learning.Plant ? LifeTaskKind.Plant : LifeTaskKind.Carry,
                Site = held.Site, Destination = target.Id, Item = held.Id, PickedUp = true };
            return;
        }
        var candidates = new List<(LifeIntent Intent, double Weight)>();
        foreach (var e in s.Events.Where(e => !e.Observed && sites.Any(site => site.Id == e.Site)))
        {
            bool useful = e.Kind == LifeEventKind.Released && !s.Learning.Carry
                || e.Kind is LifeEventKind.Established or LifeEventKind.Failed && e.Related != 0 && !s.Learning.Plant
                || e.Kind == LifeEventKind.Died && s.Learning.Deaths.Count < 2
                || e.Kind == LifeEventKind.Decomposed && s.Learning.Deaths.Contains(e.Related) && !s.Learning.Decompositions.Contains(e.Related);
            if (useful) candidates.Add((new() { Kind = LifeTaskKind.Observe, Site = e.Site, Event = e.Id }, 8 * Weight(e.Site)));
        }
        foreach (var item in s.Items.Where(i => i.Holder == LifeHolder.World && sites.Any(site => site.Id == i.Site)))
        {
            bool remains = item.Kind == LifeItemKind.Remains;
            if (remains ? !s.Learning.Recycle || item.Mass < 100 : !s.Learning.Carry) continue;
            var destination = Destination(sites.Where(site => site.Id != item.Site).ToArray(), remains, s.Learning.Plant);
            if (destination is null) continue;
            candidates.Add((new() { Kind = remains ? LifeTaskKind.Recycle : s.Learning.Plant ? LifeTaskKind.Plant : LifeTaskKind.Carry,
                Site = item.Site, Destination = destination.Id, Item = item.Id }, (remains ? 3 : 4) * Weight(item.Site)));
        }
        if (candidates.Count == 0)
        {
            foreach (var site in sites) candidates.Add((new() { Kind = LifeTaskKind.Observe, Site = site.Id }, Weight(site.Id) * (site.Colony is null ? 1 : 3)));
        }
        double choice = World.Choice() * candidates.Sum(candidate => candidate.Weight);
        var selected = candidates[^1].Intent;
        foreach (var candidate in candidates)
        {
            choice -= candidate.Weight;
            if (choice <= 0) { selected = candidate.Intent; break; }
        }
        s.Intent = selected;
    }
    private double Weight(string site) => 1.0 / (1 + World.State.RecentSites.Count(id => id == site) * 2);
    private LifeSite? Destination(LifeSite[] sites, bool remains, bool planting)
    {
        var candidates = sites.Where(site => remains || site.Colony is null).ToArray();
        if (candidates.Length == 0) return null;
        double[] weights = candidates.Select(site => Weight(site.Id) * (remains ? 1.0 + Math.Max(0, 12000 - site.Nutrients) / 1000.0
            : planting ? Math.Max(0.1, site.Nutrients / 1000.0) : 1)).ToArray();
        double target = World.Choice() * weights.Sum();
        for (int i = 0; i < candidates.Length; i++) { target -= weights[i]; if (target <= 0) return candidates[i]; }
        return candidates[^1];
    }
    internal void Finish()
    {
        var s = World.State;
        if (s.Intent is { } intent)
        {
            s.RecentSites.Add(intent.PickedUp ? intent.Destination : intent.Site);
            if (s.RecentSites.Count > 6) s.RecentSites.RemoveAt(0);
        }
        s.Intent = null;
        s.RestUntil = s.Seconds + 2;
    }
    internal void Cancel(string? restingSite = null)
    {
        foreach (var item in World.State.Items.Where(i => i.Holder == LifeHolder.Pet).ToArray()) World.Place(item.Id, restingSite ?? item.Site, completedByPet: false);
        World.State.Intent = null;
        World.State.RestUntil = World.State.Seconds + 2;
    }
}

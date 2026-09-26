using System.Drawing;
using Launcher.Pet.Behavior;

namespace Launcher.Pet.Activities;

// Scheduling and interrupt priority only. No current mode, animation or movement
// state lives here. New scheduled mechanics can be selected without a tick switch.
internal sealed class PetRoutine
{
    private readonly Random _random;
    internal PetRoutine(Random random) => _random = random;
    private long _jumpAtMs, _walkAtMs;
    internal void ScheduleJump(long now) => _jumpAtMs = now + _random.Next(JumpActivity.MinDelayMs, JumpActivity.MaxDelayMs + 1);
    internal void ScheduleWalk(long now) => _walkAtMs = now + _random.Next(WalkActivity.MinDelayMs, WalkActivity.MaxDelayMs + 1);
    internal void JumpStarted() => _jumpAtMs = long.MaxValue;
    internal void WalkStarted() => _walkAtMs = long.MaxValue;

    internal void Update(PetActivityContext c)
    {
        var actor = c.Actor;
        if (actor.Activity.UpdateBeforeRoutine(c)) return;
        Point? pickup = c.Environment.Ruins is null
            ? actor.Hat.PickupPoint(c.Environment.Surfaces, c.Environment.PickupSurfaceIdentity) : null;
        if (pickup is not null && actor.Activity.CanRetrieveHat && c.HeadVisible)
        {
            actor.Change(new RetrieveHatActivity(), c.Now);
            // The first approach tick intentionally does not start putting on yet.
            ((RetrieveHatActivity)actor.Activity).Walk(c, pickup.Value);
            return;
        }
        if (c.Environment.CanTrackCursor && actor.Activity.CanLook(pickup is not null))
        {
            if (actor.Activity is not LookActivity) actor.Change(new LookActivity(), c.Now);
            actor.Activity.Update(c);
            return;
        }
        if (actor.Activity is LookActivity) actor.Change(new IdleActivity(), c.Now);
        bool jumpDue = c.Environment.Ruins is null && c.Now >= _jumpAtMs;
        bool walkDue = c.Environment.Ruins is null && c.Now >= _walkAtMs;
        actor.Speech.Update(c.Now, actor.Activity.Calm, c.HeadVisible, jumpDue || walkDue);
        if (actor.Activity is IdleActivity)
        {
            if (!actor.Speech.IsSpeaking && jumpDue)
                actor.Change(new JumpActivity(c.Body, c.Environment), c.Now);
            else if (!actor.Speech.IsSpeaking && walkDue)
            {
                var walk = WalkActivity.Create(c.Body, c.Environment, _random);
                if (walk is null) { ScheduleWalk(c.Now); PetPlacement.Fit(c.Body, c.Environment); return; }
                actor.Change(walk, c.Now);
            }
            else if (c.Now - actor.Activity.StartedAtMs >= WaveActivity.DelayMs)
                actor.Change(new WaveActivity(), c.Now);
        }
        actor.Activity.Update(c);
        PetPlacement.Fit(c.Body, c.Environment);
    }
}

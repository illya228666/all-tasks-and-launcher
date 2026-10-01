using Launcher.Pet.Activities;
using Launcher.Pet.Animation;
using Launcher.Pet.Life;

namespace Launcher.Pet.Sprites;

/// <summary>Presentation follows the active activity; it never awards ecological results.</summary>
internal static class PetVisuals
{
    internal static PetVisualPose Resolve(PetActor actor, long now)
    {
        var body = actor.Body;
        var appearance = body.Appearance;
        if (!appearance.UsesClipFiles) return new(body.Row, body.Frame);
        long elapsed = Math.Max(0, now - actor.Activity.StartedAtMs);
        string name;
        float? progress = null;
        var intent = actor.Life?.World.State.Intent;
        bool carrying = intent?.PickedUp == true;
        if (actor.Activity is LifeGestureFinishActivity finish)
        {
            var finishedClip = appearance.Clip(finish.Clip)!;
            return new(finishedClip.Row, finish.Frame(elapsed));
        }
        if (actor.Activity is LifeActivity && intent is not null && body.Mode != Data.PetMode.Walking)
        {
            name = intent.Kind == LifeTaskKind.Observe ? "observe" : !carrying ? "pickup" : intent.Kind switch
            { LifeTaskKind.Plant => "plant", LifeTaskKind.Recycle => "recycle", _ => "place" };
            var lifeClip = appearance.Clip(name)!;
            int commitFrame = lifeClip.Phases.GetValueOrDefault("commit", lifeClip.Durations.Length - 1);
            return new(lifeClip.Row, Math.Min(commitFrame, (int)(intent.ActionSeconds / 2 * (commitFrame + 1))));
        }
        else if (actor.Activity is GrabActivity) { name = carrying ? "carry-climb" : "grab"; progress = carrying ? 0 : elapsed / 200f; }
        else if (actor.Activity is PullUpActivity) { name = carrying ? "carry-climb" : "pull-up"; progress = carrying ? 1 : elapsed / 320f; }
        else if (actor.Activity is ClimbingActivity) name = carrying ? "carry-climb" : "climb";
        else if (actor.Activity is DragActivity) name = "drag";
        else if (actor.Activity is DepartureActivity departure) { name = departure.HasLanded ? "recovery" : "jump"; progress = departure.VisualProgress; }
        else if (actor.Activity is EarthquakeActivity) { name = elapsed < 3400 ? "failed" : "recovery"; progress = elapsed < 3400 ? Math.Min(.6f, elapsed / 1300f) : (elapsed - 3400) / 1600f; }
        else if (actor.Activity is RecoveryActivity) { name = "recovery"; progress = elapsed / 1260f; }
        else if (actor.Activity is LandingActivity) { name = "jump"; progress = .75f + .25f * elapsed / 240f; }
        else if (actor.Activity is PutOnHatActivity or RuinHatPickupActivity) { name = "hat-pickup"; progress = elapsed / 960f; }
        else if (actor.Activity is FlightActivity flight)
        {
            name = flight.IsDropped ? "fall" : "jump";
            progress = flight.VisualProgress;
        }
        else if (actor.Activity is JumpActivity) return new(body.Row, body.Frame);
        else if (body.Row == PetAnimationCatalog.FailedRow) { name = "failed"; progress = body.Frame / 7f; }
        else if (body.Row == PetAnimationCatalog.JumpRow) { name = "jump"; progress = body.Frame / 15f; }
        else if (body.Row is 1 or 2) name = carrying ? body.Row == 1 ? "carry-right" : "carry-left" : body.Row == 1 ? "walk-right" : "walk-left";
        else if (body.Row == 3) name = "wave";
        else if (body.Row is 9 or 10)
        {
            var look = appearance.Clip(body.Row)!;
            return new(look.Row, Math.Clamp(body.Frame, 0, look.Durations.Length - 1));
        }
        else name = (elapsed / 1680 & 1) == 0 ? "idle" : "idle-curious";
        var clip = appearance.Clip(name) ?? appearance.Clip("idle")!;
        return new(clip.Row, progress is float p ? clip.FrameAtProgress(p) : clip.FrameAt(elapsed));
    }
}

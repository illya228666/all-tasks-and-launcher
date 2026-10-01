using Launcher.Pet.Animation;
using Launcher.Pet.Data;
using Launcher.Pet.Exploration;

namespace Launcher.Pet.Activities;

internal sealed class FlightActivity : PetActivity
{
    private float _vx, _vy, _time;
    private readonly bool _dropped;
    private readonly float _initialVelocityY;
    internal bool IsDropped => _dropped;
    internal bool Ascent => _vy < 0;
    internal float VisualProgress => _dropped ? Math.Min(.99f, _time / .8f)
        : _initialVelocityY < 0 ? Math.Clamp(.2f + (_vy - _initialVelocityY) / -_initialVelocityY * .28f, .2f, .74f) : .65f;
    internal FlightActivity(float velocityX = 0, float velocityY = 0, bool dropped = false)
        => (_vx, _vy, _initialVelocityY, _dropped) = (velocityX, velocityY, velocityY, dropped);
    internal override PetMode Mode => _dropped ? PetMode.Falling : PetMode.Traversing;
    internal override bool FallsOnEarthquake => true;
    internal override void Update(PetActivityContext c)
    {
        var nav = c.Actor.Navigation;
        var scene = c.Environment.Ruins!;
        float previousY = nav.FootY;
        _time += c.Step;
        c.Body.X += _vx * c.Step;
        nav.FootY += _vy * c.Step + c.Metrics.Gravity * c.Step * c.Step / 2;
        _vy += c.Metrics.Gravity * c.Step;
        float center = c.Body.X + c.HalfWidth;
        if (center < c.Metrics.HalfBody || center > scene.Size.Width - c.Metrics.HalfBody)
        {
            c.Body.X = Math.Clamp(center, c.Metrics.HalfBody, Math.Max(c.Metrics.HalfBody, scene.Size.Width - c.Metrics.HalfBody)) - c.HalfWidth;
            _vx = 0;
        }
        var landing = _vy >= 0 ? nav.Available.Values.Where(p => previousY <= p.Y && nav.FootY >= p.Y && center >= p.Left + c.Metrics.HalfBody && center <= p.Right - c.Metrics.HalfBody).OrderBy(p => p.Y).FirstOrDefault() : null;
        if (landing is null && _vy >= 0)
        {
            var bridge = scene.Bridges.FirstOrDefault(b => c.Environment.AwakeningSeconds >= b.RevealAt + 0.85f
                && previousY <= b.Y && nav.FootY >= b.Y && center >= b.Left + c.Metrics.HalfBody && center <= b.Right - c.Metrics.HalfBody);
            if (bridge is not null) landing = nav.Available[center < (bridge.Left + bridge.Right) / 2 ? bridge.From : bridge.To];
        }
        if (landing is null && nav.FootY >= scene.Size.Height) landing = nav.Available["ruin:floor"];
        if (landing is not null)
        {
            nav.Land(c, landing);
            if (_dropped)
            {
                c.Actor.ContinueWith(new RecoveryActivity(), c.Now);
                c.Actor.Present(PetMode.Recovering);
                c.Body.Row = PetAnimationCatalog.DragRow;
                c.Body.Frame = 4;
            }
            else if (c.Body.Appearance.UsesClipFiles) c.Actor.ContinueWith(new LandingActivity(), c.Now);
            return;
        }
        c.Actor.Present(Mode);
        c.Body.Row = _dropped ? PetAnimationCatalog.DragRow : PetAnimationCatalog.JumpRow;
        c.Body.Frame = _dropped ? (_time < 0.22f ? 2 : 3) : (_vy < 0 ? 1 : 3);
    }
}

internal sealed class LandingActivity : PetActivity
{
    internal override PetMode Mode => PetMode.Traversing;
    internal override void Update(PetActivityContext c)
    {
        c.Actor.Present(Mode);
        if (c.Now - StartedAtMs >= 240) c.Actor.ContinueWith(new IdleActivity(), c.Now);
    }
}

internal sealed class RecoveryActivity : PetActivity
{
    internal override PetMode Mode => PetMode.Recovering;
    internal override bool ShowsRoute => false;
    internal override void Update(PetActivityContext c)
    {
        var nav = c.Actor.Navigation;
        nav.FootY = nav.Available.TryGetValue(nav.Support, out var resting) ? resting.Y : c.Environment.Ruins!.Size.Height;
        long recovery = c.Now - StartedAtMs;
        if (recovery >= 1260)
        {
            c.Actor.Reset(c.Now);
            nav.NextTrip = c.Now + 3000;
        }
        else
        {
            c.Actor.Present(Mode);
            c.Body.Row = PetAnimationCatalog.DragRow;
            c.Body.Frame = recovery < 120 ? 4 : recovery < 820 ? 5 : recovery < 1040 ? 6 : 7;
        }
    }
}

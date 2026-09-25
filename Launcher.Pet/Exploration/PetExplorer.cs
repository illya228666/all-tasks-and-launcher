using System.Drawing;
using Launcher.Pet.Animation;
using Launcher.Pet.Behavior;
using Launcher.Pet.Data;
using Launcher.Pet.Hat;
using Launcher.Pet.Speech;

namespace Launcher.Pet.Exploration;

internal sealed class PetExplorer
{
    private readonly Random _random;
    private readonly Dictionary<string, int> _visits = new();
    private string _support = "ruin:floor";
    private RuinLink? _link;
    private string? _goal;
    private int _revision;
    private float _footY, _time, _vx, _vy;
    private float _dragOffsetX, _dragOffsetY;
    private long _nextTrip;
    private bool _initialized, _dropped;
    private int _phase; // 0 rest, 1 approach, 2 grab, 3 climb, 4 pull, 5 flight, 6 pickup, 7 drag, 8 recovery
    internal PetExplorer(Random random) => _random = random;
    internal bool IsAirborne => _phase is 2 or 3 or 4 or 5;
    internal bool IsDragging => _phase == 7;
    internal void CancelRoute()
    {
        _phase = 0;
        _link = null;
        _goal = null;
        _dropped = false;
    }
    internal void Fall(PetState state)
    {
        _phase = 5;
        _link = null;
        _vx = 0;
        _vy = 0;
        _dropped = false;
        state.Mode = PetMode.Falling;
    }

    internal bool BeginDrag(PetState state, PetEnvironment env, PetSpeech speech, Point cursor, long now)
    {
        if (!_initialized || env.Ruins is null)
            return false;
        float half = PetLogicalGeometry.Width * env.Scale / 2;
        _dragOffsetX = cursor.X - env.AreaScreenPosition.X - state.X - half;
        _dragOffsetY = cursor.Y - env.AreaScreenPosition.Y - _footY;
        _phase = 7;
        _link = null;
        _goal = null;
        _vx = _vy = 0;
        _dropped = false;
        state.Mode = PetMode.Dragging;
        state.StartedAtMs = now;
        state.Row = PetAnimationCatalog.DragRow;
        state.Frame = 0;
        speech.Reset(now);
        MoveDrag(state, env, cursor);
        return true;
    }

    internal void MoveDrag(PetState state, PetEnvironment env, Point cursor)
    {
        if (_phase != 7 || env.Ruins is null)
            return;
        float half = PetLogicalGeometry.Width * env.Scale / 2;
        state.X = Math.Clamp(cursor.X - env.AreaScreenPosition.X - _dragOffsetX - half, 0, Math.Max(0, env.Ruins.Size.Width - half * 2));
        _footY = Math.Clamp(cursor.Y - env.AreaScreenPosition.Y - _dragOffsetY,
            PetLogicalGeometry.Height * env.Scale, env.Ruins.Size.Height);
        state.JumpLift = env.PetZoneTopY + PetLogicalGeometry.Height - _footY;
    }

    internal void Drop(PetState state, long now)
    {
        if (_phase != 7)
            return;
        _phase = 5;
        _dropped = true;
        _time = _vy = _vx = 0;
        state.Mode = PetMode.Falling;
        state.StartedAtMs = now;
        state.Row = PetAnimationCatalog.DragRow;
        state.Frame = 2;
    }

    internal void Update(PetState state, PetEnvironment env, HatWorld hat, PetBehavior behavior, PetSpeech speech, long now, float elapsed)
    {
        RuinScene scene = env.Ruins!;
        float dt = Math.Clamp(elapsed, 0, 0.04f);
        float half = PetLogicalGeometry.Width * env.Scale / 2;
        float center = state.X + half;
        var available = scene.Platforms.Where(p => p.Id == "ruin:floor" || env.AwakeningSeconds >= p.RevealAt + 0.85f).ToDictionary(p => p.Id);
        if (!_initialized || _revision != scene.Revision)
        {
            _initialized = true;
            _revision = scene.Revision;
            _support = "ruin:floor";
            _footY = scene.Size.Height;
            _phase = 0;
            _dropped = false;
            _link = null;
            _goal = null;
            state.X = Math.Clamp(state.X, 0, Math.Max(0, scene.Size.Width - half * 2));
            behavior.Reset(now, speech);
            _nextTrip = now + 1200;
        }
        if (_phase == 7)
        {
            state.Mode = PetMode.Dragging;
            state.Row = PetAnimationCatalog.DragRow;
            state.Frame = (int)((now - state.StartedAtMs) / 180 % 2);
        }
        else if (_phase == 8)
        {
            _footY = available.TryGetValue(_support, out RuinPlatform? resting) ? resting.Y : scene.Size.Height;
            long recovery = now - state.StartedAtMs;
            if (recovery >= 1260)
            {
                _phase = 0;
                behavior.Reset(now, speech);
                _nextTrip = now + 3000;
            }
            else
            {
                state.Mode = PetMode.Recovering;
                state.Row = PetAnimationCatalog.DragRow;
                state.Frame = recovery < 120 ? 4 : recovery < 820 ? 5 : recovery < 1040 ? 6 : 7;
            }
        }
        else if (_phase == 5)
        {
            float previousY = _footY;
            _time += dt;
            state.X += _vx * dt;
            _footY += _vy * dt + RuinMotion.Gravity * dt * dt / 2;
            _vy += RuinMotion.Gravity * dt;
            center = state.X + half;
            if (center < RuinMotion.HalfBody || center > scene.Size.Width - RuinMotion.HalfBody)
            {
                state.X = Math.Clamp(center, RuinMotion.HalfBody, Math.Max(RuinMotion.HalfBody, scene.Size.Width - RuinMotion.HalfBody)) - half;
                _vx = 0;
            }
            var landing = _vy >= 0 ? available.Values.Where(p => previousY <= p.Y && _footY >= p.Y && center >= p.Left + RuinMotion.HalfBody && center <= p.Right - RuinMotion.HalfBody).OrderBy(p => p.Y).FirstOrDefault() : null;
            if (landing is not null) FinishFall(landing, state, behavior, speech, now);
            else if (_footY >= scene.Size.Height) FinishFall(available["ruin:floor"], state, behavior, speech, now);
            else
            {
                state.Mode = _dropped ? PetMode.Falling : PetMode.Traversing;
                state.Row = _dropped ? PetAnimationCatalog.DragRow : PetAnimationCatalog.JumpRow;
                state.Frame = _dropped ? (_time < 0.22f ? 2 : 3) : (_vy < 0 ? 1 : 3);
            }
        }
        else if (_phase is 2 or 3 or 4 && _link is not null)
        {
            _time += dt;
            var target = available[_link.To];
            var source = available[_link.From];
            float progress = Math.Clamp((_footY - source.Y) / (target.Y - source.Y), 0, 1);
            state.X = _link.X + ((_link.EndX ?? _link.X) - _link.X) * progress - half;
            state.Row = PetAnimationCatalog.ClimbRow;
            if (_phase == 2)
            {
                state.Mode = PetMode.Grabbing; state.Frame = 0;
                if (_time >= 0.2f) { _phase = 3; _time = 0; }
            }
            else if (_phase == 3)
            {
                state.Mode = PetMode.Climbing;
                state.Frame = 1 + (int)(_time / 0.18f) % 2;
                float distance = target.Y - _footY;
                _footY += Math.Clamp(distance, -RuinMotion.ClimbSpeed * dt, RuinMotion.ClimbSpeed * dt);
                if (Math.Abs(distance) <= RuinMotion.ClimbSpeed * dt) { _phase = 4; _time = 0; }
            }
            else
            {
                state.Mode = PetMode.PullingUp; state.Frame = 3;
                if (_time >= 0.32f) Land(target, state, behavior, speech, now);
            }
        }
        else
        {
            RuinPlatform support = available[_support];
            _footY = support.Y;
            Point? hatPoint = hat.RestingPoint(env.Surfaces);
            float hatX = hatPoint?.X - env.AreaScreenPosition.X ?? 0;
            float hatY = hatPoint?.Y - env.AreaScreenPosition.Y ?? 0;
            RuinPlatform? hatPlatform = hatPoint is null ? null : available.Values.FirstOrDefault(p => Math.Abs(p.Y - hatY) <= 3 && hatX >= p.Left + RuinMotion.HalfBody && hatX <= p.Right - RuinMotion.HalfBody);
            if (_phase == 6)
            {
                if (hatPlatform?.Id != _support && !hat.Attached) { _phase = 0; behavior.Reset(now, speech); }
                else if (PetHatPickup.PutOn(state, hat, now)) { _phase = 0; behavior.Reset(now, speech); _nextTrip = now + 3000; }
            }
            else if (env.CanTrackCursor || state.Mode == PetMode.Earthquake)
            {
                if (_phase != 0 || state.Mode == PetMode.Walking)
                {
                    _phase = 0; _link = null;
                    if (state.Mode != PetMode.Earthquake) behavior.Reset(now, speech);
                }
                state.JumpLift = 0;
                behavior.Update(now, dt, env with { PetZoneTopY = (int)_footY - PetLogicalGeometry.Height }, hat, speech, true);
            }
            else if (hatPlatform?.Id == _support)
            {
                if (WalkTo(state, hatX, half, now, dt))
                {
                    _phase = 6; state.Mode = PetMode.PuttingOnHat; state.StartedAtMs = now; speech.Reset(now);
                }
            }
            else
            {
                if (hatPlatform is not null) _goal = hatPlatform.Id;
                if (_phase == 0 && (_goal is not null || now >= _nextTrip))
                {
                    _goal ??= available.Values.Where(p => p.Id != _support).OrderBy(p => _visits.GetValueOrDefault(p.Id) + _random.NextDouble()).Select(p => p.Id).FirstOrDefault();
                    _link = _goal is null ? null : FirstStep(scene, available, _support, _goal);
                    if (_link is not null) { _phase = 1; speech.Reset(now); }
                    else { _goal = null; _nextTrip = now + 1500; }
                }
                if (_phase == 1 && _link is not null)
                {
                    float approach = _link.Climb ? _link.X : support.Center;
                    if (WalkTo(state, approach, half, now, dt))
                    {
                        _time = 0;
                        if (_link.Climb) _phase = 2;
                        else
                        {
                            var target = available[_link.To];
                            float duration = RuinMotion.FlightTime(_footY, target.Y);
                            _vx = (target.Center - (state.X + half)) / duration;
                            _vy = -RuinMotion.LaunchSpeed(_footY);
                            _phase = 5;
                        }
                    }
                }
                else if (_phase == 0)
                {
                    if (state.Mode == PetMode.Walking) behavior.Reset(now, speech);
                    state.JumpLift = 0;
                    behavior.Update(now, dt, env with { PetZoneTopY = (int)_footY - PetLogicalGeometry.Height }, hat, speech, true);
                }
            }
        }
        state.JumpLift = env.PetZoneTopY + PetLogicalGeometry.Height - _footY;
    }

    private static bool WalkTo(PetState state, float target, float half, long now, float dt)
    {
        float delta = target - (state.X + half);
        state.X += Math.Clamp(delta, -RuinMotion.WalkSpeed * dt, RuinMotion.WalkSpeed * dt);
        state.Mode = PetMode.Walking;
        state.Row = delta >= 0 ? PetAnimationCatalog.MoveRightRow : PetAnimationCatalog.MoveLeftRow;
        state.Frame = (int)(now / 120 % 8);
        return Math.Abs(delta) <= RuinMotion.WalkSpeed * dt;
    }
    private void Land(RuinPlatform platform, PetState state, PetBehavior behavior, PetSpeech speech, long now)
    {
        _support = platform.Id; _footY = platform.Y; _phase = 0; _link = null;
        _visits[_support] = _visits.GetValueOrDefault(_support) + 1;
        if (_goal == _support) _goal = null;
        behavior.Reset(now, speech);
        _nextTrip = now + _random.Next(3500, 8000);
    }
    private void FinishFall(RuinPlatform platform, PetState state, PetBehavior behavior, PetSpeech speech, long now)
    {
        bool dropped = _dropped;
        _dropped = false;
        Land(platform, state, behavior, speech, now);
        if (!dropped)
            return;
        _phase = 8;
        state.Mode = PetMode.Recovering;
        state.StartedAtMs = now;
        state.Row = PetAnimationCatalog.DragRow;
        state.Frame = 4;
    }
    private static RuinLink? FirstStep(RuinScene scene, Dictionary<string, RuinPlatform> available, string start, string goal)
    {
        var queue = new Queue<string>();
        var parents = new Dictionary<string, RuinLink>();
        queue.Enqueue(start);
        var visited = new HashSet<string> { start };
        while (queue.TryDequeue(out string? from))
            foreach (var edge in scene.Links.Where(e => e.From == from && available.ContainsKey(e.To)))
            {
                if (!visited.Add(edge.To)) continue;
                parents[edge.To] = edge;
                if (edge.To == goal)
                {
                    var step = edge;
                    while (step.From != start) step = parents[step.From];
                    return step;
                }
                queue.Enqueue(edge.To);
            }
        return null;
    }
}

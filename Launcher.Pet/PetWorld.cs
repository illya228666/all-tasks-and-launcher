using System.Drawing;
using Launcher.Pet.Activities;
using Launcher.Pet.Behavior;
using Launcher.Pet.Data;
using Launcher.Pet.Hat;
using Launcher.Pet.Speech;
using Launcher.Pet.Exploration;
using Launcher.Pet.Life;
using System.Text.Json;

namespace Launcher.Pet;
public sealed class PetWorld
{
    private PetBody Body => _actor.Body;
    private readonly HatWorld _hat = new();
    private readonly PetSpeech _speech;
    private readonly PetActor _actor;
    private PetEnvironment? _environment;
    private long _lastMs;
    private bool _running;
    private float _detachedHatFadeScale = 1f;
    private LifeWorld? _life;
    private RuinScene? _lifeGeometry;
    private PetLifeSave? _restorePosition;
    public bool HasLife => _life is not null;
    public bool IsWorldItemDragging => _life?.State.Items.Any(i => i.Holder == LifeHolder.User) == true;

    public void CreateLife(RuinScene geometry, ulong seed)
    {
        if (_life is not null) return;
        _life = LifeWorld.Create(geometry, seed);
        _lifeGeometry = geometry;
        _actor.Life = new(_life);
    }

    public PetLifeSave? CaptureLife(string? monitor = null)
    {
        if (_life is null || _lifeGeometry is null) return null;
        return new() { Geometry = _lifeGeometry, Life = JsonSerializer.Deserialize<LifeState>(JsonSerializer.Serialize(_life.State))!,
            Monitor = monitor, PetXFraction = _restorePosition?.PetXFraction ?? (float.IsFinite(Body.X)
                ? Math.Clamp((Body.X + PetLogicalGeometry.Width * RenderScale / 2) / _lifeGeometry.Size.Width, 0, 1) : .5f),
            PetPlatform = _restorePosition?.PetPlatform ?? _actor.Navigation.Support };
    }

    public void RestoreLife(PetLifeSave saved, long nowMs)
    {
        LifeGeometry.Validate(saved.Geometry);
        LifeWorld.Validate(saved.Life);
        if (saved.Version != 1 || saved.Life.Sites.Count != 12 || !float.IsFinite(saved.PetXFraction) || saved.PetXFraction is < 0 or > 1)
            throw new InvalidDataException("Invalid desktop life checkpoint.");
        _life = new(JsonSerializer.Deserialize<LifeState>(JsonSerializer.Serialize(saved.Life))!);
        _lifeGeometry = saved.Geometry;
        _life.Reanchor(saved.Geometry);
        foreach (var item in _life.State.Items.Where(i => i.Holder == LifeHolder.User)) item.Holder = LifeHolder.World;
        _actor.Life = new(_life);
        _actor.Location = PetLocation.Desktop;
        Body.X = saved.PetXFraction * saved.Geometry.Size.Width - PetLogicalGeometry.Width * RenderScale / 2;
        _actor.ResetSession(nowMs);
        _restorePosition = saved;
        _lastMs = nowMs;
    }

    public bool BeginWorldItemDrag(long id, Point screenPoint)
    {
        if (!_running || _environment is null || _life is null || Location != PetLocation.Desktop
            || IsPetDragging || IsHatDragging || IsWorldItemDragging || !_life.PickUp(id, LifeHolder.User)) return false;
        CancelLife();
        if (_actor.Activity is LifeActivity or ApproachActivity) { _actor.Navigation.CancelRoute(); _actor.Reset(_lastMs); }
        MoveWorldItem(screenPoint);
        return true;
    }
    public void MoveWorldItem(Point screenPoint)
    {
        var item = _life?.State.Items.FirstOrDefault(i => i.Holder == LifeHolder.User);
        if (item is null || _environment?.Ruins is not { } geometry) return;
        item.X = Math.Clamp(screenPoint.X - _environment.AreaScreenPosition.X, 0, geometry.Size.Width);
        item.Y = Math.Clamp(screenPoint.Y - _environment.AreaScreenPosition.Y, 0, geometry.Size.Height);
    }
    public void DropWorldItem()
    {
        var item = _life?.State.Items.FirstOrDefault(i => i.Holder == LifeHolder.User);
        if (item is null || _lifeGeometry is null) return;
        var platform = _lifeGeometry.Platforms.Where(p => item.X >= p.Left && item.X <= p.Right && p.Y >= item.Y - 12)
            .OrderBy(p => Math.Abs(p.Y - item.Y)).FirstOrDefault() ?? _lifeGeometry.Platforms.First(p => p.Id == "ruin:floor");
        var site = _life!.State.Sites.Where(s => s.Platform == platform.Id).OrderBy(s => Math.Abs(LifeWorld.Position(s, _lifeGeometry).X - item.X)).FirstOrDefault()
            ?? _life.State.Sites.Where(s => s.Platform == "ruin:floor").OrderBy(s => Math.Abs(LifeWorld.Position(s, _lifeGeometry).X - item.X)).First();
        _life.Place(item.Id, site.Id);
    }
    private static float Distance(PointF a, PointF b) => (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);

    private void CancelLife()
    {
        if (_life is null || _lifeGeometry is null) return;
        var site = _life.State.Sites.OrderBy(s => Distance(LifeWorld.Position(s, _lifeGeometry), new(Body.X + PetLogicalGeometry.Width * RenderScale / 2, _actor.Navigation.FootY))).First();
        _actor.Life!.Cancel(site.Id);
    }
    public float RenderScale => _actor.RenderScale;
    private float HatRenderScale => RenderScale * (_hat.Attached ? 1f : _detachedHatFadeScale);
    public bool HasLanded => _actor.HasLanded;
    public PetLocation Location => _actor.Location;
    public bool IsHatDragging => _hat.Scene.Mode == HatMode.Dragging;
    public bool IsPetDragging => Location == PetLocation.Desktop && _actor.Activity.IsDragging;
    public IReadOnlyList<RuinLink> PlannedRoute => Location == PetLocation.Desktop && _environment?.Ruins is RuinScene scene
        ? _actor.Navigation.PlannedRoute(scene, _environment.AwakeningSeconds, _actor.Activity) : Array.Empty<RuinLink>();
    public Point? PlannedRouteStart => Location == PetLocation.Desktop && _environment is not null
        ? new(_environment.AreaScreenPosition.X + (int)MathF.Round(Body.X + PetLogicalGeometry.Width * _environment.Scale / 2),
            _environment.AreaScreenPosition.Y + (int)MathF.Round(_actor.Navigation.FootY)) : null;
    public Point? PlannedHatTarget => Location == PetLocation.Desktop && _actor.Navigation.SeekingHat && _environment is not null
        ? _hat.RestingPoint(_environment.Surfaces) : null;

    public bool LeaveLauncher(long nowMs)
    {
        if (!_running || Location != PetLocation.Launcher || _environment is null)
            return false;
        Point position = PetPlacement.LogicalPosition(Body, _environment);
        position.Offset(_environment.AreaScreenPosition);
        _actor.ResetSession(nowMs);
        _actor.Location = PetLocation.LeavingLauncher;
        _actor.ContinueWith(new DepartureActivity(position), nowMs);
        return true;
    }
    public PetScene? Scene { get; private set; }
    public void SetAppearance(Sprites.PetAppearance appearance, long nowMs)
    {
        ArgumentNullException.ThrowIfNull(appearance);
        if (ReferenceEquals(Body.Appearance, appearance)) return;
        Body.Appearance = appearance;
        _actor.ResetSession(nowMs);
        RefreshScene();
    }

    public PetWorld(Random random)
    {
        _speech = new(random);
        _actor = new(random, _hat, _speech);
    }

    public void Start(long nowMs)
    {
        if (_running)
            return;
        _running = true;
        _lastMs = nowMs;
        _actor.ResetSession(nowMs);
    }

    public void Stop(long nowMs)
    {
        DropWorldItem();
        _running = false;
        if (_hat.Scene.Mode == HatMode.Dragging)
            _hat.Drop(false);
        _actor.Navigation.CancelRoute();
        _actor.ResetSession(nowMs);
        if (_environment is not null)
            Scene = CreateScene(_environment);
    }

    public PetScene Update(long nowMs, PetEnvironment environment)
    {
        long elapsedMs = Math.Max(0, nowMs - _lastMs);
        float elapsed = elapsedMs / 1000f;
        _lastMs = nowMs;
        _actor.DesktopScale = environment.Ruins?.WorldMetrics.RenderScale ?? .5f;
        environment = environment with { Scale = RenderScale };
        _environment = environment;
        if (_running)
        {
            if (_life is not null && environment.Ruins is { } geometry && Location == PetLocation.Desktop)
            {
                if (!ReferenceEquals(_lifeGeometry, geometry)) { _lifeGeometry = geometry; _life.Reanchor(geometry); }
                if (_restorePosition is { } saved)
                {
                    _actor.Navigation.Prepare(new(_actor, environment, nowMs, 0));
                    _actor.Navigation.RestoreSupport(saved.PetPlatform, geometry);
                    Body.X = Math.Clamp(saved.PetXFraction * geometry.Size.Width - PetLogicalGeometry.Width * RenderScale / 2, 0, Math.Max(0, geometry.Size.Width - PetLogicalGeometry.Width * RenderScale));
                    _restorePosition = null;
                }
                // Suspend/resume is a pause, not an offline catch-up.
                if (elapsed <= 5) _life.Advance(elapsedMs / 1000d);
            }
            _hat.Update(elapsed, environment.Surfaces);
            _actor.Update(new(_actor, environment, nowMs, elapsed));
        }

        environment = environment with { Scale = RenderScale };
        _environment = environment;
        _hat.SetScale(HatRenderScale);
        return Scene = CreateScene(environment);
    }

    public void SetHatFade(byte fade)
    {
        _detachedHatFadeScale = 1f + fade / 255f;
        RefreshScene();
    }

    public bool TryStartEarthquake(long nowMs)
    {
        bool started = _running && EarthquakeActivity.TryStart(Context(nowMs), Scene?.HeadScreenPosition);
        if (started) CancelLife();
        RefreshScene();
        return started;
    }

    public bool BeginHatDrag(Point cursorScreenPosition)
    {
        if (!_running || IsWorldItemDragging || (Location == PetLocation.LeavingLauncher && _hat.Attached) || !_actor.Activity.AllowsHatDrag)
            return false;
        _hat.BeginDrag(cursorScreenPosition);
        CancelLife();
        RefreshScene();
        return true;
    }

    public bool BeginPetDrag(Point cursorScreenPosition, long nowMs)
    {
        if (!_running || IsWorldItemDragging || _environment is null || !DragActivity.TryStart(Context(nowMs), cursorScreenPosition))
            return false;
        CancelLife();
        RefreshScene();
        return true;
    }

    private PetActivityContext Context(long now) => new(_actor, _environment!, now, 0);

    public void MovePet(Point cursorScreenPosition)
    {
        if (IsPetDragging && _environment is not null)
            _actor.Activity.Move(Context(_lastMs), cursorScreenPosition);
    }

    public void DropPet(long nowMs)
    {
        if (IsPetDragging)
            _actor.Activity.Drop(Context(nowMs));
    }

    public void MoveHat(Point cursorScreenPosition)
    {
        if (_hat.Scene.Mode == HatMode.Dragging)
            _hat.Drag(cursorScreenPosition);
    }

    public void DropHat(bool onHead)
    {
        if (_hat.Scene.Mode == HatMode.Dragging)
            _hat.Drop(onHead);
    }

    public void HideSpeech(long nowMs) => _speech.Reset(nowMs);
    public void RestoreHat()
    {
        _hat.Attach();
        RefreshScene();
    }

    private void RefreshScene()
    {
        _hat.SetScale(HatRenderScale);
        if (_environment is not null)
            Scene = CreateScene(_environment);
    }

    private PetScene CreateScene(PetEnvironment environment)
    {
        Point shake = _actor.Shake;
        var pose = Sprites.PetVisuals.Resolve(_actor, _lastMs);
        var frame = Body.Appearance.GetFrameGeometry(pose.Row, pose.Frame, _actor.Hat.Attached);
        Rectangle bounds = Sprites.PetSpriteLayout.GetBounds(PetPlacement.LogicalPosition(Body, environment), frame, shake, environment.Scale, Body.Appearance);
        Point? head = Sprites.PetSpriteLayout.VisibleHead(bounds, frame, environment.AreaScreenPosition, environment.VisibleScreenBounds, Body.Appearance);
        PointF held = frame.ItemAnchor is Point anchor
            ? new(bounds.X + anchor.X * bounds.Width / (float)Body.Appearance.CellSize.Width, bounds.Y + anchor.Y * bounds.Height / (float)Body.Appearance.CellSize.Height)
            : new(Body.X + PetLogicalGeometry.Width * RenderScale / 2 + 14 * (environment.Ruins?.WorldMetrics.MotionScale ?? 1), _actor.Navigation.FootY - 35 * (environment.Ruins?.WorldMetrics.MotionScale ?? 1));
        return new(Body.Mode, pose.Row, pose.Frame, bounds, head, shake, _hat.Attached, _hat.Scene,
            _life is null ? _speech.Phrase : null, _speech.VisibleLetters, RenderScale, _actor.Activity.AllowsHatDrag, _actor.Activity.ShakeWindow)
        { Appearance = Body.Appearance, Life = _life is null || environment.Ruins is null ? null
            : _life.Snapshot(environment.Ruins, held, Body.Appearance.UsesClipFiles) };
    }

}

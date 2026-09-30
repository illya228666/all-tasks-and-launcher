using System.Drawing;
using Launcher.Pet.Activities;
using Launcher.Pet.Behavior;
using Launcher.Pet.Data;
using Launcher.Pet.Hat;
using Launcher.Pet.Speech;
using Launcher.Pet.Exploration;

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
        float elapsed = Math.Max(0, nowMs - _lastMs) / 1000f;
        _lastMs = nowMs;
        _environment = environment;
        if (_running)
        {
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
        RefreshScene();
        return started;
    }

    public bool BeginHatDrag(Point cursorScreenPosition)
    {
        if (!_running || (Location == PetLocation.LeavingLauncher && _hat.Attached) || !_actor.Activity.AllowsHatDrag)
            return false;
        _hat.BeginDrag(cursorScreenPosition);
        RefreshScene();
        return true;
    }

    public bool BeginPetDrag(Point cursorScreenPosition, long nowMs)
    {
        if (!_running || _environment is null || !DragActivity.TryStart(Context(nowMs), cursorScreenPosition))
            return false;
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
        Rectangle bounds = PetPlacement.SpriteBounds(Body, environment, shake);
        Point? head = PetPlacement.VisibleHead(Body, environment, bounds);
        return new(Body.Mode, Body.Row, Body.Frame, bounds, head, shake, _hat.Attached, _hat.Scene, _speech.Phrase, _speech.VisibleLetters, RenderScale, _actor.Activity.AllowsHatDrag, _actor.Activity.ShakeWindow) { Appearance = Body.Appearance };
    }

}

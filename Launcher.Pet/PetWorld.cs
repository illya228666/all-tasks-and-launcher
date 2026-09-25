using System.Drawing;
using Launcher.Pet.Sprites;
using Launcher.Pet.Behavior;
using Launcher.Pet.Data;
using Launcher.Pet.Hat;
using Launcher.Pet.Speech;
using Launcher.Pet.Exploration;

namespace Launcher.Pet;
public sealed class PetWorld
{
    private readonly PetState _state = new();
    private readonly HatWorld _hat = new();
    private readonly PetSpeech _speech;
    private readonly PetBehavior _behavior;
    private PetEnvironment? _environment;
    private long _lastMs;
    private bool _running;
    private PetDeparture? _departure;
    private readonly PetExplorer _explorer;
    public float RenderScale => Location == PetLocation.Launcher ? 1f : _departure?.Scale ?? 0.5f;
    public bool HasLanded => Location == PetLocation.Desktop || _departure?.HasLanded == true;
    public PetLocation Location { get; private set; }
    public bool IsHatDragging => _hat.Scene.Mode == HatMode.Dragging;
    public bool IsPetDragging => Location == PetLocation.Desktop && _explorer.IsDragging;

    public bool LeaveLauncher(long nowMs)
    {
        if (!_running || Location != PetLocation.Launcher || _environment is null)
            return false;
        Point position = PetPlacement.LogicalPosition(_state, _environment);
        position.Offset(_environment.AreaScreenPosition);
        _departure = new(position);
        Location = PetLocation.LeavingLauncher;
        _behavior.Reset(nowMs, _speech);
        return true;
    }
    public PetScene? Scene { get; private set; }

    public PetWorld(Random random)
    {
        _speech = new(random);
        _explorer = new(random);
        _behavior = new(_state, random);
    }

    public void Start(long nowMs)
    {
        if (_running)
            return;
        _running = true;
        _lastMs = nowMs;
        _behavior.Reset(nowMs, _speech);
    }

    public void Stop(long nowMs)
    {
        _running = false;
        if (_hat.Scene.Mode == HatMode.Dragging)
            _hat.Drop(false);
        _explorer.CancelRoute();
        _behavior.Reset(nowMs, _speech);
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
            if (_departure is not null)
            {
                if (_departure.Update(_state, environment, elapsed))
                {
                    _departure = null;
                    Location = PetLocation.Desktop;
                    _behavior.Reset(nowMs, _speech);
                }
            }
            else if (Location == PetLocation.Desktop && environment.Ruins is not null)
                _explorer.Update(_state, environment, _hat, _behavior, _speech, nowMs, elapsed);
            else
            {
                PetPlacement.Fit(_state, environment);
                _behavior.Update(nowMs, elapsed, environment, _hat, _speech, GetVisibleHead(environment, GetSpriteBounds(environment, _behavior.Shake)) is not null);
            }
        }

        environment = environment with { Scale = RenderScale };
        _environment = environment;
        _hat.SetScale(RenderScale);
        return Scene = CreateScene(environment);
    }

    public bool TryStartEarthquake(long nowMs)
    {
        float lift = _state.JumpLift;
        bool started = _running && !IsPetDragging && Location != PetLocation.LeavingLauncher && _behavior.Earthquake(nowMs, Scene?.HeadScreenPosition, _hat, _speech);
        _state.JumpLift = lift;
        if (started && Location == PetLocation.Desktop)
        {
            if (_explorer.IsAirborne)
            {
                _behavior.Reset(nowMs, _speech);
                _explorer.Fall(_state);
                _state.JumpLift = lift;
            }
            else _explorer.CancelRoute();
        }
        RefreshScene();
        return started;
    }

    public bool BeginHatDrag(Point cursorScreenPosition)
    {
        if (!_running || (Location == PetLocation.LeavingLauncher && _hat.Attached) || _state.Mode == PetMode.Earthquake)
            return false;
        _hat.BeginDrag(cursorScreenPosition);
        RefreshScene();
        return true;
    }

    public bool BeginPetDrag(Point cursorScreenPosition, long nowMs)
    {
        if (!_running || Location != PetLocation.Desktop || _state.Mode == PetMode.Earthquake || _environment is not { Ruins: not null } environment)
            return false;
        bool started = _explorer.BeginDrag(_state, environment, _speech, cursorScreenPosition, nowMs);
        if (started)
            RefreshScene();
        return started;
    }

    public void MovePet(Point cursorScreenPosition)
    {
        if (IsPetDragging && _environment is not null)
            _explorer.MoveDrag(_state, _environment, cursorScreenPosition);
    }

    public void DropPet(long nowMs)
    {
        if (IsPetDragging)
            _explorer.Drop(_state, nowMs);
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
        if (_environment is not null)
            Scene = CreateScene(_environment);
    }

    private PetScene CreateScene(PetEnvironment environment)
    {
        Point shake = _behavior.Shake;
        Rectangle bounds = GetSpriteBounds(environment, shake);
        Point? head = GetVisibleHead(environment, bounds);
        return new(_state.Mode, _state.Row, _state.Frame, bounds, head, shake, _hat.Attached, _hat.Scene, _speech.Phrase, _speech.VisibleLetters, RenderScale);
    }

    private Rectangle GetSpriteBounds(PetEnvironment environment, Point shake) =>
        PetSpriteLayout.GetBounds(PetPlacement.LogicalPosition(_state, environment), PetSpriteCatalog.GetFrameGeometry(_state.Row, _state.Frame), shake, environment.Scale);

    private Point? GetVisibleHead(PetEnvironment environment, Rectangle spriteBounds) =>
        PetSpriteLayout.VisibleHead(spriteBounds, PetSpriteCatalog.GetFrameGeometry(_state.Row, _state.Frame), environment.AreaScreenPosition, environment.VisibleScreenBounds);
}

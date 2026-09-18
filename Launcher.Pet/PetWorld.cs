using System.Drawing;
using Launcher.Pet.Sprites;
using Launcher.Pet.Behavior;
using Launcher.Pet.Data;
using Launcher.Pet.Hat;
using Launcher.Pet.Speech;

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
    public PetScene? Scene { get; private set; }

    public PetWorld(Random random)
    {
        _speech = new(random);
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
            PetPlacement.Fit(_state, environment);
            _hat.Update(elapsed, environment.Surfaces);
            _behavior.Update(nowMs, elapsed, environment, _hat, _speech, GetVisibleHead(environment, GetSpriteBounds(environment, _behavior.Shake)) is not null);
        }

        return Scene = CreateScene(environment);
    }

    public bool TryStartEarthquake(long nowMs)
    {
        bool started = _running && _behavior.Earthquake(nowMs, Scene?.HeadScreenPosition, _hat, _speech);
        RefreshScene();
        return started;
    }

    public bool BeginHatDrag(Point cursorScreenPosition)
    {
        if (!_running || _state.Mode == PetMode.Earthquake)
            return false;
        _hat.BeginDrag(cursorScreenPosition);
        RefreshScene();
        return true;
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
        return new(_state.Mode, _state.Row, _state.Frame, bounds, head, shake, _hat.Attached, _hat.Scene, _speech.Phrase, _speech.VisibleLetters);
    }

    private Rectangle GetSpriteBounds(PetEnvironment environment, Point shake) =>
        PetSpriteLayout.GetBounds(PetPlacement.LogicalPosition(_state, environment), PetSpriteCatalog.GetFrameGeometry(_state.Row, _state.Frame), shake);

    private Point? GetVisibleHead(PetEnvironment environment, Rectangle spriteBounds) =>
        PetSpriteLayout.VisibleHead(spriteBounds, PetSpriteCatalog.GetFrameGeometry(_state.Row, _state.Frame), environment.AreaScreenPosition, environment.VisibleScreenBounds);
}

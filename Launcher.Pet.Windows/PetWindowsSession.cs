using System.Runtime.InteropServices;
using Launcher.Pet.Data;
using Launcher.Pet.Hat;
using Launcher.Pet.Windows.Debug;
using Launcher.Pet.Windows.Desktop;
using Launcher.Pet.Windows.Drawing;
using Launcher.Pet.Windows.Windows;

namespace Launcher.Pet.Windows;
public sealed class PetWindowsSession : IDisposable
{
    private const int TickIntervalMs = 10;
    private const int CursorPollIntervalMs = 50;
    private readonly Form _window;
    private readonly PetArea _area;
    private readonly PetWorld _world;
    private readonly PetImages _images;
    private readonly PetDrawing _drawing;
    private readonly WindowShake _shake;
    private readonly DesktopSurfaceProvider _surfaces;
    private readonly System.Windows.Forms.Timer _timer = new()
    {
        Interval = TickIntervalMs
    };
    private readonly HatCollisionProfile _hatCollision = new(new Size(109, 64));
    private HatWindow? _hat;
    private SpeechBubbleWindow? _speech;
    private HatCollisionDebugWindow? _debug;
    private PetColors _colors = new(Color.White, Color.White, Color.Black, Color.Gray);
    private bool _running, _disposed, _updating, _showCollisions;
    private int _version;
    private long _cursorAtMs, _debugAtMs;
    private Point _cursor;
    private string? _phrase;
    public event Action<string>? Problem;
    public PetWindowsSession(Form window, PetArea area, PetWorld world)
    {
        _window = window;
        _area = area;
        _world = world;
        try
        {
            _images = new();
            _drawing = new(area, _images);
            _shake = new(window);
            _surfaces = new(() => area.GroundScreenBounds(window));
            _area.MouseDown += AreaMouseDown;
            _timer.Tick += Tick;
        }
        catch
        {
            _area.MouseDown -= AreaMouseDown;
            _timer.Tick -= Tick;
            _drawing?.Dispose();
            _images?.Dispose();
            _timer.Dispose();
            throw;
        }
    }

    public void SetColors(PetColors colors)
    {
        _colors = colors;
        _drawing.SetColors(colors);
    }

    public void ShowCollisions(bool enabled)
    {
        _showCollisions = enabled;
        if (!enabled)
            _debug?.Hide();
    }

    public void Start()
    {
        if (_running || _disposed)
            return;
        _running = true;
        _world.Start(Environment.TickCount64);
        _timer.Start();
    }

    public void Stop()
    {
        _version++;
        _running = false;
        _timer.Stop();
        _hat?.CancelDrag();
        _world.Stop(Environment.TickCount64);
        _shake.Stop();
        _speech?.Hide();
        _debug?.Hide();
        _hat?.SetInteractionEnabled(false);
    }

    public bool TryStartEarthquake()
    {
        if (!_running || _disposed)
            return false;
        _version++;
        bool started = _world.TryStartEarthquake(Environment.TickCount64);
        if (started)
            _hat?.CancelDrag();
        return started;
    }

    private void Tick(object? sender, EventArgs args)
    {
        if (!_running || _disposed || _updating || !_area.IsHandleCreated)
            return;
        _updating = true;
        try
        {
            _hat?.UpdateDrag();
            int version = _version;
            IReadOnlyList<DesktopSurface> desktop = _surfaces.GetSurfaces(_hat?.WindowHandle ?? IntPtr.Zero, _world.Scene is { HatAttached: false } || _showCollisions);
            // RU: COM может обработать закрытие или мышь. DE: COM kann Schliessen/Mauseingaben verarbeiten.
            if (!_running || _disposed || version != _version)
                return;
            long nowMs = Environment.TickCount64;
            if (nowMs >= _cursorAtMs)
            {
                _cursor = Cursor.Position;
                _cursorAtMs = nowMs + CursorPollIntervalMs;
            }

            var surfaces = desktop.Select(surface => new HatSurface($"{surface.Identity.Type}:{surface.Identity.WindowHandle}:{surface.Identity.ItemKey}", (HatSurfaceKind)surface.Type, surface.Bounds)).ToArray();
            PetScene scene = _world.Update(nowMs, _area.ReadEnvironment(_window, _cursor, surfaces));
            _drawing.Display(scene);
            _shake.Apply(scene.Mode == PetMode.Earthquake, scene.WindowShake);
            if (!_running || _disposed || version != _version)
                return;
            DisplayHat(scene, version);
            if (!_running || _disposed || version != _version)
                return;
            DisplaySpeech(scene);
            if (!_running || _disposed || version != _version)
                return;
            if (_showCollisions && nowMs >= _debugAtMs)
            {
                _debugAtMs = nowMs + 100;
                _debug ??= new();
                _debug.UpdateDebug(desktop, scene.HatAttached ? null : scene.Hat.ScreenPosition, _images.Hat.Size, _hatCollision.Segments, _hatCollision.Connectors);
            }
        }
        catch (ExternalException error)
        {
            _world.RestoreHat();
            _world.HideSpeech(Environment.TickCount64);
            DisposeHat();
            _speech?.Dispose();
            _speech = null;
            _phrase = null;
            _debug?.Dispose();
            _debug = null;
            _showCollisions = false;
            Problem?.Invoke("Anzeige des Begleiters fehlgeschlagen: " + error.Message);
        }
        finally
        {
            _updating = false;
        }
    }

    private HatWindow EnsureHat()
    {
        if (_hat is not null)
            return _hat;
        _hat = new(_images.Hat);
        _hat.DragStarted += HatDragStarted;
        _hat.DragMoved += HatDragMoved;
        _hat.Dropped += HatDropped;
        return _hat;
    }

    private void DisplayHat(PetScene scene, int version)
    {
        if (scene.HatAttached)
        {
            DisposeHat();
            return;
        }

        HatWindow hat = EnsureHat();
        hat.SetInteractionEnabled(scene.Mode != PetMode.Earthquake);
        if (scene.Hat.Mode != HatMode.Dragging)
            hat.MoveTo(scene.Hat.ScreenPosition);
        if (_running && !_disposed && version == _version)
            hat.SetAngle(scene.Hat.Angle);
    }

    private void DisplaySpeech(PetScene scene)
    {
        if (scene.Speech is null || scene.HeadScreenPosition is not Point head)
        {
            _speech?.Hide();
            return;
        }

        _speech ??= new();
        if (_phrase != scene.Speech)
        {
            _phrase = scene.Speech;
            _speech.SetPhrase(_phrase);
        }

        _speech.Display(head, scene.VisibleLetters, _colors.Bubble, _colors.Text, _colors.Border);
    }

    private void AreaMouseDown(object? sender, MouseEventArgs args)
    {
        if (!_running || args.Button != MouseButtons.Left)
            return;
        Point cursor = Cursor.Position;
        Rectangle visibleBounds = _area.VisibleScreenBounds(_window);
        if (args.Clicks >= 2 && _drawing.IsPetAtScreen(cursor, visibleBounds))
        {
            TryStartEarthquake();
            return;
        }

        if (_world.Scene is not { HatAttached: true } || !_drawing.IsHeadAtScreen(cursor, visibleBounds))
            return;
        try
        {
            EnsureHat().BeginDrag(cursor);
        }
        catch (ExternalException error)
        {
            _world.RestoreHat();
            DisposeHat();
            Problem?.Invoke(error.Message);
        }
    }

    private void HatDragStarted()
    {
        _version++;
        if (!_world.BeginHatDrag(Cursor.Position))
            _hat?.CancelDrag();
    }

    private void HatDragMoved(Point position)
    {
        _version++;
        _world.MoveHat(position);
    }

    private void HatDropped(Point position)
    {
        _version++;
        _world.MoveHat(position);
        _world.DropHat(_drawing.IsHeadAtScreen(position, _area.VisibleScreenBounds(_window)));
    }

    private void DisposeHat()
    {
        if (_hat is null)
            return;
        _hat.DragStarted -= HatDragStarted;
        _hat.DragMoved -= HatDragMoved;
        _hat.Dropped -= HatDropped;
        _hat.Dispose();
        _hat = null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        Stop();
        _disposed = true;
        _timer.Tick -= Tick;
        _timer.Dispose();
        _area.MouseDown -= AreaMouseDown;
        DisposeHat();
        _speech?.Dispose();
        _debug?.Dispose();
        _drawing.Dispose();
        _images.Dispose();
    }
}

using System.Runtime.InteropServices;
using Launcher.Pet.Data;
using Launcher.Pet.Exploration;
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
    private readonly DesktopWallpaperSession _wallpaper;
    private readonly System.Windows.Forms.Timer _timer = new()
    {
        Interval = TickIntervalMs
    };
    private HatFrameCache? _hatFrames;
    private HatWindow? _hat;
    private PetWindow? _desktopPet;
    private RuinsWindow? _ruinsWindow;
    private RuinBuilder? _ruinBuilder;
    private RuinScene? _ruins;
    private RuinScene? _plainDesktop;
    private IReadOnlyList<DesktopSurface> _lastDesktop = Array.Empty<DesktopSurface>();
    private IReadOnlyList<DesktopSurface>? _desktopSnapshot;
    private long? _awakeningAt;
    private Point _awakeningPoint;
    private string? _ruinsMonitor;
    private string? _monitor;
    private Rectangle _desktopBounds;
    private Point _lastPetScreenPosition;
    private SpeechBubbleWindow? _speech;
    private HatCollisionDebugWindow? _debug;
    private PetColors _colors = new(Color.White, Color.White, Color.Black, Color.Gray);
    private bool _running, _disposed, _updating, _showCollisions, _ruinsActive;
    private int _version;
    private long _cursorAtMs, _debugAtMs;
    private Point _cursor;
    private string? _phrase;
    public event Action<string>? Problem;
    public PetWindowsSession(Form window, PetArea area, PetWorld world, DesktopWallpaperSession wallpaper)
    {
        _window = window;
        _area = area;
        _world = world;
        _wallpaper = wallpaper;
        try
        {
            _images = new();
            _drawing = new(area, _images);
            _shake = new(window);
            _surfaces = new(() => Outside ? null : area.GroundScreenBounds(window));
            _area.MouseDown += AreaMouseDown;
            _window.Resize += WindowResize;
            _timer.Tick += Tick;
        }
        catch
        {
            _area.MouseDown -= AreaMouseDown;
            _window.Resize -= WindowResize;
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

    public void SetHatFade(byte fade)
    {
        if (!_disposed)
            _world.SetHatFade(fade);
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
        _desktopPet?.Hide();
        _ruinsWindow?.Hide();
        _ruinsActive = false;
        if (_wallpaper.Restore() is string wallpaperError) Problem?.Invoke(wallpaperError);
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

    private bool Outside => _world.Location != PetLocation.Launcher;

    private void WindowResize(object? sender, EventArgs args)
    {
        if (!_running || _disposed)
            return;
        if (_window.WindowState != FormWindowState.Minimized)
        {
            if (_ruinsActive) DeactivateRuins();
            return;
        }
        if (!_ruinsActive)
        {
            if (!Outside && !_world.LeaveLauncher(Environment.TickCount64)) return;
            ActivateRuins();
        }
        _version++;
        _monitor = Screen.FromPoint(_lastPetScreenPosition).DeviceName;
        _drawing.Enabled = false;
        _area.Invalidate();
        _shake.Stop();
        _speech?.Hide();
    }

    private void ActivateRuins()
    {
        _ruinsActive = true;
        _desktopSnapshot = _lastDesktop.ToArray();
        _ruinBuilder = null;
        _ruins = null;
        _ruinsMonitor = null;
        _awakeningAt = _world.HasLanded ? Environment.TickCount64 : null;
        if (_wallpaper.Activate() is string error) Problem?.Invoke(error);
    }

    private void DeactivateRuins()
    {
        _version++;
        _ruinsActive = false;
        _ruinsWindow?.Hide();
        _ruinBuilder = null;
        _ruins = null;
        _desktopSnapshot = null;
        _awakeningAt = null;
        if (_wallpaper.Restore() is string error) Problem?.Invoke(error);
    }

    private PetEnvironment ReadEnvironment(IReadOnlyList<HatSurface> surfaces)
    {
        if (!Outside)
            return _area.ReadEnvironment(_window, _cursor, surfaces);
        Screen screen = Screen.AllScreens.FirstOrDefault(item => item.DeviceName == _monitor)
            ?? Screen.PrimaryScreen ?? Screen.AllScreens[0];
        _monitor = screen.DeviceName;
        _desktopBounds = screen.WorkingArea;
        if (_ruinsActive && _ruinBuilder is null)
        {
            Rectangle Local(Rectangle bounds)
            {
                if (bounds.IsEmpty) return Rectangle.Empty;
                bounds.Offset(-_desktopBounds.X, -_desktopBounds.Y);
                return bounds;
            }
            var seed = (_desktopSnapshot ?? _lastDesktop)
                .Where(s => s.Type is DesktopSurfaceType.Window or DesktopSurfaceType.DesktopIcon && s.Identity.WindowHandle != _window.Handle)
                .Where(s => _desktopBounds.IntersectsWith(s.Bounds))
                .Select(s => new DesktopSeed(Local(s.VisualBounds.IsEmpty ? s.Bounds : s.VisualBounds), Local(s.LabelBounds), s.Type == DesktopSurfaceType.DesktopIcon));
            _ruinBuilder = new(_desktopBounds.Size, seed);
            _awakeningPoint = new(Math.Clamp(_lastPetScreenPosition.X - _desktopBounds.X, 0, _desktopBounds.Width), _desktopBounds.Height);
        }
        if (_ruinsActive && (_ruins is null || _ruins.Size != _desktopBounds.Size || _ruinsMonitor != screen.DeviceName))
        {
            _ruins = _ruinBuilder!.Build(_desktopBounds.Size, _awakeningPoint);
            _ruinsMonitor = screen.DeviceName;
        }
        if (_plainDesktop is null || _plainDesktop.Size != _desktopBounds.Size)
            _plainDesktop = new(_desktopBounds.Size, new[] { new RuinPlatform("ruin:floor", 0, _desktopBounds.Width, _desktopBounds.Height, 0) },
                Array.Empty<RuinLink>(), Array.Empty<RuinBridge>(), Array.Empty<RuinDecoration>(), 0);
        RuinScene activeScene = _ruinsActive ? _ruins! : _plainDesktop;
        float seconds = _ruinsActive && _awakeningAt is long started ? Math.Max(0, Environment.TickCount64 - started) / 1000f : 0;
        var combined = ComposeHatSurfaces(surfaces, activeScene, _desktopBounds.Location, seconds, _world.HasLanded);
        return new(_desktopBounds.Location, _desktopBounds.Width,
            _desktopBounds.Height - PetLogicalGeometry.Height, _desktopBounds.Width,
            screen.Bounds, _world.IsHatDragging, _cursor,
            Array.Empty<Rectangle>(), combined, "ruin:floor", _world.RenderScale, activeScene, seconds);
    }

    private static IReadOnlyList<HatSurface> ComposeHatSurfaces(IReadOnlyList<HatSurface> desktop, RuinScene scene, Point origin, float seconds, bool landed)
    {
        var platforms = scene.Platforms.Where(p => p.Id == "ruin:floor" || landed && seconds >= p.RevealAt + 0.85f)
            .Select(p => new HatSurface(p.Id, HatSurfaceKind.Ruin, new Rectangle(origin.X + (int)p.Left, origin.Y + (int)p.Y, (int)(p.Right - p.Left), 1))).ToArray();
        var bridges = scene.Bridges.Where(b => landed && seconds >= b.RevealAt + 0.85f)
            .Select(b => new HatSurface(b.Id, HatSurfaceKind.Ruin, new Rectangle(origin.X + (int)b.Left, origin.Y + (int)b.Y, (int)(b.Right - b.Left), 1)));
        var result = new List<HatSurface>(platforms);
        result.AddRange(bridges);
        foreach (var surface in desktop)
        {
            var pieces = new List<Rectangle> { surface.Bounds };
            foreach (var platform in result.Where(p => p.Kind == HatSurfaceKind.Ruin && Math.Abs(p.Bounds.Top - surface.Bounds.Top) <= 1))
            {
                var next = new List<Rectangle>();
                foreach (var piece in pieces)
                {
                    if (platform.Bounds.Left >= piece.Right || platform.Bounds.Right <= piece.Left) { next.Add(piece); continue; }
                    if (piece.Left < platform.Bounds.Left) next.Add(Rectangle.FromLTRB(piece.Left, piece.Top, platform.Bounds.Left, piece.Bottom));
                    if (piece.Right > platform.Bounds.Right) next.Add(Rectangle.FromLTRB(platform.Bounds.Right, piece.Top, piece.Right, piece.Bottom));
                }
                pieces = next;
            }
            foreach (var piece in pieces)
                result.Add(surface with { Bounds = piece, Identity = piece == surface.Bounds ? surface.Identity : $"{surface.Identity}:{piece.Left}:{piece.Right}" });
        }
        return result;
    }

    private bool IsHeadAtScreen(Point point) => Outside
        ? _desktopPet?.IsHeadAtScreen(point) == true
        : _drawing.IsHeadAtScreen(point, _area.VisibleScreenBounds(_window));

    private void Tick(object? sender, EventArgs args)
    {
        if (!_running || _disposed || _updating || !_area.IsHandleCreated)
            return;
        _updating = true;
        try
        {
            _hat?.UpdateDrag();
            if (_world.IsPetDragging)
            {
                _world.MovePet(Cursor.Position);
                if ((HatMouseApi.GetAsyncKeyState(0x01) & 0x8000) == 0)
                    _world.DropPet(Environment.TickCount64);
            }
            int version = _version;
            IReadOnlyList<DesktopSurface> desktop = _surfaces.GetSurfaces(_hat?.WindowHandle ?? IntPtr.Zero, true);
            // RU: COM может обработать закрытие или мышь. DE: COM kann Schliessen/Mauseingaben verarbeiten.
            if (!_running || _disposed || version != _version)
                return;
            _lastDesktop = desktop;
            long nowMs = Environment.TickCount64;
            if (nowMs >= _cursorAtMs)
            {
                _cursor = Cursor.Position;
                _cursorAtMs = nowMs + CursorPollIntervalMs;
            }

            var surfaces = desktop.Select(surface => new HatSurface($"{surface.Identity.Type}:{surface.Identity.WindowHandle}:{surface.Identity.ItemKey}", (HatSurfaceKind)surface.Type, surface.Bounds)).ToArray();
            PetEnvironment environment = ReadEnvironment(surfaces);
            PetScene scene = _world.Update(nowMs, environment);
            if (Outside)
            {
                if (_ruinsActive && _world.HasLanded && _awakeningAt is null)
                {
                    _awakeningAt = nowMs;
                    _awakeningPoint = new(scene.SpriteBounds.Left + scene.SpriteBounds.Width / 2, _desktopBounds.Height);
                }
                if (_ruinsActive && _awakeningAt is long start && _ruins is not null)
                {
                    _ruinsWindow ??= new();
                    if (_ruinsWindow.Display(_ruins, _desktopBounds.Location, _awakeningPoint, (nowMs - start) / 1000f, nowMs) && !_showCollisions)
                        _ruinsWindow.RaiseWithoutActivation();
                }
                if (_desktopPet is null)
                {
                    _desktopPet = new(_images);
                    _desktopPet.MouseDown += AreaMouseDown;
                }
                _desktopPet.Display(scene, environment.AreaScreenPosition);
                _lastPetScreenPosition = new(environment.AreaScreenPosition.X + scene.SpriteBounds.X + scene.SpriteBounds.Width / 2,
                    environment.AreaScreenPosition.Y + scene.SpriteBounds.Y + scene.SpriteBounds.Height / 2);
            }
            else
            {
                _lastPetScreenPosition = new(environment.AreaScreenPosition.X + scene.SpriteBounds.X + scene.SpriteBounds.Width / 2,
                    environment.AreaScreenPosition.Y + scene.SpriteBounds.Y + scene.SpriteBounds.Height / 2);
                _drawing.Display(scene);
            }
            _shake.Apply(!Outside && scene.Mode == PetMode.Earthquake, scene.WindowShake);
            if (!_running || _disposed || version != _version)
                return;
            DisplayHat(scene, version);
            if (!_running || _disposed || version != _version)
                return;
            DisplaySpeech(scene);
            if (Outside && !_showCollisions)
            {
                _desktopPet?.RaiseWithoutActivation();
                _hat?.RaiseWithoutActivation();
                _speech?.RaiseWithoutActivation();
            }
            if (!_running || _disposed || version != _version)
                return;
            if (_showCollisions && nowMs >= _debugAtMs)
            {
                _debugAtMs = nowMs + 100;
                _debug ??= new();
                var hatSize = new Size((int)Math.Round(HatGeometry.Width * scene.Hat.Scale), (int)Math.Round(HatGeometry.Height * scene.Hat.Scale));
                var collision = new HatCollisionProfile(hatSize);
                var debugSurfaces = environment.Surfaces.Select(s => new DesktopSurface(s.Bounds, new DesktopSurfaceIdentity((DesktopSurfaceType)s.Kind, IntPtr.Zero, s.Identity))).ToArray();
                _debug.UpdateDebug(debugSurfaces, scene.HatAttached ? null : scene.Hat.ScreenPosition, hatSize,
                    collision.Segments, collision.Connectors, environment.Ruins, environment.AreaScreenPosition,
                    _world.PlannedRoute, _world.PlannedRouteStart, _world.PlannedHatTarget);
            }
        }
        catch (Exception error) when (error is ExternalException or System.ComponentModel.Win32Exception)
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
        _hatFrames ??= new(_images.Hat, _images.HatFalling);
        _hat = new(_hatFrames);
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
        hat.SetScale(scene.Hat.Scale);
        hat.SetInteractionEnabled(scene.Mode != PetMode.Earthquake);
        if (scene.Hat.Mode != HatMode.Dragging)
            hat.MoveTo(scene.Hat.ScreenPosition);
        if (_running && !_disposed && version == _version)
            hat.DisplayPose(scene.Hat.Pose);
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
        if (!_running || (Outside && ReferenceEquals(sender, _area)) || _world.Location == PetLocation.LeavingLauncher || args.Button != MouseButtons.Left)
            return;
        Point cursor = Cursor.Position;
        Rectangle visibleBounds = _area.VisibleScreenBounds(_window);
        if (args.Clicks >= 2 && (Outside || _drawing.IsPetAtScreen(cursor, visibleBounds)))
        {
            TryStartEarthquake();
            return;
        }

        if (Outside && _desktopPet?.IsBodyAtScreen(cursor) == true)
        {
            if (_world.BeginPetDrag(cursor, Environment.TickCount64))
            {
                _version++;
                _speech?.Hide();
            }
            return;
        }

        if (_world.Scene is not { HatAttached: true } || !IsHeadAtScreen(cursor))
            return;
        try
        {
            HatWindow hat = EnsureHat();
            hat.SetScale(_world.RenderScale);
            hat.BeginDrag(cursor);
        }
        catch (Exception error) when (error is ExternalException or System.ComponentModel.Win32Exception)
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
        _world.DropHat(IsHeadAtScreen(position));
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
        _window.Resize -= WindowResize;
        DisposeHat();
        _speech?.Dispose();
        _debug?.Dispose();
        if (_desktopPet is not null)
        {
            _desktopPet.MouseDown -= AreaMouseDown;
            _desktopPet.Dispose();
        }
        _ruinsWindow?.Dispose();
        _drawing.Dispose();
        _hatFrames?.Dispose();
        _images.Dispose();
    }
}

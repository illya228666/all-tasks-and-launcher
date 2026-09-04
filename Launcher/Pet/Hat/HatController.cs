using System.Drawing;
using System.Runtime.InteropServices;
using Launcher.Pet.Hat.Debug;
using Launcher.Pet.UI;

namespace Launcher.Pet.Hat;

internal sealed class HatController : IDisposable
{
    private readonly HatState _state = new();
    private readonly HatPhysics _physics = new();
    private readonly DesktopSurfaceProvider _surfaceProvider = new();
    private readonly System.Windows.Forms.Timer _updateTimer = new() { Interval = HatTiming.RuntimeTickIntervalMs };
    private readonly Func<Point, bool> _isHeadAtScreenPoint;
    private readonly Action<bool> _setHatAttached;
    private readonly Action<string> _hintRequested;
    private readonly Bitmap _sprite;
    private readonly HatCollisionProfile _collisionProfile;
    private readonly HatCollisionDebugController _collisionDebug;
    private HatWindow? _window;
    private long _lastTick;
    private bool _running;
    private bool _disposed;
    private bool _updating;
    private int _stateVersion;

    internal HatController(
        Func<Point, bool> isHeadAtScreenPoint,
        Action<bool> setHatAttached,
        Action<string> hintRequested)
    {
        _isHeadAtScreenPoint = isHeadAtScreenPoint;
        _setHatAttached = setHatAttached;
        _hintRequested = hintRequested;
        using var sprite = new Bitmap(Path.Combine(AppContext.BaseDirectory, "Resources", "hat_small.png"));
        if (sprite.Width != 109 || sprite.Height != 64)
            throw new InvalidDataException($"Unerwartete Hutgroesse: {sprite.Width}x{sprite.Height}.");
        _sprite = new Bitmap(sprite);
        _collisionProfile = new HatCollisionProfile(_sprite.Size);
        _collisionDebug = new HatCollisionDebugController(
            _surfaceProvider,
            _collisionProfile,
            () => _window);
        _updateTimer.Tick += UpdateTimer_Tick;
    }

    internal void SetCollisionDebug(bool enabled) => _collisionDebug.SetEnabled(enabled);

    internal void Start()
    {
        if (_disposed)
            return;
        _running = true;
        _collisionDebug.Start();
        _lastTick = Environment.TickCount64;
        UpdateActivity();
    }

    internal void Stop()
    {
        _stateVersion++;
        _running = false;
        _collisionDebug.Stop();
        _window?.CancelDrag();
        if (_state.Mode == HatMode.Dragging)
            BeginFalling();
        UpdateActivity();
    }

    private void UpdateActivity()
    {
        _window?.SetInteractionEnabled(_running && !_disposed);
        _updateTimer.Enabled = _running && !_disposed && _window is not null
            && _state.Mode is HatMode.Dragging or HatMode.Falling or HatMode.Resting;
    }

    internal void DetachAndBeginDrag(Point cursorPosition)
    {
        if (_disposed || !_running || _state.Mode != HatMode.Attached)
            return;

        try
        {
            _window = new HatWindow(_sprite);
            _window.DragStarted += Window_DragStarted;
            _window.Dropped += Window_Dropped;
            _window.BeginDrag(cursorPosition);
        }
        catch (ExternalException exception)
        {
            AttachToPet();
            _hintRequested($"Der Hut konnte nicht abgenommen werden: {exception.Message}");
        }
    }

    private void Window_DragStarted()
    {
        _stateVersion++;
        _state.Mode = HatMode.Dragging;
        _state.Support = null;
        _state.ResolveInitialOverlap = false;
        _state.VelocityY = 0f;
        _state.Angle = 0f;
        _setHatAttached(false);
        UpdateActivity();
    }

    private void Window_Dropped(Point screenPoint)
    {
        if (_window is null)
            return;
        if (_isHeadAtScreenPoint(screenPoint))
        {
            AttachToPet();
            return;
        }
        BeginFalling();
    }

    private void BeginFalling()
    {
        if (_window is null)
            return;

        _stateVersion++;
        _state.Mode = HatMode.Falling;
        _state.Position = _window.Location;
        _state.VelocityY = 0f;
        _state.Angle = 0f;
        _state.FallTimeSeconds = 0f;
        _state.Support = null;
        _state.ResolveInitialOverlap = true;
        _lastTick = Environment.TickCount64;
        UpdateActivity();
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        if (!_running || _disposed || _window is null || _updating)
            return;

        _updating = true;
        try
        {
            if (_state.Mode == HatMode.Dragging)
                UpdateDragging();
            else if (_state.Mode == HatMode.Falling)
                UpdateFalling();
            else if (_state.Mode == HatMode.Resting)
                UpdateResting();
            else
                UpdateActivity();
        }
        catch (ExternalException exception)
        {
            AttachToPet();
            _hintRequested($"Der Hut konnte nicht angezeigt werden: {exception.Message}");
        }
        finally
        {
            _updating = false;
        }
    }

    private void UpdateDragging() => _window!.UpdateDrag();

    private void UpdateFalling()
    {
        long now = Environment.TickCount64;
        float elapsedSeconds = (now - _lastTick) / 1000f;
        _lastTick = now;
        Size size = _window!.ClientSize;
        RectangleF previousBounds = new(_state.Position, size);
        _physics.Advance(_state, elapsedSeconds);
        RectangleF currentBounds = new(_state.Position, size);
        int version = _stateVersion;
        IReadOnlyList<DesktopSurface> surfaces = _surfaceProvider.GetSurfaces(_window.WindowHandle);
        // Shell COM может пропустить drag/stop/dispose через цикл сообщений.
        // Результат старого tick не должен отменять более новый переход.
        if (!_running || _disposed || version != _stateVersion)
            return;
        HatCollision? collision = _collisionProfile.FindFirstCollision(
            surfaces,
            previousBounds,
            currentBounds,
            _state.ResolveInitialOverlap);
        _state.ResolveInitialOverlap = false;
        if (collision is not null)
        {
            LandOn(collision.Value);
            return;
        }
        ApplyVisualState();
    }

    private void LandOn(HatCollision collision)
    {
        _stateVersion++;
        DesktopSurface surface = collision.Surface;
        _state.Position = new(
            _state.Position.X,
            surface.Bounds.Top - collision.ContactY);
        _state.VelocityY = 0f;
        _state.Support = new HatSupport(
            surface.Identity, _state.Position.X - surface.Bounds.Left, collision.Segment);
        _state.Mode = HatMode.Resting;
        ApplyVisualState();
        UpdateActivity();
    }

    private void UpdateResting()
    {
        if (_state.Support is not HatSupport support)
        {
            BeginFalling();
            return;
        }

        int version = _stateVersion;
        IReadOnlyList<DesktopSurface> surfaces = _surfaceProvider.GetSurfaces(_window!.WindowHandle);
        if (!_running || _disposed || version != _stateVersion)
            return;

        RectangleF restingBounds = new(_state.Position, _window.ClientSize);
        HatCollision? takeover = _collisionProfile.FindFirstCollision(
            surfaces.Where(surface => surface.Identity != support.Identity),
            restingBounds,
            restingBounds,
            resolveInitialOverlap: true);
        if (takeover is not null)
        {
            LandOn(takeover.Value);
            return;
        }

        bool valid = _surfaceProvider.TryRefresh(
            support.Identity,
            _window.WindowHandle,
            out DesktopSurface surface);
        if (!_running || _disposed || version != _stateVersion)
            return;
        if (!valid)
        {
            BeginFalling();
            return;
        }

        float newX = surface.Bounds.Left + support.RelativeX;
        if (!HatCollisionProfile.HorizontallyOverlaps(surface.Bounds, newX, support.Segment))
        {
            BeginFalling();
            return;
        }

        _state.Position = new(
            newX,
            surface.Bounds.Top - support.Segment.ContactY);
        ApplyVisualState();
    }

    private void ApplyVisualState()
    {
        _window!.MoveTo(new Point(
            (int)Math.Round(_state.Position.X),
            (int)Math.Round(_state.Position.Y)));
        _window.SetAngle(_state.Angle);
    }

    private void AttachToPet()
    {
        _stateVersion++;
        DisposeWindow();
        _state.Mode = HatMode.Attached;
        _state.Support = null;
        _state.ResolveInitialOverlap = false;
        _state.VelocityY = 0f;
        _state.Angle = 0f;
        _setHatAttached(true);
        UpdateActivity();
    }

    private void DisposeWindow()
    {
        if (_window is null)
            return;
        _window.DragStarted -= Window_DragStarted;
        _window.Dropped -= Window_Dropped;
        _window.Dispose();
        _window = null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Stop();
        _updateTimer.Tick -= UpdateTimer_Tick;
        _updateTimer.Dispose();
        _collisionDebug.Dispose();
        _state.Support = null;
        DisposeWindow();
        _sprite.Dispose();
    }
}

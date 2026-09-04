using System.Drawing;
using System.Runtime.InteropServices;
using Launcher.Pet.UI;

namespace Launcher.Pet.Hat;

internal sealed class HatController : IDisposable
{
    private const int UpdateIntervalMs = 16;

    private readonly HatState _state = new();
    private readonly HatPhysics _physics = new();
    private readonly DesktopSurfaceProvider _surfaceProvider = new();
    private readonly System.Windows.Forms.Timer _updateTimer = new() { Interval = UpdateIntervalMs };
    private readonly Func<Point, bool> _isHeadAtScreenPoint;
    private readonly Action<bool> _setHatAttached;
    private readonly Action<string> _hintRequested;
    private readonly Bitmap _sprite;
    private readonly HatCollisionProfile _collisionProfile;
    private HatWindow? _window;
    private long _lastTick;
    private bool _running;
    private bool _disposed;

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
        _updateTimer.Tick += UpdateTimer_Tick;
    }

    internal void Start()
    {
        if (_disposed)
            return;
        _running = true;
        if (_state.Mode is HatMode.Falling or HatMode.RestingOnWindow)
        {
            _lastTick = Environment.TickCount64;
            _updateTimer.Start();
        }
    }

    internal void Stop()
    {
        _running = false;
        _updateTimer.Stop();
    }

    internal void DetachAndBeginDrag(Point cursorPosition)
    {
        if (_disposed || _state.Mode != HatMode.Attached)
            return;

        try
        {
            _window = new HatWindow(_sprite);
            _window.DragStarted += Window_DragStarted;
            _window.Dropped += Window_Dropped;
            _window.BeginDrag(cursorPosition);
            _state.Mode = HatMode.Dragging;
            _setHatAttached(false);
        }
        catch (ExternalException exception)
        {
            DisposeWindow();
            _state.Mode = HatMode.Attached;
            _setHatAttached(true);
            _hintRequested($"Der Hut konnte nicht abgenommen werden: {exception.Message}");
        }
    }

    private void Window_DragStarted()
    {
        _updateTimer.Stop();
        _state.Mode = HatMode.Dragging;
        _state.RestingWindowHandle = IntPtr.Zero;
        _state.VelocityY = 0f;
        _state.Angle = 0f;
        _setHatAttached(false);
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

        _state.Mode = HatMode.Falling;
        _state.Position = _window.Location;
        _state.VelocityY = 0f;
        _state.Angle = 0f;
        _state.FallTimeSeconds = 0f;
        _state.RestingWindowHandle = IntPtr.Zero;
        _lastTick = Environment.TickCount64;
        _window.SetAngle(0f);

        DesktopSurface taskbar = _surfaceProvider.GetTaskbarSurface(new Point(
            _window.Left + _window.Width / 2,
            _window.Top + _window.Height / 2));
        float? taskbarContactY = _collisionProfile.GetSupportContactY(taskbar.Bounds, _state.Position.X);
        if (taskbarContactY.HasValue
            && _state.Position.Y + taskbarContactY.Value >= taskbar.Bounds.Top)
        {
            LandOn(new HatCollision(taskbar, taskbarContactY.Value));
            return;
        }

        if (_running)
            _updateTimer.Start();
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        if (_window is null)
            return;

        try
        {
            if (_state.Mode == HatMode.Falling)
                UpdateFalling();
            else if (_state.Mode == HatMode.RestingOnWindow)
                UpdateRestingWindow();
            else
                _updateTimer.Stop();
        }
        catch (ExternalException exception)
        {
            AttachToPet();
            _hintRequested($"Der Hut konnte nicht angezeigt werden: {exception.Message}");
        }
    }

    private void UpdateFalling()
    {
        long now = Environment.TickCount64;
        float elapsedSeconds = (now - _lastTick) / 1000f;
        _lastTick = now;
        Size size = _window!.ClientSize;
        RectangleF previousBounds = new(_state.Position, size);
        _physics.Advance(_state, elapsedSeconds);
        RectangleF currentBounds = new(_state.Position, size);
        HatCollision? collision = _surfaceProvider.FindCollision(
            previousBounds,
            currentBounds,
            _window.WindowHandle,
            _collisionProfile);
        if (collision is not null)
        {
            LandOn(collision.Value);
            return;
        }
        ApplyVisualState();
    }

    private void LandOn(HatCollision collision)
    {
        DesktopSurface surface = collision.Surface;
        _state.Position = new(
            _state.Position.X,
            surface.Bounds.Top - collision.ContactY);
        _state.VelocityY = 0f;
        _state.RestingWindowHandle = surface.WindowHandle;
        _state.RelativeX = _state.Position.X - surface.Bounds.Left;
        _state.Mode = surface.Type switch
        {
            DesktopSurfaceType.Window => HatMode.RestingOnWindow,
            DesktopSurfaceType.DesktopIcon => HatMode.RestingOnDesktopIcon,
            _ => HatMode.RestingOnTaskbar
        };
        ApplyVisualState();

        if (_state.Mode == HatMode.RestingOnWindow && _running)
            _updateTimer.Start();
        else
            _updateTimer.Stop();
    }

    private void UpdateRestingWindow()
    {
        if (!_surfaceProvider.TryGetWindowSurface(_state.RestingWindowHandle, out DesktopSurface surface))
        {
            BeginFalling();
            return;
        }

        float newX = surface.Bounds.Left + _state.RelativeX;
        float? contactY = _collisionProfile.GetSupportContactY(surface.Bounds, newX);
        if (!contactY.HasValue)
        {
            BeginFalling();
            return;
        }

        _state.Position = new(
            newX,
            surface.Bounds.Top - contactY.Value);
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
        _updateTimer.Stop();
        DisposeWindow();
        _state.Mode = HatMode.Attached;
        _state.RestingWindowHandle = IntPtr.Zero;
        _state.VelocityY = 0f;
        _state.Angle = 0f;
        _setHatAttached(true);
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
        DisposeWindow();
        _sprite.Dispose();
    }
}

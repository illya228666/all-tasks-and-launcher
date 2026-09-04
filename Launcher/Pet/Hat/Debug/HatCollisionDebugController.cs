using Launcher.Pet.UI;

namespace Launcher.Pet.Hat.Debug;

internal sealed class HatCollisionDebugController : IDisposable
{
    private const int RefreshIntervalMs = 100;

    private readonly DesktopSurfaceProvider _surfaceProvider;
    private readonly HatCollisionProfile _collisionProfile;
    private readonly Func<HatWindow?> _getHatWindow;
    private readonly System.Windows.Forms.Timer _refreshTimer = new() { Interval = RefreshIntervalMs };
    private HatCollisionDebugWindow? _window;
    private bool _enabled;
    private bool _running;
    private bool _disposed;

    internal HatCollisionDebugController(
        DesktopSurfaceProvider surfaceProvider,
        HatCollisionProfile collisionProfile,
        Func<HatWindow?> getHatWindow)
    {
        _surfaceProvider = surfaceProvider;
        _collisionProfile = collisionProfile;
        _getHatWindow = getHatWindow;
        _refreshTimer.Tick += RefreshTimer_Tick;
    }

    internal void SetEnabled(bool enabled)
    {
        if (_disposed || _enabled == enabled)
            return;

        _enabled = enabled;
        UpdateActivity();
    }

    internal void Start()
    {
        if (_disposed)
            return;

        _running = true;
        UpdateActivity();
    }

    internal void Stop()
    {
        _running = false;
        _refreshTimer.Stop();
        _window?.Hide();
    }

    private void UpdateActivity()
    {
        if (!_enabled || !_running)
        {
            _refreshTimer.Stop();
            _window?.Hide();
            return;
        }

        _window ??= new HatCollisionDebugWindow();
        Refresh();
        _refreshTimer.Start();
    }

    private void RefreshTimer_Tick(object? sender, EventArgs e) => Refresh();

    private void Refresh()
    {
        if (!_enabled || !_running)
            return;

        HatWindow? hatWindow = _getHatWindow();
        IntPtr excludedWindow = hatWindow is { Visible: true }
            ? hatWindow.WindowHandle
            : IntPtr.Zero;
        Point? hatLocation = hatWindow is { Visible: true }
            ? hatWindow.Location
            : null;
        Size hatSize = hatWindow is { Visible: true }
            ? hatWindow.ClientSize
            : Size.Empty;

        IReadOnlyList<DesktopSurface> surfaces =
            _surfaceProvider.GetSurfaces(excludedWindow);

        _window!.UpdateDebug(
            surfaces,
            hatLocation,
            hatSize,
            _collisionProfile.Segments);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _refreshTimer.Stop();
        _refreshTimer.Tick -= RefreshTimer_Tick;
        _refreshTimer.Dispose();
        _window?.Dispose();
        _window = null;
    }
}

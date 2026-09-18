namespace Launcher.Pet.Windows.Windows;
internal sealed class WindowShake
{
    private readonly Form _window;
    private Point? _origin;
    private Point _lastLocation;
    private bool _active;
    internal WindowShake(Form window) => _window = window;
    internal void Apply(bool active, Point offset)
    {
        if (!active)
        {
            Stop();
            return;
        }

        if (!_active)
        {
            _active = true;
            _origin = _window.WindowState == FormWindowState.Normal ? _window.Location : null;
            _lastLocation = _window.Location;
        }

        if (_origin is not Point origin)
            return;
        if (_window.WindowState != FormWindowState.Normal || _window.Location != _lastLocation)
        {
            _origin = null;
            return;
        }

        _lastLocation = new(origin.X + offset.X, origin.Y + offset.Y);
        _window.Location = _lastLocation;
    }

    internal void Stop()
    {
        Point? origin = _origin;
        _origin = null;
        _active = false;
        if (origin is Point point && !_window.IsDisposed && !_window.Disposing && _window.WindowState == FormWindowState.Normal && _window.Location == _lastLocation)
            _window.Location = point;
    }
}

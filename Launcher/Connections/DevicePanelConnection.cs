using Launcher.Device.Connection;
using Launcher.Device.Data;
using Launcher.Windows;

namespace Launcher.Connections;
internal sealed class DevicePanelConnection : IDisposable
{
    private readonly DeviceConnection _device;
    private readonly MainWindow _window;
    private readonly Action<Action> _post;
    internal DevicePanelConnection(DeviceConnection device, MainWindow window, Action<Action> post)
    {
        _device = device;
        _window = window;
        _post = post;
        device.StateChanged += StateChanged;
        device.IndicatorCompleted += IndicatorCompleted;
        device.Failed += Failed;
        window.Device.IndicatorRequested += SetIndicator;
    }

    private void SetIndicator(bool enabled)
    {
        if (!_device.SetIndicator(enabled))
            _window.Status.ShowHint("Controller wird geschlossen.");
    }

    private void StateChanged(DeviceState state) =>
        _post(() => _window.Device.Display(state.IsConnected, state.PortName, state.ProtocolVersion));

    private void IndicatorCompleted(bool enabled, bool available) =>
        _post(() => _window.Status.ShowHint(!available ? "Controller nicht erreichbar." : enabled ? "LED eingeschaltet." : "LED ausgeschaltet."));

    private void Failed(string message) => _post(() => _window.Status.ShowHint("Controller: " + message));

    public void Dispose()
    {
        _device.StateChanged -= StateChanged;
        _device.IndicatorCompleted -= IndicatorCompleted;
        _device.Failed -= Failed;
        _window.Device.IndicatorRequested -= SetIndicator;
    }
}

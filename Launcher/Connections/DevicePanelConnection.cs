using Launcher.Device.Connection;
using Launcher.Device.Data;
using Launcher.Windows;
namespace Launcher.Connections;

internal sealed class DevicePanelConnection : IDisposable
{
    private readonly DeviceConnection _device;
    private readonly MainWindow _window;
    private readonly Action<Action> _post;
    private bool _disposed;
    internal DevicePanelConnection(DeviceConnection device, MainWindow window, Action<Action> post)
    {
        _device = device;
        _window = window;
        _post = post;
        device.StateChanged += StateChanged;
        device.InputReceived += InputReceived;
        device.FadeReceived += FadeReceived;
        window.Device.IndicatorRequested += SetIndicator;
        window.Device.Display(device.State);
    }
    private async void SetIndicator(bool enabled)
    {
        DeviceCommandResult result = await _device.SetIndicatorAsync(enabled);
        if (!_disposed && !_window.IsDisposed)
            _window.Status.ShowHint(DevicePresentation.CommandResult(result));
    }
    private void StateChanged(DeviceState state) => _post(() =>
    {
        if (_disposed) return;
        _window.Device.Display(state);
    });
    private void InputReceived(DeviceInput input) => _post(() =>
    {
        if (!_disposed) _window.Device.DisplayInput(input);
    });

    private void FadeReceived(byte fade) => _post(() =>
    {
        if (!_disposed) _window.Device.DisplayFade(fade);
    });
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _device.StateChanged -= StateChanged;
        _device.InputReceived -= InputReceived;
        _device.FadeReceived -= FadeReceived;
        _window.Device.IndicatorRequested -= SetIndicator;
    }
}

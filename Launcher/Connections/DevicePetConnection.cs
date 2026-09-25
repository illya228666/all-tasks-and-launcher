using Launcher.Device.Connection;
using Launcher.Device.Data;
using Launcher.Pet.Windows;

namespace Launcher.Connections;
internal sealed class DevicePetConnection : IDisposable
{
    private readonly DeviceConnection _device;
    private readonly PetWindowsSession? _pet;
    private readonly Action<Action> _post;
    internal DevicePetConnection(DeviceConnection device, PetWindowsSession? pet, Action<Action> post)
    {
        _device = device;
        _pet = pet;
        _post = post;
        _device.InputReceived += InputReceived;
        _device.FadeReceived += FadeReceived;
        _device.StateChanged += StateChanged;
    }

    private void InputReceived(DeviceInput input)
    {
        if (input == DeviceInput.PrimaryButtonPressed)
            _post(() => _pet?.TryStartEarthquake());
    }

    private void FadeReceived(byte fade) => _post(() => _pet?.SetHatFade(fade));

    private void StateChanged(DeviceState state)
    {
        if (!state.IsConnected || state.ProtocolVersion != DeviceProtocolVersion.V3)
            _post(() => _pet?.SetHatFade(0));
    }

    public void Dispose()
    {
        _device.InputReceived -= InputReceived;
        _device.FadeReceived -= FadeReceived;
        _device.StateChanged -= StateChanged;
    }
}

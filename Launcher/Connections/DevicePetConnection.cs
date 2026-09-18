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
    }

    private void InputReceived(DeviceInput input)
    {
        if (input == DeviceInput.PrimaryButtonPressed)
            _post(() => _pet?.TryStartEarthquake());
    }

    public void Dispose() => _device.InputReceived -= InputReceived;
}

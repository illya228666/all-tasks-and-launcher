using Launcher.Device.Connection;
using Launcher.Device.Data;
using Launcher.Pet;
using Launcher.Pet.Windows;
using Launcher.Windows;

namespace Launcher.Connections;
internal sealed class DeviceInputConnection : IDisposable
{
    private static readonly (DeviceLights Lights, Color Color)[] ChaosFrames =
    {
        (DeviceLights.Red, Color.FromArgb(220, 52, 52)),
        (DeviceLights.Yellow, Color.FromArgb(255, 193, 7)),
        (DeviceLights.Green, Color.FromArgb(40, 180, 99))
    };

    private readonly DeviceConnection _device;
    private readonly PetWindowsSession? _pet;
    private readonly AppListConnection _apps;
    private readonly MainWindow _window;
    private readonly Action<Action> _post;
    private readonly System.Windows.Forms.Timer _chaosTimer = new()
    {
        Interval = 140
    };

    private long _chaosUntilMs;
    private int _chaosFrame;
    private bool _disposed;

    internal DeviceInputConnection(DeviceConnection device, PetWindowsSession? pet, AppListConnection apps, MainWindow window, Action<Action> post)
    {
        _device = device;
        _pet = pet;
        _apps = apps;
        _window = window;
        _post = post;
        _device.InputReceived += InputReceived;
        _chaosTimer.Tick += ChaosTick;
    }

    private void InputReceived(DeviceInput input) => _post(() => HandleInput(input));

    private void HandleInput(DeviceInput input)
    {
        if (_disposed)
            return;

        _window.Device.DisplayInput(input);

        switch (input)
        {
            case DeviceInput.PrimaryButtonPressed:
            case DeviceInput.LeftButtonPressed:
                _pet?.TryStartEarthquake();
                break;
            case DeviceInput.RightButtonPressed:
                _apps.LaunchRandom();
                break;
            case DeviceInput.BothButtonsPressed:
                StartChaos();
                break;
        }
    }

    private void StartChaos()
    {
        _pet?.TryStartEarthquake();
        _chaosUntilMs = Environment.TickCount64 + PetWorld.EarthquakeDurationMs;
        _chaosFrame = 0;
        ApplyChaosFrame();
        _chaosTimer.Start();
    }

    private void ChaosTick(object? sender, EventArgs args)
    {
        if (_disposed || Environment.TickCount64 >= _chaosUntilMs)
        {
            EndChaos();
            return;
        }

        _chaosFrame = (_chaosFrame + 1) % ChaosFrames.Length;
        ApplyChaosFrame();
    }

    private void ApplyChaosFrame()
    {
        var frame = ChaosFrames[_chaosFrame];
        _device.SetLights(frame.Lights);
        _window.SetChaosColor(frame.Color);
    }

    private void EndChaos()
    {
        _chaosTimer.Stop();
        _device.SetLights(DeviceLights.None);
        if (!_window.IsDisposed)
            _window.SetChaosColor(null);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _device.InputReceived -= InputReceived;
        _chaosTimer.Tick -= ChaosTick;
        EndChaos();
        _chaosTimer.Dispose();
    }
}

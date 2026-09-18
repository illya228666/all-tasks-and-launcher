using Launcher.Device.Data;

namespace Launcher.Windows;
internal sealed class DevicePanel : FlowLayoutPanel
{
    private readonly Label _status = new()
    {
        Text = "Controller: wartet",
        AutoSize = true,
        MinimumSize = new Size(255, 0),
        Margin = new Padding(8, 8, 8, 0)
    };

    private readonly Label _input = new()
    {
        Text = "Input: -",
        AutoSize = true,
        MinimumSize = new Size(90, 0),
        Margin = new Padding(4, 8, 8, 0)
    };

    private readonly Button _on = new()
    {
        Text = "LED ON",
        AutoSize = true,
        Enabled = false
    };

    private readonly Button _off = new()
    {
        Text = "LED OFF",
        AutoSize = true,
        Enabled = false
    };

    internal event Action<bool>? IndicatorRequested;

    internal DevicePanel()
    {
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Dock = DockStyle.None;
        WrapContents = false;
        Margin = new Padding(12, 0, 0, 0);
        Controls.AddRange(new Control[] { _status, _input, _on, _off });

        _on.Click += (_, _) => IndicatorRequested?.Invoke(true);
        _off.Click += (_, _) => IndicatorRequested?.Invoke(false);
    }

    internal void Display(DeviceState state)
    {
        _status.Text = state.Phase switch
        {
            DeviceConnectionPhase.Waiting => "Controller: wartet",
            DeviceConnectionPhase.Searching => "Controller: suche...",
            DeviceConnectionPhase.Connecting => $"Controller: pruefe {state.PortName ?? "COM"}...",
            DeviceConnectionPhase.Connected => $"Controller: {state.PortName} | IO v{(int)(state.ProtocolVersion ?? DeviceProtocolVersion.V1)}",
            DeviceConnectionPhase.Retry => "Controller: nicht verbunden",
            _ => "Controller: unbekannt"
        };

        if (!string.IsNullOrWhiteSpace(state.Message) && state.Phase != DeviceConnectionPhase.Connected)
            _status.Text += " | " + state.Message;

        _on.Enabled = _off.Enabled = state.IsConnected;
        if (!state.IsConnected)
            _input.Text = "Input: -";
    }

    internal void DisplayInput(DeviceInput input)
    {
        _input.Text = "Input: " + (input switch
        {
            DeviceInput.PrimaryButtonPressed => "V1 BUTTON",
            DeviceInput.LeftButtonPressed => "LEFT",
            DeviceInput.RightButtonPressed => "RIGHT",
            DeviceInput.BothButtonsPressed => "BOTH",
            _ => input.ToString()
        });
    }
}

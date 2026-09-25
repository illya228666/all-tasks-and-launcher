using Launcher.Device.Data;
namespace Launcher.Windows;

internal sealed class DevicePanel : FlowLayoutPanel
{
    private readonly Label _status = new() { AutoSize = false, Width = 310, Height = 28, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label _input = new() { AutoSize = true, Text = "Input: -", Margin = new Padding(4, 7, 8, 0) };
    private readonly Button _on = new() { Text = "Indicator ON", AutoSize = true, Enabled = false };
    private readonly Button _off = new() { Text = "Indicator OFF", AutoSize = true, Enabled = false };
    private readonly ToolTip _detail = new();
    internal event Action<bool>? IndicatorRequested;
    internal DevicePanel()
    {
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Dock = DockStyle.Fill;
        WrapContents = true;
        Controls.AddRange(new Control[] { _status, _input, _on, _off });
        _on.Click += (_, _) => IndicatorRequested?.Invoke(true);
        _off.Click += (_, _) => IndicatorRequested?.Invoke(false);
    }
    internal void Display(DeviceState state)
    {
        _status.Text = DevicePresentation.Status(state);
        _detail.SetToolTip(_status, string.Join(Environment.NewLine,
            new[] { _status.Text, state.Failure == DeviceFailure.None ? null : DevicePresentation.Failure(state.Failure), state.Detail }.Where(text => !string.IsNullOrWhiteSpace(text))));
        bool indicator = state.IsConnected && state.Capabilities.HasFlag(DeviceCapabilities.Indicator);
        _on.Visible = _off.Visible = indicator;
        _on.Enabled = _off.Enabled = indicator;
        if (!state.IsConnected) _input.Text = "Input: -";
    }
    internal void DisplayInput(DeviceInput input) => _input.Text = "Input: " + (input switch
    {
        DeviceInput.PrimaryButtonPressed => "BUTTON",
        DeviceInput.LeftButtonPressed => "LEFT",
        DeviceInput.RightButtonPressed => "RIGHT",
        DeviceInput.BothButtonsPressed => "BOTH",
        _ => "?"
    });

    internal void DisplayFade(byte fade)
    {
        int percent = (int)Math.Round(fade * 100d / 255d);
        _input.Text = $"Input: FADE {percent}%";
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) _detail.Dispose();
        base.Dispose(disposing);
    }
}

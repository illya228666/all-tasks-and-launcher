namespace Launcher.Windows;
internal sealed class DevicePanel : FlowLayoutPanel
{
    private readonly Label _status = new()
    {
        Text = "Controller: nicht verbunden",
        AutoSize = true,
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
        Dock = DockStyle.Right;
        WrapContents = false;
        Controls.AddRange(new Control[] { _status, _on, _off });
        _on.Click += (_, _) => IndicatorRequested?.Invoke(true);
        _off.Click += (_, _) => IndicatorRequested?.Invoke(false);
    }

    internal void Display(bool connected, string? port)
    {
        _status.Text = connected ? "Controller: " + port : "Controller: nicht verbunden";
        _on.Enabled = _off.Enabled = connected;
    }
}

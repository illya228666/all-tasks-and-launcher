namespace Launcher.Windows;
internal sealed class StatusPanel : TableLayoutPanel
{
    private readonly Label _stats = new()
    {
        AutoSize = true,
        Dock = DockStyle.Fill
    };
    private readonly Label _hint = new()
    {
        AutoSize = true,
        Dock = DockStyle.Fill,
        Text = "Ctrl+F Suche, Ctrl+R Surprise, F5 Refresh, Esc loeschen"
    };
    internal StatusPanel()
    {
        Dock = DockStyle.Fill;
        AutoSize = true;
        ColumnCount = 1;
        RowCount = 2;
        Padding = new Padding(10);
        Controls.Add(_stats, 0, 0);
        Controls.Add(_hint, 0, 1);
    }

    internal void ShowStats(string text) => _stats.Text = text;
    internal void ShowHint(string text) => _hint.Text = text;
}

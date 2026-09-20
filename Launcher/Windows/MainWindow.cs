namespace Launcher.Windows;
internal sealed class MainWindow : Form
{
    private readonly TableLayoutPanel _header = new()
    {
        Dock = DockStyle.Fill,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        ColumnCount = 1,
        RowCount = 3,
        Padding = new Padding(10)
    };
    private readonly Label _title;
    private readonly Font _titleFont = new("Segoe UI", 18, FontStyle.Bold);
    private readonly Icon _windowIcon;
    private readonly Button _theme = new()
    {
        AutoSize = true,
        Text = "Theme: Light"
    };
    private readonly CheckBox _collisions = new()
    {
        AutoSize = true,
        Text = "Kollisionen anzeigen"
    };
    private readonly TableLayoutPanel _layout = new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = 1,
        RowCount = 4
    };
    internal SearchPanel Search { get; } = new();
    internal AppListPanel Apps { get; } = new();
    internal DevicePanel Device { get; } = new();
    internal StatusPanel Status { get; } = new();

    internal event Action? ThemeRequested;
    internal event Action<bool>? CollisionsChanged;
    internal event Action? RefreshRequested;
    internal event Action? RandomRequested;
    internal MainWindow(bool showCollisions)
    {
        Text = "Zahlen Launcher";
        ClientSize = new(1100, 800);
        MinimumSize = new(980, 720);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Font;
        KeyPreview = true;
        DoubleBuffered = true;
        _title = new Label
        {
            Text = "Launcher Control Center",
            Font = _titleFont,
            Dock = DockStyle.Top,
            Height = 38
        };
        var options = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true
        };
        _collisions.Checked = showCollisions;
        options.Controls.AddRange(new Control[] { _theme, _collisions });
        _header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int row = 0; row < 3; row++) _header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _header.Controls.Add(_title, 0, 0);
        _header.Controls.Add(options, 0, 1);
        _header.Controls.Add(Device, 0, 2);
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.Controls.Add(_header, 0, 0);
        _layout.Controls.Add(Search, 0, 1);
        _layout.Controls.Add(Apps, 0, 2);
        _layout.Controls.Add(Status, 0, 3);
        Controls.Add(_layout);
        _theme.Click += (_, _) => ThemeRequested?.Invoke();
        _collisions.CheckedChanged += (_, _) => CollisionsChanged?.Invoke(_collisions.Checked);
        using var resourceIcon = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow)).GetObject("$this.Icon") as Icon;
        _windowIcon = (Icon)(resourceIcon ?? SystemIcons.Application).Clone();
        Icon = _windowIcon;
    }

    internal void SetTheme(WindowTheme theme)
    {
        theme.Apply(this);
        _header.BackColor = theme.Header;
        _title.BackColor = theme.Header;
        _title.ForeColor = Color.White;
        _theme.Text = theme.Dark ? "Theme: Dark" : "Theme: Light";
        Apps.SetTheme(theme);
    }

    internal void DisableActions()
    {
        Search.Enabled = Device.Enabled = Apps.Enabled = _header.Enabled = false;
    }

    protected override void OnKeyDown(KeyEventArgs args)
    {
        base.OnKeyDown(args);
        if (!Search.Enabled)
            return;
        if (args.Control && args.KeyCode == Keys.F)
            Search.FocusSearch();
        else if (args.Control && args.KeyCode == Keys.R)
            RandomRequested?.Invoke();
        else if (args.Control && args.KeyCode == Keys.D)
            ThemeRequested?.Invoke();
        else if (args.KeyCode == Keys.F5)
            RefreshRequested?.Invoke();
        else if (args.KeyCode == Keys.Escape)
            Search.ClearSearch();
        else
            return;
        args.Handled = args.SuppressKeyPress = true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _titleFont.Dispose();
            _windowIcon.Dispose();
        }

        base.Dispose(disposing);
    }
}

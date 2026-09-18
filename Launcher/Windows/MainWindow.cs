namespace Launcher.Windows;
internal sealed class MainWindow : Form
{
    private readonly Panel _header = new()
    {
        Dock = DockStyle.Fill,
        Height = 92,
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
    private WindowTheme _currentTheme = new(false);
    private Color? _chaosColor;

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
            Dock = DockStyle.Bottom,
            Height = 36,
            WrapContents = false,
            AutoScroll = true
        };
        _collisions.Checked = showCollisions;
        options.Controls.AddRange(new Control[] { _theme, _collisions, Device });
        _header.Controls.Add(options);
        _header.Controls.Add(_title);
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
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
        _currentTheme = theme;
        ApplyBaseTheme();
        if (_chaosColor is Color color)
            ApplyChaosColor(color);
    }

    internal void SetChaosColor(Color? color)
    {
        _chaosColor = color;
        if (color is Color active)
            ApplyChaosColor(active);
        else
            ApplyBaseTheme();
    }

    private void ApplyBaseTheme()
    {
        _currentTheme.Apply(this);
        _header.BackColor = _currentTheme.Header;
        _title.BackColor = _currentTheme.Header;
        _title.ForeColor = Color.White;
        _theme.Text = _currentTheme.Dark ? "Theme: Dark" : "Theme: Light";
        Apps.SetTheme(_currentTheme);
    }

    private void ApplyChaosColor(Color color)
    {
        Color foreground = Contrast(color);
        _header.BackColor = color;
        _title.BackColor = color;
        _title.ForeColor = foreground;
        ApplyChaosColor(this, color, foreground);
    }

    private static void ApplyChaosColor(Control parent, Color background, Color foreground)
    {
        foreach (Control child in parent.Controls)
        {
            if (child is ButtonBase or TextBox or ComboBox)
            {
                child.BackColor = background;
                child.ForeColor = foreground;
                if (child is Button button)
                    button.FlatAppearance.BorderColor = foreground;
            }

            ApplyChaosColor(child, background, foreground);
        }
    }

    private static Color Contrast(Color color)
    {
        int luminance = color.R * 299 + color.G * 587 + color.B * 114;
        return luminance >= 150000 ? Color.Black : Color.White;
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

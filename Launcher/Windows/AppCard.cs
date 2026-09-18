using Launcher.Apps.Data;

namespace Launcher.Windows;
internal sealed class AppCard : UserControl
{
    private readonly AppCardData _data;
    private WindowTheme _theme;
    private readonly ContextMenuStrip _menu = new();
    private readonly Font _titleFont = new("Segoe UI", 10, FontStyle.Bold);
    internal event Action<AppInfo, AppCardAction>? Requested;
    internal AppCard(AppCardData data, WindowTheme theme)
    {
        _data = data;
        _theme = theme;
        DoubleBuffered = true;
        Size = new(280, 166);
        Margin = new Padding(6);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
            ColumnCount = 1,
            RowCount = 5
        };
        var title = new Label
        {
            Text = data.App.Name,
            Font = _titleFont,
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            Height = 24
        };
        string last = data.Usage.LastLaunchUtc?.ToLocalTime().ToString("dd.MM.yyyy HH:mm") ?? "-";
        var labels = new[]
        {
            title,
            new Label
            {
                Text = data.App.Category,
                AutoEllipsis = true,
                Dock = DockStyle.Fill
            },
            new Label
            {
                Text = data.App.IsAvailable ? Path.GetFileName(data.App.ExePath) : "Datei fehlt",
                Dock = DockStyle.Fill
            },
            new Label
            {
                Text = $"Starts: {data.Usage.Count} | Letzter Start: {last}",
                Dock = DockStyle.Fill
            }
        };
        for (int i = 0; i < labels.Length; i++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, i == 0 ? 25 : 24));
            layout.Controls.Add(labels[i], 0, i);
            labels[i].DoubleClick += StartDoubleClick;
        }

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            Margin = Padding.Empty
        };
        AddButton("Start", AppCardAction.Start, data.App.IsAvailable);
        AddButton("Ordner", AppCardAction.OpenFolder, true);
        AddButton("Path", AppCardAction.CopyPath, true);
        AddButton(data.Favorite ? "Fav-" : "Fav+", AppCardAction.ToggleFavorite, true);
        layout.Controls.Add(actions, 0, 4);
        Controls.Add(layout);
        _menu.Items.Add("Als Admin starten", null, (_, _) => Raise(AppCardAction.StartAsAdmin));
        _menu.Items.Add("EXE-Pfad kopieren", null, (_, _) => Raise(AppCardAction.CopyPath));
        ContextMenuStrip = _menu;
        DoubleClick += StartDoubleClick;
        SetTheme(theme);
        void AddButton(string text, AppCardAction action, bool enabled)
        {
            var button = new Button
            {
                Text = text,
                Width = 59,
                Height = 27,
                Margin = new Padding(1),
                Enabled = enabled
            };
            button.Click += (_, _) => Raise(action);
            actions.Controls.Add(button);
        }
    }

    private void Raise(AppCardAction action) => Requested?.Invoke(_data.App, action);
    private void StartDoubleClick(object? sender, EventArgs args)
    {
        if (_data.App.IsAvailable)
            Raise(AppCardAction.Start);
    }

    internal void SetTheme(WindowTheme theme)
    {
        _theme = theme;
        theme.Apply(this);
        BackColor = theme.Surface;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs args)
    {
        base.OnPaint(args);
        using var pen = new Pen(_data.Favorite ? _theme.Accent : _theme.Border, _data.Favorite ? 2 : 1);
        args.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _menu.Dispose();
            _titleFont.Dispose();
            Requested = null;
        }

        base.Dispose(disposing);
    }
}

using Launcher.Apps.Data;
using Launcher.Pet.Windows.Windows;

namespace Launcher.Windows;
internal sealed class AppListPanel : UserControl
{
    private readonly FlowLayoutPanel _content = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        WrapContents = false,
        FlowDirection = FlowDirection.TopDown
    };
    private readonly List<(Panel Section, FlowLayoutPanel Cards)> _sections = new();
    private WindowTheme _theme = new(false);
    private bool _layingOut;
    internal PetArea PetArea { get; } = new();

    internal event Action<AppInfo, AppCardAction>? Requested;
    internal AppListPanel()
    {
        Dock = DockStyle.Fill;
        Controls.Add(_content);
        _content.SizeChanged += (_, _) => Arrange();
    }

    internal void Display(IReadOnlyList<AppCardData> apps, bool grouped)
    {
        Point scroll = _content.AutoScrollPosition;
        _content.SuspendLayout();
        try
        {
            PetArea.Parent?.Controls.Remove(PetArea);
            foreach (Control card in PetArea.Controls.Cast<Control>().ToArray())
                card.Dispose();
            foreach (Control section in _content.Controls.Cast<Control>().ToArray())
                section.Dispose();
            _sections.Clear();
            var groups = grouped ? apps.GroupBy(card => card.App.Category).Select(group => (Title: group.Key, Cards: group.ToArray())).ToArray() : new[]
            {
                (Title: apps.Count == 0 ? "Keine Apps gefunden. Pruefe deine Filter." : $"Ergebnisse ({apps.Count})", Cards: apps.ToArray())
            };
            if (groups.Length == 0)
                groups = new[]
                {
                    ("Keine Apps gefunden. Pruefe deine Filter.", Array.Empty<AppCardData>())
                };
            for (int index = 0; index < groups.Length; index++)
            {
                var section = new Panel
                {
                    Margin = new Padding(4, 6, 4, 10)
                };
                var title = new Label
                {
                    Text = groups[index].Title,
                    Location = new Point(10, 10),
                    AutoSize = true
                };
                FlowLayoutPanel cards = index == groups.Length - 1 ? PetArea : new FlowLayoutPanel
                {
                    WrapContents = true
                };
                cards.Location = new Point(8, 38);
                foreach (AppCardData data in groups[index].Cards)
                {
                    var card = new AppCard(data, _theme);
                    card.Requested += Forward;
                    cards.Controls.Add(card);
                }

                section.Controls.Add(title);
                section.Controls.Add(cards);
                _content.Controls.Add(section);
                _sections.Add((section, cards));
            }

            SetTheme(_theme);
        }
        finally
        {
            _content.ResumeLayout();
        }

        Arrange();
        _content.AutoScrollPosition = new Point(-scroll.X, -scroll.Y);
    }

    private void Forward(AppInfo app, AppCardAction action) => Requested?.Invoke(app, action);
    private void Arrange()
    {
        if (_layingOut)
            return;
        _layingOut = true;
        try
        {
            int width = Math.Max(860, _content.ClientSize.Width - 34);
            foreach (var(section, cards)in _sections)
            {
                section.Width = width;
                cards.Width = width - 16;
                int columns = Math.Max(1, cards.Width / 292);
                int rows = (cards.Controls.Count + columns - 1) / columns;
                int cardsHeight = rows * 178;
                cards.Height = cardsHeight + (cards == PetArea ? PetArea.RequiredExtraHeight : 0);
                cards.PerformLayout();
                if (cards == PetArea)
                    PetArea.SetGeometry(cardsHeight, cards.Controls.Cast<Control>().Select(control => control.Bounds));
                section.Height = cards.Top + cards.Height + 10;
            }
        }
        finally
        {
            _layingOut = false;
        }
    }

    internal void SetTheme(WindowTheme theme)
    {
        _theme = theme;
        BackColor = _content.BackColor = theme.Background;
        foreach (var(section, cards)in _sections)
        {
            section.BackColor = cards.BackColor = theme.SurfaceAlt;
            section.ForeColor = theme.Text;
            foreach (AppCard card in cards.Controls.OfType<AppCard>())
                card.SetTheme(theme);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && PetArea.Parent is null)
            PetArea.Dispose();
        base.Dispose(disposing);
    }
}

using System.Drawing;
using System.IO;
using Launcher.Domain;
using Launcher.UI.Controls;

namespace Launcher.UI;

public partial class Main
{
    #region [RU] Бизнес-логика | [DE] Fachlogik

    /// <summary>
    /// RU: Перерисовывает центральную область со списком приложений.
    /// DE: Rendert den zentralen Bereich mit der Anwendungsliste neu.
    /// </summary>
    private void Render(bool preservePetSpeech = false)
    {
        if (!IsHandleCreated)
            return;

        List<AppEntry> visibleApps = BuildVisibleApps();
        Point scrollPosition = flpApps.AutoScrollPosition;
        _pet.BeginHostChange(preservePetSpeech);

        flpApps.SuspendLayout();
        try
        {
            flpApps.Controls.Clear();
            if (visibleApps.Count == 0)
            {
                flpApps.Controls.Add(CreateEmptyState());
                flpApps.Controls.Add(CreatePetOnlyPanel());
            }
            else
            {
                bool groupedByCategory = GetSelectedSortMode() == SortMode.ByCategory
                    && string.Equals(_cbCategory.SelectedItem as string,
                        LauncherConstants.AllCategories, StringComparison.Ordinal);

                if (groupedByCategory)
                {
                    var groups = visibleApps
                        .GroupBy(app => app.Category)
                        .OrderBy(group => group.Key)
                        .Select(group => (group.Key, Apps: group.ToList()))
                        .ToList();

                    for (int index = 0; index < groups.Count; index++)
                    {
                        flpApps.Controls.Add(CreateSection(
                            groups[index].Key,
                            groups[index].Apps,
                            includePetZone: index == groups.Count - 1));
                    }
                }
                else
                {
                    flpApps.Controls.Add(CreateSection(
                        $"Ergebnisse ({visibleApps.Count})", visibleApps, includePetZone: true));
                }
            }
        }
        finally
        {
            flpApps.ResumeLayout();
            if (preservePetSpeech)
                flpApps.AutoScrollPosition = new Point(-scrollPosition.X, -scrollPosition.Y);
            _pet.EndHostChange();
        }
        UpdateStats(visibleApps);
    }

    #endregion

    #region [RU] Вспомогательные методы | [DE] Hilfsmethoden

    private Control CreateSection(string title, List<AppEntry> apps, bool includePetZone)
    {
        int width = Math.Max(860, flpApps.ClientSize.Width - 34);

        var section = new Panel
        {
            Width = width,
            Margin = new Padding(4, 6, 4, 10),
            BackColor = SurfaceAlt
        };

        section.Paint += (_, e) =>
        {
            using var pen = new Pen(BorderColor, 1f);
            var rect = section.ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;
            e.Graphics.DrawRectangle(pen, rect);
        };

        var headerLabel = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = TextPrimary,
            Location = new Point(10, 10),
            Size = new Size(width - 20, 22)
        };

        var cardsPanel = new FlowLayoutPanel
        {
            Location = new Point(8, 38),
            Width = width - 16,
            WrapContents = true,
            BackColor = Color.Transparent
        };

        foreach (AppEntry app in apps)
            cardsPanel.Controls.Add(CreateCard(app));

        const int cardWidth = 292;
        const int cardHeight = 178;
        int columns = Math.Max(1, cardsPanel.Width / cardWidth);
        int rows = (apps.Count + columns - 1) / columns;
        int cardsHeight = Math.Max(cardHeight, rows * cardHeight);
        cardsPanel.Height = cardsHeight + (includePetZone ? _pet.RequiredHostHeight : 0);

        if (includePetZone)
        {
            cardsPanel.BackColor = SurfaceAlt;
            _pet.AttachHost(cardsPanel, cardsHeight);
        }

        section.Height = cardsPanel.Top + cardsPanel.Height + 10;
        section.Controls.Add(headerLabel);
        section.Controls.Add(cardsPanel);
        return section;
    }

    private Panel CreatePetOnlyPanel()
    {
        var panel = new Panel
        {
            Width = Math.Max(_pet.RequiredHostWidth, flpApps.ClientSize.Width - 34),
            Height = _pet.RequiredHostHeight,
            Margin = new Padding(4, 0, 4, 0),
            BackColor = SurfaceAlt
        };

        _pet.AttachHost(panel, 0);
        return panel;
    }

    private Control CreateEmptyState()
    {
        var panel = new Panel
        {
            Width = Math.Max(760, flpApps.ClientSize.Width - 48),
            Height = 118,
            BackColor = Surface,
            Margin = new Padding(10)
        };

        panel.Paint += (_, e) =>
        {
            using var pen = new Pen(BorderColor, 1f);
            var rect = panel.ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;
            e.Graphics.DrawRectangle(pen, rect);
        };

        panel.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10),
            ForeColor = TextMuted,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "Keine Apps gefunden. Baue die Loesung oder pruefe deine Filter."
        });
        return panel;
    }

    private Control CreateCard(AppEntry app)
    {
        bool executableExists = File.Exists(app.ExePath);
        bool favorite = _launcherFacade.IsFavorite(app, _favoriteKeys);
        AppUsage usage = _launcherFacade.GetUsage(app, _state);
        string lastStart = usage.LastLaunchUtc.HasValue
            ? usage.LastLaunchUtc.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
            : "-";

        var card = new AppCardControl();
        card.BindData(
            app.Name,
            app.Category,
            executableExists ? Path.GetFileName(app.ExePath) : "Datei fehlt",
            $"Starts: {usage.Count} | Letzter Start: {lastStart}",
            executableExists,
            favorite,
            Surface,
            SurfaceAlt,
            TextPrimary,
            TextMuted,
            Accent,
            BorderColor);

        card.StartClicked += (_, __) => StartApp(app);
        card.FolderClicked += (_, __) => OpenFolder(app.FolderPath);
        card.PathClicked += (_, __) => CopyPath(app.ExePath);
        card.FavoriteClicked += (_, __) =>
        {
            _launcherFacade.ToggleFavorite(app, _favoriteKeys, _state);
            PersistState();
            Render();
        };
        card.CardDoubleClicked += (_, __) =>
        {
            if (File.Exists(app.ExePath))
                StartApp(app);
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Als Admin starten", null, (_, __) => StartApp(app, asAdmin: true));
        menu.Items.Add("EXE-Pfad kopieren", null, (_, __) => CopyPath(app.ExePath));
        card.ContextMenuStrip = menu;

        _tips.SetToolTip(card, "Doppelklick = Start | Rechtsklick = Mehr Optionen");
        return card;
    }

    #endregion
}

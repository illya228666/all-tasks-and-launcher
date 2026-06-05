using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
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
    private void Render()
    {
        if (!IsHandleCreated)
        {
            return;
        }

        List<AppEntry> visibleApps = BuildVisibleApps();

        flpApps.SuspendLayout();
        flpApps.Controls.Clear();

        if (visibleApps.Count == 0)
        {
            flpApps.Controls.Add(CreateEmptyState());
        }
        else
        {
            bool groupedByCategory = GetSelectedSortMode() == SortMode.ByCategory
                && string.Equals(_cbCategory.SelectedItem as string, LauncherConstants.AllCategories, System.StringComparison.Ordinal);

            if (groupedByCategory)
            {
                foreach (var group in visibleApps.GroupBy(app => app.Category).OrderBy(group => group.Key))
                {
                    flpApps.Controls.Add(CreateSection(group.Key, group.ToList()));
                }
            }
            else
            {
                flpApps.Controls.Add(CreateSection($"Ergebnisse ({visibleApps.Count})", visibleApps));
            }
        }

        flpApps.ResumeLayout();
        UpdateStats(visibleApps);
    }

    #endregion

    #region [RU] Вспомогательные методы | [DE] Hilfsmethoden

    private Control CreateSection(string title, List<AppEntry> apps)
    {
        int width = System.Math.Max(860, flpApps.ClientSize.Width - 34);

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
        {
            cardsPanel.Controls.Add(CreateCard(app));
        }

        int cardWidth = 292;
        int cardHeight = 178;
        int columns = System.Math.Max(1, cardsPanel.Width / cardWidth);
        int rows = (apps.Count + columns - 1) / columns;
        cardsPanel.Height = System.Math.Max(cardHeight, rows * cardHeight);

        section.Height = cardsPanel.Top + cardsPanel.Height + 10;
        section.Controls.Add(headerLabel);
        section.Controls.Add(cardsPanel);

        return section;
    }

    private Control CreateEmptyState()
    {
        var panel = new Panel
        {
            Width = System.Math.Max(760, flpApps.ClientSize.Width - 48),
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

        var label = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10),
            ForeColor = TextMuted,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "Keine Apps gefunden. Baue die Loesung oder pruefe deine Filter."
        };

        panel.Controls.Add(label);
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
            {
                StartApp(app);
            }
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

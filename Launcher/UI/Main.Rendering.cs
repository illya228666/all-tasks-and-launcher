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
    private const int PetFrameWidth = 192;
    private const int PetFrameHeight = 208;
    private const int PetIdleRow = 0;
    private const int PetMoveRightRow = 1;
    private const int PetMoveLeftRow = 2;
    private const int PetWaveRow = 3;
    private const int PetJumpRow = 4;
    private const int PetFailedRow = 5;
    private const int PetWaveIntervalMs = 9000;
    private const int PetAppStateLoopCount = 3;
    private const int PetMovementMinDelayMs = 15000;
    private const int PetMovementMaxDelayMs = 30000;
    private const int PetJumpMinDelayMs = 15000;
    private const int PetJumpMaxDelayMs = 30000;
    private const int PetEdgePadding = 16;
    private const int PetCardProbeOffset = 2;
    private const float PetPixelsPerMovementCycle = 120f;

    // Source: openai/codex codex-rs/tui/src/pets/model.rs default_animations().
    private static readonly int[][] PetFrameDurationsByRow =
    {
        new[] { 1680, 660, 660, 840, 840, 1920 },       // idle
        new[] { 120, 120, 120, 120, 120, 120, 120, 220 }, // running-right / move_right
        new[] { 120, 120, 120, 120, 120, 120, 120, 220 }, // running-left / move_left
        new[] { 140, 140, 140, 280 },                   // waving / wave
        new[] { 140, 140, 140, 140, 280 },              // jumping / bounce
        new[] { 140, 140, 140, 140, 140, 140, 140, 240 }, // failed / sad
        new[] { 150, 150, 150, 150, 150, 260 },         // waiting
        new[] { 120, 120, 120, 120, 120, 220 },         // running
        new[] { 150, 150, 150, 150, 150, 280 }          // review
    };

    private static readonly (int Row, int Frame, float Lift)[] PetSuccessfulJumpFrames =
    {
        (PetJumpRow, 0, 0f),
        (PetJumpRow, 1, 0.5f),
        (PetJumpRow, 2, 1f),
        (PetJumpRow, 3, 0.5f),
        (PetJumpRow, 4, 0f)
    };

    private static readonly (int Row, int Frame, float Lift)[] PetFailedJumpFrames =
    {
        (PetJumpRow, 0, 0f),
        (PetJumpRow, 1, 1f),
        (PetJumpRow, 3, 0.5f),
        (PetFailedRow, 4, 0f),
        (PetFailedRow, 3, 0f),
        (PetFailedRow, 5, 0f),
        (PetFailedRow, 6, 0f),
        (PetFailedRow, 7, 0f)
    };

    private Bitmap? _petAtlas;
    private Panel _petPanel = null!;
    private System.Windows.Forms.Timer _petTimer = null!;
    private System.Windows.Forms.Timer _petMovementTimer = null!;
    private System.Windows.Forms.Timer _petJumpTimer = null!;
    private int _petRow = PetIdleRow;
    private int _petFrame;
    private int _petIdleElapsedMs;
    private int _petWaveLoopsRemaining;
    private int _petGroundY;
    private float _petX = float.NaN;
    private float _petMoveStartX;
    private float _petMoveTargetX;
    private int _petMoveElapsedMs;
    private int _petMoveDurationMs;
    private (int Row, int Frame, float Lift)[]? _petJumpSequence;
    private int _petJumpIndex;
    private float _petJumpPeak;
    private bool _petJumpPending;

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
            flpApps.Controls.Add(CreatePetOnlyPanel());
        }
        else
        {
            bool groupedByCategory = GetSelectedSortMode() == SortMode.ByCategory
                && string.Equals(_cbCategory.SelectedItem as string, LauncherConstants.AllCategories, System.StringComparison.Ordinal);

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
                flpApps.Controls.Add(CreateSection($"Ergebnisse ({visibleApps.Count})", visibleApps, includePetZone: true));
            }
        }

        flpApps.ResumeLayout();
        UpdateStats(visibleApps);
    }

    #endregion

    #region [RU] Вспомогательные методы | [DE] Hilfsmethoden

    private void InitializePet()
    {
        string atlasPath = Path.Combine(AppContext.BaseDirectory, "Resources", "sumrak-spritesheet.png");
        using var source = new Bitmap(atlasPath);

        if (source.Width != 1536 || source.Height != 2288)
        {
            throw new InvalidDataException($"Unerwartete Sumrak-Atlasgroesse: {source.Width}x{source.Height}.");
        }

        _petAtlas = new Bitmap(source);
        _petPanel = new Panel
        {
            Height = PetFrameHeight,
            Margin = new Padding(4, 0, 4, 0),
            BackColor = Color.Transparent
        };
        _petPanel.Paint += PetPanel_Paint;
        EnableDoubleBuffer(_petPanel);

        _petTimer = new System.Windows.Forms.Timer(components)
        {
            Interval = PetFrameDurationsByRow[PetIdleRow][0]
        };
        _petTimer.Tick += PetTimer_Tick;

        _petMovementTimer = new System.Windows.Forms.Timer(components);
        _petMovementTimer.Tick += PetMovementTimer_Tick;

        _petJumpTimer = new System.Windows.Forms.Timer(components);
        _petJumpTimer.Tick += PetJumpTimer_Tick;

        System.Diagnostics.Debug.Assert(!IsPetInsideCardSpan(99, 100, 400));
        System.Diagnostics.Debug.Assert(IsPetInsideCardSpan(100, 100, 400));
        System.Diagnostics.Debug.Assert(IsPetInsideCardSpan(250, 100, 400));
        System.Diagnostics.Debug.Assert(!IsPetInsideCardSpan(401, 100, 400));
    }

    private void PetTimer_Tick(object? sender, System.EventArgs e)
    {
        if (_petJumpSequence is not null)
        {
            AdvancePetJump();
            return;
        }

        int[] durations = PetFrameDurationsByRow[_petRow];
        bool isMoving = _petRow is PetMoveRightRow or PetMoveLeftRow;

        if (isMoving)
        {
            _petMoveElapsedMs += durations[_petFrame];
            float progress = System.Math.Min(1f, (float)_petMoveElapsedMs / _petMoveDurationMs);
            _petX = _petMoveStartX + ((_petMoveTargetX - _petMoveStartX) * progress);
        }
        else if (_petRow == PetIdleRow)
        {
            _petIdleElapsedMs += durations[_petFrame];
        }

        _petFrame++;

        if (_petFrame >= durations.Length)
        {
            _petFrame = 0;

            if (_petRow == PetWaveRow && --_petWaveLoopsRemaining == 0)
            {
                _petRow = PetIdleRow;
            }
        }

        if (isMoving && _petMoveElapsedMs >= _petMoveDurationMs)
        {
            _petX = _petMoveTargetX;
            ClampPetPosition();
            _petRow = PetIdleRow;
            _petFrame = 0;
            _petMoveElapsedMs = 0;
            ScheduleNextPetMovement();
        }

        if (_petRow == PetIdleRow && _petJumpPending)
        {
            StartPetJump();
            return;
        }

        if (_petRow == PetIdleRow && _petIdleElapsedMs >= PetWaveIntervalMs)
        {
            _petRow = PetWaveRow;
            _petFrame = 0;
            _petIdleElapsedMs = 0;
            _petWaveLoopsRemaining = PetAppStateLoopCount;
        }

        durations = PetFrameDurationsByRow[_petRow];
        _petTimer.Interval = durations[_petFrame];
        _petPanel.Invalidate();
    }

    private void PetJumpTimer_Tick(object? sender, System.EventArgs e)
    {
        _petJumpTimer.Stop();

        if (_petRow is PetMoveRightRow or PetMoveLeftRow)
        {
            ScheduleNextPetJump();
            return;
        }

        if (_petRow != PetIdleRow)
        {
            _petJumpPending = true;
            return;
        }

        StartPetJump();
    }

    private void StartPetJump()
    {
        bool failed = IsPetBelowBottomCardRow();
        _petJumpTimer.Stop();
        _petJumpPending = false;
        _petJumpSequence = failed ? PetFailedJumpFrames : PetSuccessfulJumpFrames;
        _petJumpIndex = 0;
        _petJumpPeak = PetFrameHeight / (failed ? 4f : 3f);
        _petIdleElapsedMs = 0;
        ApplyCurrentPetJumpFrame();
    }

    private void AdvancePetJump()
    {
        _petJumpIndex++;

        if (_petJumpSequence is null || _petJumpIndex >= _petJumpSequence.Length)
        {
            _petJumpSequence = null;
            _petJumpIndex = 0;
            _petRow = PetIdleRow;
            _petFrame = 0;
            _petIdleElapsedMs = 0;
            _petTimer.Interval = PetFrameDurationsByRow[PetIdleRow][0];
            ScheduleNextPetJump();
            _petPanel.Invalidate();
            return;
        }

        ApplyCurrentPetJumpFrame();
    }

    private void ApplyCurrentPetJumpFrame()
    {
        (int row, int frame, _) = _petJumpSequence![_petJumpIndex];
        _petRow = row;
        _petFrame = frame;
        _petTimer.Interval = PetFrameDurationsByRow[row][frame];
        _petPanel.Invalidate();
    }

    private bool IsPetBelowBottomCardRow()
    {
        List<AppCardControl> cards = _petPanel.Controls.OfType<AppCardControl>().ToList();

        if (cards.Count == 0)
        {
            return false;
        }

        int bottomRowTop = cards.Max(card => card.Top);
        List<AppCardControl> bottomRow = cards.Where(card => card.Top == bottomRowTop).ToList();
        int left = bottomRow.Min(card => card.Left);
        int right = bottomRow.Max(card => card.Right);

        return IsPetInsideCardSpan(_petX + PetCardProbeOffset, left, right);
    }

    private static bool IsPetInsideCardSpan(float petX, int left, int right) => petX >= left && petX <= right;

    private void ScheduleNextPetJump()
    {
        _petJumpPending = false;
        _petJumpTimer.Stop();
        _petJumpTimer.Interval = _random.Next(PetJumpMinDelayMs, PetJumpMaxDelayMs + 1);
        _petJumpTimer.Start();
    }

    private void PetMovementTimer_Tick(object? sender, System.EventArgs e)
    {
        _petMovementTimer.Stop();

        if (_petRow != PetIdleRow)
        {
            ScheduleNextPetMovement();
            return;
        }

        ClampPetPosition();

        float minX = PetEdgePadding;
        float maxX = System.Math.Max(minX, _petPanel.ClientSize.Width - PetFrameWidth - PetEdgePadding);
        float minDistance = ClientSize.Width / 8f;
        float availableLeft = _petX - minX;
        float availableRight = maxX - _petX;
        bool canMoveLeft = availableLeft >= minDistance;
        bool canMoveRight = availableRight >= minDistance;

        if (!canMoveLeft && !canMoveRight)
        {
            ScheduleNextPetMovement();
            return;
        }

        bool moveRight = canMoveRight && (!canMoveLeft || _random.Next(2) == 0);
        float available = moveRight ? availableRight : availableLeft;
        float distance = minDistance + ((float)_random.NextDouble() * (available - minDistance));

        _petMoveStartX = _petX;
        _petMoveTargetX = _petX + (moveRight ? distance : -distance);
        int movementCycleMs = PetFrameDurationsByRow[PetMoveRightRow].Sum();
        int movementCycles = System.Math.Max(1, (int)System.Math.Round(distance / PetPixelsPerMovementCycle));
        _petMoveDurationMs = movementCycles * movementCycleMs;
        _petMoveElapsedMs = 0;
        _petIdleElapsedMs = 0;
        _petRow = moveRight ? PetMoveRightRow : PetMoveLeftRow;
        _petFrame = 0;

        System.Diagnostics.Debug.Assert(distance >= minDistance && distance <= available);
        _petTimer.Interval = PetFrameDurationsByRow[_petRow][0];
        _petPanel.Invalidate();
    }

    private void ScheduleNextPetMovement()
    {
        _petMovementTimer.Stop();
        _petMovementTimer.Interval = _random.Next(PetMovementMinDelayMs, PetMovementMaxDelayMs + 1);
        _petMovementTimer.Start();
    }

    private void ClampPetPosition()
    {
        float minX = PetEdgePadding;
        float maxX = System.Math.Max(minX, _petPanel.ClientSize.Width - PetFrameWidth - PetEdgePadding);
        _petX = float.IsNaN(_petX)
            ? System.Math.Clamp((_petPanel.ClientSize.Width - PetFrameWidth) / 2f, minX, maxX)
            : System.Math.Clamp(_petX, minX, maxX);
    }

    private void PetPanel_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.Clear(SurfaceAlt);

        using (var pen = new Pen(BorderColor, 1f))
        {
            var zone = new Rectangle(0, _petGroundY, _petPanel.ClientSize.Width - 1, PetFrameHeight - 1);
            e.Graphics.DrawRectangle(pen, zone);
        }

        if (_petAtlas is null)
        {
            return;
        }

        ClampPetPosition();
        float jumpLift = _petJumpSequence?[_petJumpIndex].Lift * _petJumpPeak ?? 0f;
        var destination = new Rectangle(
            (int)System.Math.Round(_petX),
            _petGroundY - (int)System.Math.Round(jumpLift),
            PetFrameWidth,
            PetFrameHeight);
        var source = new Rectangle(
            _petFrame * PetFrameWidth,
            _petRow * PetFrameHeight,
            PetFrameWidth,
            PetFrameHeight);

        e.Graphics.DrawImage(_petAtlas, destination, source, GraphicsUnit.Pixel);
    }

    private Control CreateSection(string title, List<AppEntry> apps, bool includePetZone)
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
        int cardsHeight = System.Math.Max(cardHeight, rows * cardHeight);
        cardsPanel.Height = cardsHeight + (includePetZone ? PetFrameHeight : 0);

        if (includePetZone)
        {
            cardsPanel.BackColor = SurfaceAlt;
            SetPetHost(cardsPanel, cardsHeight);
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
            Width = System.Math.Max(PetFrameWidth, flpApps.ClientSize.Width - 34),
            Height = PetFrameHeight,
            Margin = new Padding(4, 0, 4, 0),
            BackColor = SurfaceAlt
        };

        SetPetHost(panel, 0);
        return panel;
    }

    private void SetPetHost(Panel panel, int groundY)
    {
        _petPanel = panel;
        _petGroundY = groundY;
        _petPanel.Paint += PetPanel_Paint;
        EnableDoubleBuffer(_petPanel);
        ClampPetPosition();
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

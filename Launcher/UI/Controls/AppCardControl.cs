using System;
using System.Drawing;
using System.Windows.Forms;

namespace Launcher.UI.Controls;

/// <summary>
/// RU: Карточка приложения в списке Launcher.
/// DE: Anwendungskarte in der Launcher-Liste.
/// </summary>
public partial class AppCardControl : UserControl
{
    #region [RU] Поля | [DE] Felder

    private Color _borderColor = Color.LightGray;
    private float _borderWidth = 1f;

    #endregion

    #region [RU] События | [DE] Ereignisse

    /// <summary>RU: Нажатие на Start. DE: Klick auf Start.</summary>
    public event EventHandler? StartClicked;

    /// <summary>RU: Нажатие на Ordner. DE: Klick auf Ordner.</summary>
    public event EventHandler? FolderClicked;

    /// <summary>RU: Нажатие на Path. DE: Klick auf Path.</summary>
    public event EventHandler? PathClicked;

    /// <summary>RU: Нажатие на Fav. DE: Klick auf Fav.</summary>
    public event EventHandler? FavoriteClicked;

    /// <summary>RU: Двойной клик по карточке. DE: Doppelklick auf Karte.</summary>
    public event EventHandler? CardDoubleClicked;

    #endregion

    #region [RU] Конструктор | [DE] Konstruktor

    /// <summary>
    /// RU: Инициализирует контрол и его внутренние события.
    /// DE: Initialisiert das Control und interne Events.
    /// </summary>
    public AppCardControl()
    {
        InitializeComponent();
        BindUiEvents();

        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.UserPaint,
            true);
    }

    #endregion

    #region [RU] Бизнес-логика | [DE] Fachlogik

    /// <summary>
    /// RU: Заполняет карточку данными приложения и применяет тему.
    /// DE: Befuellt die Karte mit App-Daten und wendet das Theme an.
    /// </summary>
    public void BindData(
        string title,
        string category,
        string file,
        string usage,
        bool executableExists,
        bool favorite,
        Color surface,
        Color surfaceAlt,
        Color textPrimary,
        Color textMuted,
        Color accent,
        Color border)
    {
        lblTitle.Text = title;
        lblCategory.Text = category;
        lblFile.Text = file;
        lblUsage.Text = usage;

        lblTitle.ForeColor = textPrimary;
        lblCategory.ForeColor = textMuted;
        lblUsage.ForeColor = textMuted;
        lblFile.ForeColor = executableExists ? textMuted : Color.FromArgb(196, 78, 78);

        _borderColor = favorite ? accent : border;
        _borderWidth = favorite ? 2f : 1f;

        btnStart.Enabled = executableExists;
        btnFavorite.Text = favorite ? "Fav-" : "Fav+";

        StyleButton(btnStart, true, accent, border, surfaceAlt, textPrimary);
        StyleButton(btnFolder, false, accent, border, surfaceAlt, textPrimary);
        StyleButton(btnPath, false, accent, border, surfaceAlt, textPrimary);
        StyleButton(btnFavorite, false, accent, border, surfaceAlt, textPrimary);

        BackColor = surface;
        Invalidate();
    }

    #endregion

    #region [RU] Вспомогательные методы | [DE] Hilfsmethoden

    private void StyleButton(Button button, bool primary, Color accent, Color border, Color surfaceAlt, Color textPrimary)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = primary ? accent : border;
        button.BackColor = primary ? accent : surfaceAlt;
        button.ForeColor = primary ? Color.White : textPrimary;
        button.Font = new Font("Segoe UI", 8, FontStyle.Bold);
    }

    private void BindUiEvents()
    {
        btnStart.Click += (_, __) => StartClicked?.Invoke(this, EventArgs.Empty);
        btnFolder.Click += (_, __) => FolderClicked?.Invoke(this, EventArgs.Empty);
        btnPath.Click += (_, __) => PathClicked?.Invoke(this, EventArgs.Empty);
        btnFavorite.Click += (_, __) => FavoriteClicked?.Invoke(this, EventArgs.Empty);

        DoubleClick += (_, __) => CardDoubleClicked?.Invoke(this, EventArgs.Empty);
        lblTitle.DoubleClick += (_, __) => CardDoubleClicked?.Invoke(this, EventArgs.Empty);
        lblCategory.DoubleClick += (_, __) => CardDoubleClicked?.Invoke(this, EventArgs.Empty);
        lblFile.DoubleClick += (_, __) => CardDoubleClicked?.Invoke(this, EventArgs.Empty);
        lblUsage.DoubleClick += (_, __) => CardDoubleClicked?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// RU: Рисует рамку карточки (обычную или акцентную).
    /// DE: Zeichnet den Kartenrahmen (normal oder hervorgehoben).
    /// </summary>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(_borderColor, _borderWidth);
        var rect = ClientRectangle;
        rect.Width -= 1;
        rect.Height -= 1;
        e.Graphics.DrawRectangle(pen, rect);
    }

    #endregion
}

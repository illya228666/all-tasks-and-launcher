using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Launcher.Pet;

namespace Launcher.UI;

public partial class Main
{
    #region [RU] Цветовая тема | [DE] Farbthema

    private Color FormBack => _isDarkTheme ? Color.FromArgb(24, 28, 34) : Color.FromArgb(240, 245, 252);
    private Color HeaderFrom => _isDarkTheme ? Color.FromArgb(16, 52, 78) : Color.FromArgb(38, 77, 126);
    private Color HeaderTo => _isDarkTheme ? Color.FromArgb(24, 91, 112) : Color.FromArgb(36, 123, 160);
    private Color HeaderBorder => _isDarkTheme ? Color.FromArgb(10, 35, 55) : Color.FromArgb(20, 50, 90);
    private Color TitleColor => _isDarkTheme ? Color.FromArgb(238, 246, 255) : Color.White;
    private Color SubColor => _isDarkTheme ? Color.FromArgb(174, 197, 214) : Color.FromArgb(218, 232, 245);
    private Color Surface => _isDarkTheme ? Color.FromArgb(31, 37, 45) : Color.FromArgb(252, 253, 255);
    private Color SurfaceAlt => _isDarkTheme ? Color.FromArgb(37, 43, 53) : Color.FromArgb(246, 249, 253);
    private Color BorderColor => _isDarkTheme ? Color.FromArgb(67, 80, 94) : Color.FromArgb(196, 210, 225);
    private Color TextPrimary => _isDarkTheme ? Color.FromArgb(231, 236, 243) : Color.FromArgb(26, 33, 41);
    private Color TextMuted => _isDarkTheme ? Color.FromArgb(163, 172, 183) : Color.FromArgb(82, 92, 104);
    private Color Accent => _isDarkTheme ? Color.FromArgb(72, 170, 255) : Color.FromArgb(24, 107, 191);

    #endregion

    #region [RU] Бизнес-логика | [DE] Fachlogik

    /// <summary>
    /// RU: Применяет выбранную тему к элементам формы.
    /// DE: Wendet das ausgewaehlte Theme auf alle Form-Elemente an.
    /// </summary>
    private void ApplyTheme()
    {
        BackColor = FormBack;
        flpApps.BackColor = FormBack;

        _lblTitle.ForeColor = TitleColor;
        _lblSub.ForeColor = SubColor;
        _lblStats.ForeColor = TextPrimary;
        _lblHint.ForeColor = TextMuted;

        StyleInput(_txtSearch);
        StyleInput(_cbCategory);
        StyleInput(_cbSort);
        StyleCheck(_chkFavoritesOnly);
        StyleCheck(_chkAvailableOnly);

        StyleButton(_btnRefresh, true);
        StyleButton(_btnSurprise, true);
        StyleButton(_btnRoot, false);
        StyleButton(_btnTheme, false);

        _btnTheme.Text = _isDarkTheme ? "Theme: Dark" : "Theme: Light";
        _header.Invalidate();
        _pet.ApplyTheme(new PetTheme(SurfaceAlt, Surface, TextPrimary, BorderColor, _isDarkTheme));
        // Перекраска не начинает реплику заново и сохраняет прокрутку к питомцу.
        Render(preservePetSpeech: true);
    }

    #endregion

    #region [RU] Вспомогательные методы | [DE] Hilfsmethoden

    private void Header_Paint(object? sender, PaintEventArgs e)
    {
        Rectangle rect = _header.ClientRectangle;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        using var brush = new LinearGradientBrush(rect, HeaderFrom, HeaderTo, 0f);
        e.Graphics.FillRectangle(brush, rect);

        using var pen = new Pen(HeaderBorder, 1f);
        e.Graphics.DrawLine(pen, 0, rect.Height - 1, rect.Width, rect.Height - 1);
    }

    private void StyleInput(Control control)
    {
        control.Font = new Font("Segoe UI", 9);
        control.BackColor = Surface;
        control.ForeColor = TextPrimary;
        control.Margin = new Padding(4, 0, 4, 0);
    }

    private void StyleCheck(CheckBox checkBox)
    {
        checkBox.Font = new Font("Segoe UI", 9);
        checkBox.ForeColor = TitleColor;
        checkBox.BackColor = Color.Transparent;
        checkBox.Margin = new Padding(4, 0, 4, 0);
    }

    private void StyleButton(Button button, bool accent)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = accent ? Accent : BorderColor;
        button.BackColor = accent ? Accent : Surface;
        button.ForeColor = accent ? Color.White : TextPrimary;
        button.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        button.Padding = new Padding(6, 2, 6, 2);
        button.Margin = new Padding(4, 0, 4, 0);
    }

    #endregion
}

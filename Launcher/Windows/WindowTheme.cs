namespace Launcher.Windows;
internal sealed record WindowTheme(bool Dark)
{
    internal Color Background => Dark ? Color.FromArgb(24, 28, 34) : Color.FromArgb(240, 245, 252);
    internal Color Surface => Dark ? Color.FromArgb(31, 37, 45) : Color.FromArgb(252, 253, 255);
    internal Color SurfaceAlt => Dark ? Color.FromArgb(37, 43, 53) : Color.FromArgb(246, 249, 253);
    internal Color Border => Dark ? Color.FromArgb(67, 80, 94) : Color.FromArgb(196, 210, 225);
    internal Color Text => Dark ? Color.FromArgb(231, 236, 243) : Color.FromArgb(26, 33, 41);
    internal Color Muted => Dark ? Color.FromArgb(163, 172, 183) : Color.FromArgb(82, 92, 104);
    internal Color Accent => Dark ? Color.FromArgb(72, 170, 255) : Color.FromArgb(24, 107, 191);
    internal Color Header => Dark ? Color.FromArgb(16, 52, 78) : Color.FromArgb(38, 77, 126);

    internal void Apply(Control control)
    {
        control.ForeColor = Text;
        control.BackColor = control is TextBox or ComboBox or Button ? Surface : Background;
        if (control is Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Border;
            button.Padding = new Padding(4, 2, 4, 2);
        }

        foreach (Control child in control.Controls)
            Apply(child);
    }
}

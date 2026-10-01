using System.Drawing.Drawing2D;
using Launcher.Pet.Life;

namespace Launcher.Pet.Windows.Drawing;

internal static class LifeDrawing
{
    private static readonly Lazy<RuinArtCatalog> Art = new(() => new RuinArtCatalog("life:"));
    internal static void DrawItem(Graphics g, LifeItemKind kind, PointF center, int mass, double seconds, float diameter = 18, float decomposition = 0)
    {
        var art = Art.Value.Find(kind == LifeItemKind.Spore ? "life:spore" : "life:remains");
        if (art is not null)
        {
            float pulse = kind == LifeItemKind.Spore ? 1 + .06f * (float)Math.Sin(seconds * 2) : Math.Clamp(mass / 1000f, .7f, 1.1f) * (1 - Math.Clamp(decomposition, 0, 1) * .55f);
            float width = diameter * pulse;
            float height = width * art.Image.Height / art.Image.Width;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(art.Image, new RectangleF(center.X - width / 2, center.Y - height / 2, width, height));
            return;
        }
        var saved = g.Save();
        g.TranslateTransform(center.X, center.Y); g.ScaleTransform(diameter / 18, diameter / 18);
        center = PointF.Empty;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        if (kind == LifeItemKind.Spore)
        {
            float pulse = 0.75f + 0.25f * (float)Math.Sin(seconds * 2);
            for (int radius = 9; radius >= 3; radius -= 3)
            {
                using var glow = new SolidBrush(Color.FromArgb((int)((radius == 3 ? 235 : 30) * pulse), 129, 241, 215));
                g.FillEllipse(glow, center.X - radius, center.Y - radius, radius * 2, radius * 2);
            }
            using var white = new SolidBrush(Color.FromArgb(240, 224, 255, 230));
            g.FillEllipse(white, center.X - 1.5f, center.Y - 2, 3, 4);
        }
        else
        {
            float size = Math.Clamp(mass / 250f, 3, 11);
            using var shadow = new SolidBrush(Color.FromArgb(180, 44, 30, 36));
            g.FillEllipse(shadow, center.X - size, center.Y - 2, size * 2, 5);
            using var leaf = new Pen(Color.FromArgb(230, 169, 117, 73), 2);
            using var vein = new Pen(Color.FromArgb(235, 213, 165, 104), 1);
            for (int i = 0; i < 5; i++)
            {
                float x = center.X + (i - 2) * size / 3;
                float y = center.Y - (i % 2 == 0 ? 3 : 6);
                g.DrawLine(leaf, x - 3, y + 3, x + 2, y);
                g.DrawLine(vein, x - 1, y + 2, x + 1, y + 1);
            }
        }
        g.Restore(saved);
    }
}

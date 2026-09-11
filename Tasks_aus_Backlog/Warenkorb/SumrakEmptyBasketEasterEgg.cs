using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Warenkorb
{
    /// <summary>
    /// Rein visuelle Empty-State-Komponente für den Warenkorb.
    /// Sie enthält bewusst keinerlei Abhängigkeit zur Warenkorb-Domäne oder zum Launcher:
    /// die Sumrak-Referenz bleibt damit ein austauschbares UI-Detail.
    /// </summary>
    internal sealed class SumrakEmptyBasketEasterEgg : Control
    {
        private static readonly Color Ink = Color.FromArgb(34, 40, 37);
        private static readonly Color Muted = Color.FromArgb(104, 111, 105);
        private static readonly Color Accent = Color.FromArgb(211, 76, 34);
        private static readonly Color HoverBackground = Color.FromArgb(250, 249, 246);

        private bool hovered;
        private bool discovered;

        /// <summary>
        /// Wird ausgelöst, wenn die Referenz per Maus oder Tastatur entdeckt wurde.
        /// Die aufrufende Form entscheidet selbst, wie sie darauf reagiert.
        /// </summary>
        public event EventHandler Discovered;

        public SumrakEmptyBasketEasterEgg()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);

            BackColor = Color.White;
            Cursor = Cursors.Hand;
            Height = 112;
            MinimumSize = new Size(220, 112);
            Margin = new Padding(0, 12, 0, 0);
            TabStop = true;

            AccessibleName = "Verlorener Hut";
            AccessibleDescription = "Ein kleines verstecktes Detail im leeren Warenkorb.";
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(hovered ? HoverBackground : BackColor);

            Rectangle hatBounds = new Rectangle(12, 17, 74, 68);
            DrawHat(e.Graphics, hatBounds, hovered ? Accent : Ink);

            Rectangle titleBounds = new Rectangle(96, 18, Math.Max(1, ClientSize.Width - 108), 48);
            Rectangle hintBounds = new Rectangle(96, 67, Math.Max(1, ClientSize.Width - 108), 28);

            using (Font titleFont = new Font(Font, FontStyle.Bold))
            using (Font hintFont = new Font(Font.FontFamily, Math.Max(8F, Font.Size - 1F), FontStyle.Regular))
            {
                TextRenderer.DrawText(
                    e.Graphics,
                    "Irgendjemand hat hier seinen Hut verloren.",
                    titleFont,
                    titleBounds,
                    Ink,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);

                TextRenderer.DrawText(
                    e.Graphics,
                    discovered ? "Besitzer zuletzt im Launcher gesehen." : "Fundstück ohne Besitzer.",
                    hintFont,
                    hintBounds,
                    Muted,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            if (Focused && ShowFocusCues)
            {
                Rectangle focusBounds = ClientRectangle;
                focusBounds.Inflate(-2, -2);
                ControlPaint.DrawFocusRectangle(e.Graphics, focusBounds);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            Focus();
            base.OnMouseDown(e);
        }

        protected override void OnClick(EventArgs e)
        {
            Discover();
            base.OnClick(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                Discover();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }

            base.OnKeyDown(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate();
            base.OnLostFocus(e);
        }

        private void Discover()
        {
            discovered = true;
            Invalidate();

            EventHandler handler = Discovered;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private static void DrawHat(Graphics graphics, Rectangle bounds, Color color)
        {
            GraphicsState state = graphics.Save();
            try
            {
                graphics.TranslateTransform(bounds.Left + bounds.Width / 2F, bounds.Top + bounds.Height / 2F);
                graphics.RotateTransform(-8F);
                graphics.TranslateTransform(-bounds.Width / 2F, -bounds.Height / 2F);

                using (SolidBrush hatBrush = new SolidBrush(color))
                using (SolidBrush bandBrush = new SolidBrush(Accent))
                using (GraphicsPath crown = new GraphicsPath())
                {
                    graphics.FillEllipse(hatBrush, 3F, 46F, 68F, 14F);

                    crown.StartFigure();
                    crown.AddBezier(18F, 48F, 18F, 34F, 20F, 11F, 35F, 8F);
                    crown.AddBezier(35F, 8F, 53F, 7F, 55F, 31F, 57F, 48F);
                    crown.CloseFigure();
                    graphics.FillPath(hatBrush, crown);

                    graphics.FillRectangle(bandBrush, 20F, 38F, 35F, 5F);
                }
            }
            finally
            {
                graphics.Restore(state);
            }
        }
    }
}

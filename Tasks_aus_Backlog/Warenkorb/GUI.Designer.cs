using System.Drawing;
using System.Windows.Forms;

namespace Warenkorb
{
    partial class GUI
    {
        private System.ComponentModel.IContainer components = null;

        // Основные цвета интерфейса: тёплый фон, тёмный текст и оранжевый акцент.
        private static readonly Color Paper = Color.FromArgb(247, 245, 240);
        private static readonly Color Ink = Color.FromArgb(34, 40, 37);
        private static readonly Color Muted = Color.FromArgb(104, 111, 105);
        private static readonly Color Accent = Color.FromArgb(211,  76,  34);
        private readonly FlowLayoutPanel catalog = new FlowLayoutPanel();
        private readonly FlowLayoutPanel basketRows = new FlowLayoutPanel();
        private readonly Label count = new Label();
        private readonly Label status = new Label();
        private readonly Label capacity = new Label();
        private readonly ProgressBar meter = new ProgressBar();
        private readonly TextBox search = new TextBox();
        private readonly Button removeLast = new Button();
        private readonly Button sort = new Button();

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            Name = "GUI";
            IsMdiContainer = false;
            Text = "Warenkorb | MARKET STUDIO";
            BackColor = Paper;
            ForeColor = Ink;
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(1180, 780);
            MinimumSize = new Size(1060, 740);
            StartPosition = FormStartPosition.CenterScreen;
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(32, 20, 32, 18), RowCount = 4, ColumnCount = 1 };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            Controls.Add(root);

            var nav = new Panel { Dock = DockStyle.Fill };
            nav.Controls.Add(TextLabel("M /  MARKET STUDIO", 15, FontStyle.Bold, Ink, new Rectangle(0, 6, 300, 35)));
            var tag = TextLabel("DEIN ALLTAG. GUT SORTIERT.", 9, FontStyle.Bold, Muted, new Rectangle(0, 8, 310, 30));
            tag.Dock = DockStyle.Right;
            tag.TextAlign = ContentAlignment.MiddleRight;
            nav.Controls.Add(tag);
            root.Controls.Add(nav, 0, 0);

            var hero = new Panel { Dock = DockStyle.Fill, BackColor = Ink, Margin = new Padding(0, 0, 0, 22) };
            hero.Controls.Add(TextLabel("WENIGER SUCHEN. MEHR FINDEN.", 9, FontStyle.Bold, Color.FromArgb(209, 224, 186), new Rectangle(25, 16, 450, 22)));
            hero.Controls.Add(TextLabel("Gute Dinge. Dein Warenkorb.", 28, FontStyle.Bold, Color.White, new Rectangle(22, 40, 790, 55)));
            hero.Controls.Add(TextLabel("Entdecken, hinzufügen und alles im Blick behalten.", 11, FontStyle.Regular, Color.FromArgb(208, 213, 208), new Rectangle(26, 98, 720, 26)));
            root.Controls.Add(hero, 0, 1);

            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 350));
            root.Controls.Add(body, 0, 2);
            var shop = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = new Padding(0, 0, 22, 0) };
            shop.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            shop.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            shop.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            shop.Controls.Add(TextLabel("Das Sortiment", 20, FontStyle.Bold, Ink, Rectangle.Empty), 0, 0);
            search.Dock = DockStyle.Fill;
            search.Margin = new Padding(0, 2, 3, 12);
            search.Font = new Font("Segoe UI", 12);
            search.AccessibleName = "Produkte durchsuchen";
            var searchBox = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = Padding.Empty };
            searchBox.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            searchBox.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            searchBox.Controls.Add(TextLabel("Suchen", 10, FontStyle.Regular, Muted, Rectangle.Empty), 0, 0);
            searchBox.Controls.Add(search, 1, 0);
            shop.Controls.Add(searchBox, 0, 1);
            catalog.Dock = DockStyle.Fill;
            catalog.AutoScroll = true;
            catalog.Margin = Padding.Empty;
            shop.Controls.Add(catalog, 0, 2);
            body.Controls.Add(shop, 0, 0);

            var summary = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(20), ColumnCount = 1, RowCount = 7, Margin = Padding.Empty };
            foreach (int height in new[] { 42, 30, 0, 28, 24, 48, 44 })
                summary.RowStyles.Add(new RowStyle(height == 0 ? SizeType.Percent : SizeType.Absolute, height == 0 ? 100 : height));
            summary.Controls.Add(TextLabel("Dein Warenkorb", 18, FontStyle.Bold, Ink, Rectangle.Empty), 0, 0);
            count.Dock = DockStyle.Fill;
            count.ForeColor = Muted;
            summary.Controls.Add(count, 0, 1);
            basketRows.Dock = DockStyle.Fill;
            basketRows.AutoScroll = true;
            basketRows.FlowDirection = FlowDirection.TopDown;
            basketRows.WrapContents = false;
            basketRows.Margin = new Padding(0, 8, 0, 8);
            summary.Controls.Add(basketRows, 0, 2);
            capacity.Dock = DockStyle.Fill;
            capacity.ForeColor = Muted;
            summary.Controls.Add(capacity, 0, 3);
            meter.Dock = DockStyle.Top;
            meter.Height = 8;
            summary.Controls.Add(meter, 0, 4);
            StyleButton(sort, "A–Z sortieren", Accent, Color.White);
            sort.Dock = DockStyle.Fill;
            summary.Controls.Add(sort, 0, 5);
            StyleButton(removeLast, "Letzten Artikel entfernen", Paper, Ink);
            removeLast.Dock = DockStyle.Fill;
            summary.Controls.Add(removeLast, 0, 6);
            body.Controls.Add(summary, 1, 0);
            status.Dock = DockStyle.Fill;
            status.TextAlign = ContentAlignment.BottomLeft;
            status.ForeColor = Muted;
            root.Controls.Add(status, 0, 3);
            ResumeLayout(false);
        }

        private static Label TextLabel(string text, float size, FontStyle style, Color color, Rectangle bounds)
        {
            var label = new Label { Text = text, Font = new Font("Segoe UI", size, style), ForeColor = color, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
            if (bounds.IsEmpty) label.Dock = DockStyle.Fill;
            else label.Bounds = bounds;
            return label;
        }

        private static void StyleButton(Button button, string text, Color background, Color foreground)
        {
            button.Text = text;
            button.BackColor = background;
            button.ForeColor = foreground;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(background, .12F);
            button.Cursor = Cursors.Hand;
            button.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            button.UseVisualStyleBackColor = false;
        }

    }
}

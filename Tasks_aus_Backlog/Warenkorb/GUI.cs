using System;
using System.Drawing;
using System.Windows.Forms;

namespace Warenkorb
{
    public partial class GUI : Form
    {
        // Основные цвета интерфейса: тёплый фон, тёмный текст и оранжевый акцент.
        private static readonly Color Paper = Color.FromArgb(247, 245, 240);
        private static readonly Color Ink = Color.FromArgb(34, 40, 37);
        private static readonly Color Muted = Color.FromArgb(104, 111, 105);
        private static readonly Color Accent = Color.FromArgb(211, 76, 34);

        private readonly Warenkorb basket = new Warenkorb(10);

        public GUI()
        {
            InitializeComponent();
        }

        private void GUI_Load(object sender, EventArgs e)
        {
            meter.Maximum = basket.Size;
            RenderCatalog();
            RefreshBasket("Bereit für deine Auswahl. Drei Beispielartikel sind schon dabei.");
        }

        private void addProduct_Click(object sender, EventArgs e)
        {
            string product = productInput.Text.Trim();
            if (product.Length == 0)
            {
                status.Text = "● Bitte einen Produktnamen eingeben.";
                productInput.Focus();
                return;
            }
            if (basket.Add(product) < 0)
            {
                status.Text = "● Dein Warenkorb ist voll. Entferne zuerst einen Artikel.";
                return;
            }
            RefreshBasket(product + " hinzugefügt.");
            productInput.Clear();
            productInput.Focus();
        }

        private void productInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            addProduct.PerformClick();
        }

        private void search_TextChanged(object sender, EventArgs e)
        {
            RenderCatalog();
        }

        private void basketRows_SizeChanged(object sender, EventArgs e)
        {
            ResizeBasketRows();
        }

        private void sort_Click(object sender, EventArgs e)
        {
            basket.Sort();
            RefreshBasket("Alles alphabetisch sortiert.");
        }

        private void removeLast_Click(object sender, EventArgs e)
        {
            basket.DeleteLast();
            RefreshBasket("Letzten Artikel entfernt.");
        }

        private void RenderCatalog()
        {
            catalog.SuspendLayout();
            while (catalog.Controls.Count > 0) catalog.Controls[0].Dispose();
            string query = search.Text.Trim();
            foreach (string product in PossibleProducts.Get())
            {
                if ((product).IndexOf(query, StringComparison.CurrentCultureIgnoreCase) < 0) continue;
                var card = new Panel { Size = new Size(198, 194), BackColor = Color.White, Margin = new Padding(0, 0, 12, 12) };
                var artwork = new Panel { Bounds = new Rectangle(10, 10, 178, 80), BackColor = Color.FromArgb(236, 239, 226) };
                var initial = TextLabel(product.ToUpperInvariant(), 32, FontStyle.Bold, Ink, Rectangle.Empty);
                initial.TextAlign = ContentAlignment.MiddleCenter;
                artwork.Controls.Add(initial);
                card.Controls.Add(artwork);
                card.Controls.Add(TextLabel(product.ToUpperInvariant(), 12, FontStyle.Bold, Ink, new Rectangle(12, 96, 174, 26)));
                var add = new Button { Bounds = new Rectangle(10, 139, 178, 40), AccessibleName = product + " hinzufügen" };
                StyleButton(add, "+  Hinzufügen", Paper, Ink);
                add.Click += (sender, args) =>
                {
                    int index = basket.Add(product);
                    RefreshBasket(index < 0 ? "Dein Warenkorb ist voll. Entferne zuerst einen Artikel." : product.ToUpperInvariant() + " hinzugefügt.");
                };
                card.Controls.Add(add);
                catalog.Controls.Add(card);
            }
            if (catalog.Controls.Count == 0)
                catalog.Controls.Add(new Label { Text = "Keine Artikel gefunden. Versuche einen anderen Suchbegriff.", AutoSize = true, ForeColor = Muted, MaximumSize = new Size(450, 0), Padding = new Padding(0, 20, 0, 0) });
            catalog.ResumeLayout();
        }

        private void RefreshBasket(string message)
        {
            basketRows.SuspendLayout();
            while (basketRows.Controls.Count > 0) basketRows.Controls[0].Dispose();
            int occupied = 0;
            for (int i = 0; i < basket.Content.Length; i++)
            {
                if (string.IsNullOrEmpty(basket.Content[i])) continue;
                occupied++;
                int index = i;
                var row = new Panel { Height = 50, BackColor = Paper, Margin = new Padding(0, 0, 0, 7) };
                var delete = new Button { Dock = DockStyle.Right, Width = 38, AccessibleName = basket.Content[i] + " entfernen" };
                StyleButton(delete, "×", Paper, Muted);
                delete.Click += (sender, args) => { basket.Delete(index); RefreshBasket("Artikel entfernt."); };
                var name = TextLabel("  " + occupied.ToString("00") + @" '" + basket.Content[i] + @"'", 11, FontStyle.Bold, Ink, Rectangle.Empty);
                name.UseMnemonic = false;
                row.Controls.Add(name);
                row.Controls.Add(delete);
                basketRows.Controls.Add(row);
            }

            if (occupied == 0)
                RenderEmptyBasketState();

            count.Text = occupied + " Artikel · deine Auswahl";
            capacity.Text = occupied + " von " + basket.Size + " Plätzen belegt";
            meter.Value = occupied;
            removeLast.Enabled = occupied > 0;
            sort.Enabled = occupied > 1;
            status.Text = "●  " + message;
            ResizeBasketRows();
            basketRows.ResumeLayout();
        }

        /// <summary>
        /// Baut den leeren Zustand ausschließlich aus Präsentationskomponenten auf.
        /// Die Sumrak-Referenz bleibt damit vom Warenkorb-Modell vollständig getrennt.
        /// </summary>
        private void RenderEmptyBasketState()
        {
            var emptyState = new SumrakEmptyBasketEasterEgg();
            emptyState.Discovered += SumrakEmptyState_Discovered;
            basketRows.Controls.Add(emptyState);
        }

        private void SumrakEmptyState_Discovered(object sender, EventArgs e)
        {
            status.Text = "●  Besitzer zuletzt im Launcher gesehen.";
        }

        private void ResizeBasketRows()
        {
            foreach (Control row in basketRows.Controls)
                row.Width = Math.Max(100, basketRows.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 2);
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

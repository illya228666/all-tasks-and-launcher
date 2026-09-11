using System;
using System.Drawing;
using System.Windows.Forms;

namespace Warenkorb
{
    public partial class GUI : Form
    {
        private readonly Warenkorb basket = new Warenkorb(10);

        public GUI()
        {
            InitializeComponent();
            meter.Maximum = basket.Size;
            search.TextChanged += (sender, args) => RenderCatalog();
            basketRows.SizeChanged += (sender, args) => ResizeBasketRows();
            sort.Click += (sender, args) => { basket.Sort(); RefreshBasket("Alles alphabetisch sortiert."); };
            removeLast.Click += (sender, args) => { basket.DeleteLast(); RefreshBasket("Letzten Artikel entfernt."); };
            RenderCatalog();
            RefreshBasket("Bereit für deine Auswahl. Drei Beispielartikel sind schon dabei.");
        }

        private void RenderCatalog()
        {
            catalog.SuspendLayout();
            while (catalog.Controls.Count > 0) catalog.Controls[0].Dispose();
            string query = search.Text.Trim();
            foreach (string product in PossibleProducts.Get())
            {
                if (("Artikel " + product).IndexOf(query, StringComparison.CurrentCultureIgnoreCase) < 0) continue;
                var card = new Panel { Size = new Size(198, 194), BackColor = Color.White, Margin = new Padding(0, 0, 12, 12) };
                var artwork = new Panel { Bounds = new Rectangle(10, 10, 178,  80), BackColor = Color.FromArgb(236, 239, 226) };
                var initial = TextLabel(product.ToUpperInvariant(), 32, FontStyle.Bold, Ink, Rectangle.Empty);
                initial.TextAlign = ContentAlignment.MiddleCenter;
                artwork.Controls.Add(initial);
                card.Controls.Add(artwork);
                card.Controls.Add(TextLabel("Artikel " + product.ToUpperInvariant(), 12, FontStyle.Bold, Ink, new Rectangle(12, 96, 174, 26)));
                var add = new Button { Bounds = new Rectangle(10, 139, 178,  40), AccessibleName = "Artikel " + product + " hinzufügen" };
                StyleButton(add, "+  Hinzufügen", Paper, Ink);
                add.Click += (sender, args) =>
                {
                    int index = basket.Add(product);
                    RefreshBasket(index < 0 ? "Dein Warenkorb ist voll. Entferne zuerst einen Artikel." : "Artikel " + product.ToUpperInvariant() + " hinzugefügt.");
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
                var delete = new Button { Dock = DockStyle.Right, Width = 38, AccessibleName = "Artikel " + basket.Content[i] + " entfernen" };
                StyleButton(delete, "×", Paper, Muted);
                delete.Click += (sender, args) => { basket.Delete(index); RefreshBasket("Artikel entfernt."); };
                var name = TextLabel("  " + occupied.ToString("00") + "    Artikel " + basket.Content[i].ToUpperInvariant(), 11, FontStyle.Bold, Ink, Rectangle.Empty);
                row.Controls.Add(name);
                row.Controls.Add(delete);
                basketRows.Controls.Add(row);
            }
            if (occupied == 0)
                basketRows.Controls.Add(new Label { Text = "Noch ganz viel Platz.\nWähle links deinen ersten Artikel.", ForeColor = Muted, Height =  80, Margin = new Padding(0, 20, 0, 0) });
            count.Text = occupied + " Artikel · deine Auswahl";
            capacity.Text = occupied + " von " + basket.Size + " Plätzen belegt";
            meter.Value = occupied;
            removeLast.Enabled = occupied > 0;
            sort.Enabled = occupied > 1;
            status.Text = "●  " + message;
            ResizeBasketRows();
            basketRows.ResumeLayout();
        }

        private void ResizeBasketRows()
        {
            foreach (Control row in basketRows.Controls)
                row.Width = Math.Max(100, basketRows.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 2);
        }
    }
}

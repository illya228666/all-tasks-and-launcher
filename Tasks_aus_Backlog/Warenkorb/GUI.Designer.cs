namespace Warenkorb
{
    partial class GUI
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.root = new System.Windows.Forms.TableLayoutPanel();
            this.nav = new System.Windows.Forms.Panel();
            this.brandLabel = new System.Windows.Forms.Label();
            this.tagLabel = new System.Windows.Forms.Label();
            this.hero = new System.Windows.Forms.Panel();
            this.eyebrowLabel = new System.Windows.Forms.Label();
            this.headlineLabel = new System.Windows.Forms.Label();
            this.introLabel = new System.Windows.Forms.Label();
            this.body = new System.Windows.Forms.TableLayoutPanel();
            this.shop = new System.Windows.Forms.TableLayoutPanel();
            this.catalogTitle = new System.Windows.Forms.Label();
            this.searchBox = new System.Windows.Forms.TableLayoutPanel();
            this.searchLabel = new System.Windows.Forms.Label();
            this.search = new System.Windows.Forms.TextBox();
            this.catalog = new System.Windows.Forms.FlowLayoutPanel();
            this.productEntry = new System.Windows.Forms.Panel();
            this.productInput = new System.Windows.Forms.TextBox();
            this.addProduct = new System.Windows.Forms.Button();
            this.productLabel = new System.Windows.Forms.Label();
            this.summary = new System.Windows.Forms.TableLayoutPanel();
            this.basketTitle = new System.Windows.Forms.Label();
            this.count = new System.Windows.Forms.Label();
            this.basketRows = new System.Windows.Forms.FlowLayoutPanel();
            this.capacity = new System.Windows.Forms.Label();
            this.meter = new System.Windows.Forms.ProgressBar();
            this.sort = new System.Windows.Forms.Button();
            this.removeLast = new System.Windows.Forms.Button();
            this.status = new System.Windows.Forms.Label();
            this.root.SuspendLayout();
            this.nav.SuspendLayout();
            this.hero.SuspendLayout();
            this.body.SuspendLayout();
            this.shop.SuspendLayout();
            this.searchBox.SuspendLayout();
            this.productEntry.SuspendLayout();
            this.summary.SuspendLayout();
            this.SuspendLayout();
            // 
            // root
            // 
            this.root.ColumnCount = 1;
            this.root.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.root.Controls.Add(this.nav, 0, 0);
            this.root.Controls.Add(this.hero, 0, 1);
            this.root.Controls.Add(this.body, 0, 2);
            this.root.Controls.Add(this.status, 0, 3);
            this.root.Dock = System.Windows.Forms.DockStyle.Fill;
            this.root.Location = new System.Drawing.Point(0, 0);
            this.root.Name = "root";
            this.root.Padding = new System.Windows.Forms.Padding(32, 20, 32, 18);
            this.root.RowCount = 4;
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 160F));
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.root.Size = new System.Drawing.Size(1180, 780);
            this.root.TabIndex = 0;
            // 
            // nav
            // 
            this.nav.Controls.Add(this.brandLabel);
            this.nav.Controls.Add(this.tagLabel);
            this.nav.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nav.Location = new System.Drawing.Point(35, 23);
            this.nav.Name = "nav";
            this.nav.Size = new System.Drawing.Size(1110, 54);
            this.nav.TabIndex = 1;
            // 
            // brandLabel
            // 
            this.brandLabel.AutoEllipsis = true;
            this.brandLabel.Font = new System.Drawing.Font("Segoe UI", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.brandLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(40)))), ((int)(((byte)(37)))));
            this.brandLabel.Location = new System.Drawing.Point(0, 6);
            this.brandLabel.Name = "brandLabel";
            this.brandLabel.Size = new System.Drawing.Size(300, 35);
            this.brandLabel.TabIndex = 2;
            this.brandLabel.Text = "M /  MARKET STUDIO";
            this.brandLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tagLabel
            // 
            this.tagLabel.AutoEllipsis = true;
            this.tagLabel.Dock = System.Windows.Forms.DockStyle.Right;
            this.tagLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tagLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(104)))), ((int)(((byte)(111)))), ((int)(((byte)(105)))));
            this.tagLabel.Location = new System.Drawing.Point(800, 0);
            this.tagLabel.Name = "tagLabel";
            this.tagLabel.Size = new System.Drawing.Size(310, 54);
            this.tagLabel.TabIndex = 3;
            this.tagLabel.Text = "DEIN ALLTAG. GUT SORTIERT.";
            this.tagLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // hero
            // 
            this.hero.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(40)))), ((int)(((byte)(37)))));
            this.hero.Controls.Add(this.eyebrowLabel);
            this.hero.Controls.Add(this.headlineLabel);
            this.hero.Controls.Add(this.introLabel);
            this.hero.Dock = System.Windows.Forms.DockStyle.Fill;
            this.hero.Location = new System.Drawing.Point(32, 80);
            this.hero.Margin = new System.Windows.Forms.Padding(0, 0, 0, 22);
            this.hero.Name = "hero";
            this.hero.Size = new System.Drawing.Size(1116, 138);
            this.hero.TabIndex = 4;
            // 
            // eyebrowLabel
            // 
            this.eyebrowLabel.AutoEllipsis = true;
            this.eyebrowLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.eyebrowLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(209)))), ((int)(((byte)(224)))), ((int)(((byte)(186)))));
            this.eyebrowLabel.Location = new System.Drawing.Point(25, 16);
            this.eyebrowLabel.Name = "eyebrowLabel";
            this.eyebrowLabel.Size = new System.Drawing.Size(450, 22);
            this.eyebrowLabel.TabIndex = 5;
            this.eyebrowLabel.Text = "WENIGER SUCHEN. MEHR FINDEN.";
            this.eyebrowLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // headlineLabel
            // 
            this.headlineLabel.AutoEllipsis = true;
            this.headlineLabel.Font = new System.Drawing.Font("Segoe UI", 28F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.headlineLabel.ForeColor = System.Drawing.Color.White;
            this.headlineLabel.Location = new System.Drawing.Point(22, 40);
            this.headlineLabel.Name = "headlineLabel";
            this.headlineLabel.Size = new System.Drawing.Size(790, 55);
            this.headlineLabel.TabIndex = 6;
            this.headlineLabel.Text = "Gute Dinge. Dein Warenkorb.";
            this.headlineLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // introLabel
            // 
            this.introLabel.AutoEllipsis = true;
            this.introLabel.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.introLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(213)))), ((int)(((byte)(208)))));
            this.introLabel.Location = new System.Drawing.Point(26, 98);
            this.introLabel.Name = "introLabel";
            this.introLabel.Size = new System.Drawing.Size(720, 26);
            this.introLabel.TabIndex = 7;
            this.introLabel.Text = "Entdecken, hinzufügen und alles im Blick behalten.";
            this.introLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // body
            // 
            this.body.ColumnCount = 2;
            this.body.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.body.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 350F));
            this.body.Controls.Add(this.shop, 0, 0);
            this.body.Controls.Add(this.summary, 1, 0);
            this.body.Dock = System.Windows.Forms.DockStyle.Fill;
            this.body.Location = new System.Drawing.Point(32, 240);
            this.body.Margin = new System.Windows.Forms.Padding(0);
            this.body.Name = "body";
            this.body.RowCount = 1;
            this.body.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.body.Size = new System.Drawing.Size(1116, 484);
            this.body.TabIndex = 8;
            // 
            // shop
            // 
            this.shop.ColumnCount = 1;
            this.shop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.shop.Controls.Add(this.catalogTitle, 0, 0);
            this.shop.Controls.Add(this.searchBox, 0, 1);
            this.shop.Controls.Add(this.catalog, 0, 3);
            this.shop.Controls.Add(this.productEntry, 0, 2);
            this.shop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.shop.Location = new System.Drawing.Point(0, 0);
            this.shop.Margin = new System.Windows.Forms.Padding(0, 0, 22, 0);
            this.shop.Name = "shop";
            this.shop.RowCount = 4;
            this.shop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.shop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.shop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.shop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.shop.Size = new System.Drawing.Size(744, 484);
            this.shop.TabIndex = 9;
            // 
            // catalogTitle
            // 
            this.catalogTitle.AutoEllipsis = true;
            this.catalogTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.catalogTitle.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.catalogTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(40)))), ((int)(((byte)(37)))));
            this.catalogTitle.Location = new System.Drawing.Point(3, 0);
            this.catalogTitle.Name = "catalogTitle";
            this.catalogTitle.Size = new System.Drawing.Size(738, 48);
            this.catalogTitle.TabIndex = 10;
            this.catalogTitle.Text = "Das Sortiment";
            this.catalogTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.catalogTitle.Visible = false;
            // 
            // searchBox
            // 
            this.searchBox.ColumnCount = 2;
            this.searchBox.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.searchBox.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.searchBox.Controls.Add(this.searchLabel, 0, 0);
            this.searchBox.Controls.Add(this.search, 1, 0);
            this.searchBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.searchBox.Location = new System.Drawing.Point(0, 48);
            this.searchBox.Margin = new System.Windows.Forms.Padding(0);
            this.searchBox.Name = "searchBox";
            this.searchBox.RowCount = 1;
            this.searchBox.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.searchBox.Size = new System.Drawing.Size(744, 48);
            this.searchBox.TabIndex = 11;
            // 
            // searchLabel
            // 
            this.searchLabel.AutoEllipsis = true;
            this.searchLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.searchLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.searchLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(104)))), ((int)(((byte)(111)))), ((int)(((byte)(105)))));
            this.searchLabel.Location = new System.Drawing.Point(3, 0);
            this.searchLabel.Name = "searchLabel";
            this.searchLabel.Size = new System.Drawing.Size(74, 48);
            this.searchLabel.TabIndex = 12;
            this.searchLabel.Text = "Suchen";
            this.searchLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.searchLabel.Visible = false;
            // 
            // search
            // 
            this.search.AccessibleName = "Produkte durchsuchen";
            this.search.Dock = System.Windows.Forms.DockStyle.Fill;
            this.search.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.search.Location = new System.Drawing.Point(80, 2);
            this.search.Margin = new System.Windows.Forms.Padding(0, 2, 3, 12);
            this.search.Name = "search";
            this.search.Size = new System.Drawing.Size(661, 29);
            this.search.TabIndex = 13;
            this.search.Visible = false;
            this.search.TextChanged += new System.EventHandler(this.search_TextChanged);
            // 
            // catalog
            // 
            this.catalog.AutoScroll = true;
            this.catalog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.catalog.Location = new System.Drawing.Point(0, 176);
            this.catalog.Margin = new System.Windows.Forms.Padding(0);
            this.catalog.Name = "catalog";
            this.catalog.Size = new System.Drawing.Size(744, 308);
            this.catalog.TabIndex = 14;
            // 
            // productEntry
            // 
            this.productEntry.Controls.Add(this.productInput);
            this.productEntry.Controls.Add(this.addProduct);
            this.productEntry.Controls.Add(this.productLabel);
            this.productEntry.Dock = System.Windows.Forms.DockStyle.Fill;
            this.productEntry.Location = new System.Drawing.Point(0, 96);
            this.productEntry.Margin = new System.Windows.Forms.Padding(0);
            this.productEntry.Name = "productEntry";
            this.productEntry.Size = new System.Drawing.Size(744, 80);
            this.productEntry.TabIndex = 12;
            // 
            // productInput
            // 
            this.productInput.AccessibleName = "Produktname";
            this.productInput.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.productInput.Location = new System.Drawing.Point(0, 34);
            this.productInput.Name = "productInput";
            this.productInput.Size = new System.Drawing.Size(572, 25);
            this.productInput.TabIndex = 1;
            this.productInput.KeyDown += new System.Windows.Forms.KeyEventHandler(this.productInput_KeyDown);
            // 
            // addProduct
            // 
            this.addProduct.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.addProduct.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(211)))), ((int)(((byte)(76)))), ((int)(((byte)(34)))));
            this.addProduct.FlatAppearance.BorderSize = 0;
            this.addProduct.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.addProduct.ForeColor = System.Drawing.Color.White;
            this.addProduct.Location = new System.Drawing.Point(584, 28);
            this.addProduct.Name = "addProduct";
            this.addProduct.Size = new System.Drawing.Size(160, 38);
            this.addProduct.TabIndex = 2;
            this.addProduct.Text = "+ Hinzufügen";
            this.addProduct.UseVisualStyleBackColor = false;
            this.addProduct.Click += new System.EventHandler(this.addProduct_Click);
            // 
            // productLabel
            // 
            this.productLabel.Location = new System.Drawing.Point(0, 0);
            this.productLabel.Name = "productLabel";
            this.productLabel.Size = new System.Drawing.Size(400, 28);
            this.productLabel.TabIndex = 0;
            this.productLabel.Text = "Eigenes &Produkt hinzufügen";
            // 
            // summary
            // 
            this.summary.BackColor = System.Drawing.Color.White;
            this.summary.ColumnCount = 1;
            this.summary.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.summary.Controls.Add(this.basketTitle, 0, 0);
            this.summary.Controls.Add(this.count, 0, 1);
            this.summary.Controls.Add(this.basketRows, 0, 2);
            this.summary.Controls.Add(this.capacity, 0, 3);
            this.summary.Controls.Add(this.meter, 0, 4);
            this.summary.Controls.Add(this.sort, 0, 5);
            this.summary.Controls.Add(this.removeLast, 0, 6);
            this.summary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.summary.Location = new System.Drawing.Point(766, 0);
            this.summary.Margin = new System.Windows.Forms.Padding(0);
            this.summary.Name = "summary";
            this.summary.Padding = new System.Windows.Forms.Padding(20);
            this.summary.RowCount = 7;
            this.summary.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.summary.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.summary.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.summary.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.summary.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.summary.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.summary.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.summary.Size = new System.Drawing.Size(350, 484);
            this.summary.TabIndex = 15;
            // 
            // basketTitle
            // 
            this.basketTitle.AutoEllipsis = true;
            this.basketTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.basketTitle.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.basketTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(40)))), ((int)(((byte)(37)))));
            this.basketTitle.Location = new System.Drawing.Point(23, 20);
            this.basketTitle.Name = "basketTitle";
            this.basketTitle.Size = new System.Drawing.Size(304, 42);
            this.basketTitle.TabIndex = 16;
            this.basketTitle.Text = "Dein Warenkorb";
            this.basketTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // count
            // 
            this.count.AutoEllipsis = true;
            this.count.Dock = System.Windows.Forms.DockStyle.Fill;
            this.count.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.count.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(104)))), ((int)(((byte)(111)))), ((int)(((byte)(105)))));
            this.count.Location = new System.Drawing.Point(23, 62);
            this.count.Name = "count";
            this.count.Size = new System.Drawing.Size(304, 30);
            this.count.TabIndex = 17;
            this.count.Text = "0 Artikel · deine Auswahl";
            this.count.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // basketRows
            // 
            this.basketRows.AutoScroll = true;
            this.basketRows.Dock = System.Windows.Forms.DockStyle.Fill;
            this.basketRows.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.basketRows.Location = new System.Drawing.Point(20, 100);
            this.basketRows.Margin = new System.Windows.Forms.Padding(0, 8, 0, 8);
            this.basketRows.Name = "basketRows";
            this.basketRows.Size = new System.Drawing.Size(310, 212);
            this.basketRows.TabIndex = 18;
            this.basketRows.WrapContents = false;
            this.basketRows.SizeChanged += new System.EventHandler(this.basketRows_SizeChanged);
            // 
            // capacity
            // 
            this.capacity.AutoEllipsis = true;
            this.capacity.Dock = System.Windows.Forms.DockStyle.Fill;
            this.capacity.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.capacity.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(104)))), ((int)(((byte)(111)))), ((int)(((byte)(105)))));
            this.capacity.Location = new System.Drawing.Point(23, 320);
            this.capacity.Name = "capacity";
            this.capacity.Size = new System.Drawing.Size(304, 28);
            this.capacity.TabIndex = 19;
            this.capacity.Text = "0 von 10 Plätzen belegt";
            this.capacity.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // meter
            // 
            this.meter.Dock = System.Windows.Forms.DockStyle.Top;
            this.meter.Location = new System.Drawing.Point(23, 351);
            this.meter.Maximum = 10;
            this.meter.Name = "meter";
            this.meter.Size = new System.Drawing.Size(304, 8);
            this.meter.TabIndex = 20;
            // 
            // sort
            // 
            this.sort.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(211)))), ((int)(((byte)(76)))), ((int)(((byte)(34)))));
            this.sort.Cursor = System.Windows.Forms.Cursors.Hand;
            this.sort.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sort.FlatAppearance.BorderSize = 0;
            this.sort.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(98)))), ((int)(((byte)(60)))));
            this.sort.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.sort.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.sort.ForeColor = System.Drawing.Color.White;
            this.sort.Location = new System.Drawing.Point(23, 375);
            this.sort.Name = "sort";
            this.sort.Size = new System.Drawing.Size(304, 42);
            this.sort.TabIndex = 21;
            this.sort.Text = "A–Z sortieren";
            this.sort.UseVisualStyleBackColor = false;
            this.sort.Click += new System.EventHandler(this.sort_Click);
            // 
            // removeLast
            // 
            this.removeLast.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(245)))), ((int)(((byte)(240)))));
            this.removeLast.Cursor = System.Windows.Forms.Cursors.Hand;
            this.removeLast.Dock = System.Windows.Forms.DockStyle.Fill;
            this.removeLast.FlatAppearance.BorderSize = 0;
            this.removeLast.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(249)))), ((int)(((byte)(247)))), ((int)(((byte)(243)))));
            this.removeLast.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.removeLast.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.removeLast.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(40)))), ((int)(((byte)(37)))));
            this.removeLast.Location = new System.Drawing.Point(23, 423);
            this.removeLast.Name = "removeLast";
            this.removeLast.Size = new System.Drawing.Size(304, 38);
            this.removeLast.TabIndex = 22;
            this.removeLast.Text = "Letzten Artikel entfernen";
            this.removeLast.UseVisualStyleBackColor = false;
            this.removeLast.Click += new System.EventHandler(this.removeLast_Click);
            // 
            // status
            // 
            this.status.AutoEllipsis = true;
            this.status.Dock = System.Windows.Forms.DockStyle.Fill;
            this.status.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.status.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(104)))), ((int)(((byte)(111)))), ((int)(((byte)(105)))));
            this.status.Location = new System.Drawing.Point(35, 724);
            this.status.Name = "status";
            this.status.Size = new System.Drawing.Size(1110, 38);
            this.status.TabIndex = 23;
            this.status.Text = "Bereit für deine Auswahl.";
            this.status.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // GUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(245)))), ((int)(((byte)(240)))));
            this.ClientSize = new System.Drawing.Size(1180, 780);
            this.Controls.Add(this.root);
            this.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(40)))), ((int)(((byte)(37)))));
            this.MinimumSize = new System.Drawing.Size(1060, 740);
            this.Name = "GUI";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Warenkorb | MARKET STUDIO";
            this.Load += new System.EventHandler(this.GUI_Load);
            this.root.ResumeLayout(false);
            this.nav.ResumeLayout(false);
            this.hero.ResumeLayout(false);
            this.body.ResumeLayout(false);
            this.shop.ResumeLayout(false);
            this.searchBox.ResumeLayout(false);
            this.searchBox.PerformLayout();
            this.productEntry.ResumeLayout(false);
            this.productEntry.PerformLayout();
            this.summary.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel root;
        private System.Windows.Forms.Panel nav;
        private System.Windows.Forms.Label brandLabel;
        private System.Windows.Forms.Label tagLabel;
        private System.Windows.Forms.Panel hero;
        private System.Windows.Forms.Label eyebrowLabel;
        private System.Windows.Forms.Label headlineLabel;
        private System.Windows.Forms.Label introLabel;
        private System.Windows.Forms.TableLayoutPanel body;
        private System.Windows.Forms.TableLayoutPanel shop;
        private System.Windows.Forms.Label catalogTitle;
        private System.Windows.Forms.TableLayoutPanel searchBox;
        private System.Windows.Forms.Label searchLabel;
        private System.Windows.Forms.TextBox search;
        private System.Windows.Forms.FlowLayoutPanel catalog;
        private System.Windows.Forms.Panel productEntry;
        private System.Windows.Forms.TextBox productInput;
        private System.Windows.Forms.Button addProduct;
        private System.Windows.Forms.Label productLabel;
        private System.Windows.Forms.TableLayoutPanel summary;
        private System.Windows.Forms.Label basketTitle;
        private System.Windows.Forms.Label count;
        private System.Windows.Forms.FlowLayoutPanel basketRows;
        private System.Windows.Forms.Label capacity;
        private System.Windows.Forms.ProgressBar meter;
        private System.Windows.Forms.Button sort;
        private System.Windows.Forms.Button removeLast;
        private System.Windows.Forms.Label status;
    }
}

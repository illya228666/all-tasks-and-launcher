namespace Launcher.UI
{
    partial class Main
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            this.layoutRoot = new System.Windows.Forms.TableLayoutPanel();
            this._header = new System.Windows.Forms.Panel();
            this.panelControlRow = new System.Windows.Forms.FlowLayoutPanel();
            this._txtSearch = new System.Windows.Forms.TextBox();
            this._cbCategory = new System.Windows.Forms.ComboBox();
            this._cbSort = new System.Windows.Forms.ComboBox();
            this._chkFavoritesOnly = new System.Windows.Forms.CheckBox();
            this._chkAvailableOnly = new System.Windows.Forms.CheckBox();
            this._btnRefresh = new System.Windows.Forms.Button();
            this._btnSurprise = new System.Windows.Forms.Button();
            this._btnRoot = new System.Windows.Forms.Button();
            this._btnTheme = new System.Windows.Forms.Button();
            this.panelHeaderText = new System.Windows.Forms.Panel();
            this._lblSub = new System.Windows.Forms.Label();
            this._lblTitle = new System.Windows.Forms.Label();
            this.panelStats = new System.Windows.Forms.TableLayoutPanel();
            this._lblStats = new System.Windows.Forms.Label();
            this._lblHint = new System.Windows.Forms.Label();
            this.flpApps = new System.Windows.Forms.FlowLayoutPanel();
            this._tips = new System.Windows.Forms.ToolTip(this.components);
            this.layoutRoot.SuspendLayout();
            this._header.SuspendLayout();
            this.panelControlRow.SuspendLayout();
            this.panelHeaderText.SuspendLayout();
            this.panelStats.SuspendLayout();
            this.SuspendLayout();
            // 
            // layoutRoot
            // 
            this.layoutRoot.ColumnCount = 1;
            this.layoutRoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutRoot.Controls.Add(this._header, 0, 0);
            this.layoutRoot.Controls.Add(this.panelStats, 0, 1);
            this.layoutRoot.Controls.Add(this.flpApps, 0, 2);
            this.layoutRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutRoot.Location = new System.Drawing.Point(0, 0);
            this.layoutRoot.Margin = new System.Windows.Forms.Padding(0);
            this.layoutRoot.Name = "layoutRoot";
            this.layoutRoot.RowCount = 3;
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutRoot.Size = new System.Drawing.Size(1100, 800);
            this.layoutRoot.TabIndex = 0;
            // 
            // _header
            // 
            this._header.Controls.Add(this.panelControlRow);
            this._header.Controls.Add(this.panelHeaderText);
            this._header.Dock = System.Windows.Forms.DockStyle.Fill;
            this._header.Location = new System.Drawing.Point(0, 0);
            this._header.Margin = new System.Windows.Forms.Padding(0);
            this._header.Name = "_header";
            this._header.Padding = new System.Windows.Forms.Padding(16, 14, 16, 12);
            this._header.Size = new System.Drawing.Size(1100, 120);
            this._header.TabIndex = 0;
            this._header.Paint += new System.Windows.Forms.PaintEventHandler(this.Header_Paint);
            // 
            // panelControlRow
            // 
            this.panelControlRow.BackColor = System.Drawing.Color.Transparent;
            this.panelControlRow.Controls.Add(this._txtSearch);
            this.panelControlRow.Controls.Add(this._cbCategory);
            this.panelControlRow.Controls.Add(this._cbSort);
            this.panelControlRow.Controls.Add(this._chkFavoritesOnly);
            this.panelControlRow.Controls.Add(this._chkAvailableOnly);
            this.panelControlRow.Controls.Add(this._btnRefresh);
            this.panelControlRow.Controls.Add(this._btnSurprise);
            this.panelControlRow.Controls.Add(this._btnRoot);
            this.panelControlRow.Controls.Add(this._btnTheme);
            this.panelControlRow.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelControlRow.Location = new System.Drawing.Point(16, 74);
            this.panelControlRow.Margin = new System.Windows.Forms.Padding(0);
            this.panelControlRow.Name = "panelControlRow";
            this.panelControlRow.Size = new System.Drawing.Size(1068, 34);
            this.panelControlRow.TabIndex = 1;
            this.panelControlRow.WrapContents = false;
            // 
            // _txtSearch
            // 
            this._txtSearch.Location = new System.Drawing.Point(3, 3);
            this._txtSearch.Name = "_txtSearch";
            this._txtSearch.PlaceholderText = "Suche nach Name/Kategorie";
            this._txtSearch.Size = new System.Drawing.Size(210, 23);
            this._txtSearch.TabIndex = 0;
            // 
            // _cbCategory
            // 
            this._cbCategory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbCategory.FormattingEnabled = true;
            this._cbCategory.Location = new System.Drawing.Point(219, 3);
            this._cbCategory.Name = "_cbCategory";
            this._cbCategory.Size = new System.Drawing.Size(185, 23);
            this._cbCategory.TabIndex = 1;
            // 
            // _cbSort
            // 
            this._cbSort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbSort.FormattingEnabled = true;
            this._cbSort.Items.AddRange(new object[] {
            "Kategorie, Name",
            "Name (A-Z)",
            "Zuletzt gestartet",
            "Am haeufigsten",
            "Favoriten zuerst"});
            this._cbSort.Location = new System.Drawing.Point(410, 3);
            this._cbSort.Name = "_cbSort";
            this._cbSort.Size = new System.Drawing.Size(170, 23);
            this._cbSort.TabIndex = 2;
            // 
            // _chkFavoritesOnly
            // 
            this._chkFavoritesOnly.AutoSize = true;
            this._chkFavoritesOnly.Location = new System.Drawing.Point(586, 3);
            this._chkFavoritesOnly.Name = "_chkFavoritesOnly";
            this._chkFavoritesOnly.Padding = new System.Windows.Forms.Padding(8, 7, 8, 0);
            this._chkFavoritesOnly.Size = new System.Drawing.Size(114, 26);
            this._chkFavoritesOnly.TabIndex = 3;
            this._chkFavoritesOnly.Text = "Nur Favoriten";
            this._chkFavoritesOnly.UseVisualStyleBackColor = true;
            // 
            // _chkAvailableOnly
            // 
            this._chkAvailableOnly.AutoSize = true;
            this._chkAvailableOnly.Checked = true;
            this._chkAvailableOnly.CheckState = System.Windows.Forms.CheckState.Checked;
            this._chkAvailableOnly.Location = new System.Drawing.Point(706, 3);
            this._chkAvailableOnly.Name = "_chkAvailableOnly";
            this._chkAvailableOnly.Padding = new System.Windows.Forms.Padding(8, 7, 8, 0);
            this._chkAvailableOnly.Size = new System.Drawing.Size(124, 26);
            this._chkAvailableOnly.TabIndex = 4;
            this._chkAvailableOnly.Text = "Nur lauffaehige";
            this._chkAvailableOnly.UseVisualStyleBackColor = true;
            // 
            // _btnRefresh
            // 
            this._btnRefresh.AutoSize = true;
            this._btnRefresh.Location = new System.Drawing.Point(836, 3);
            this._btnRefresh.Name = "_btnRefresh";
            this._btnRefresh.Size = new System.Drawing.Size(84, 25);
            this._btnRefresh.TabIndex = 5;
            this._btnRefresh.Text = "Refresh (F5)";
            this._btnRefresh.UseVisualStyleBackColor = true;
            // 
            // _btnSurprise
            // 
            this._btnSurprise.AutoSize = true;
            this._btnSurprise.Location = new System.Drawing.Point(926, 3);
            this._btnSurprise.Name = "_btnSurprise";
            this._btnSurprise.Size = new System.Drawing.Size(112, 25);
            this._btnSurprise.TabIndex = 6;
            this._btnSurprise.Text = "Surprise (Ctrl+R)";
            this._btnSurprise.UseVisualStyleBackColor = true;
            // 
            // _btnRoot
            // 
            this._btnRoot.AutoSize = true;
            this._btnRoot.Location = new System.Drawing.Point(1044, 3);
            this._btnRoot.Name = "_btnRoot";
            this._btnRoot.Size = new System.Drawing.Size(47, 25);
            this._btnRoot.TabIndex = 7;
            this._btnRoot.Text = "Root";
            this._btnRoot.UseVisualStyleBackColor = true;
            // 
            // _btnTheme
            // 
            this._btnTheme.AutoSize = true;
            this._btnTheme.Location = new System.Drawing.Point(1097, 3);
            this._btnTheme.Name = "_btnTheme";
            this._btnTheme.Size = new System.Drawing.Size(91, 25);
            this._btnTheme.TabIndex = 8;
            this._btnTheme.Text = "Theme: Light";
            this._btnTheme.UseVisualStyleBackColor = true;
            // 
            // panelHeaderText
            // 
            this.panelHeaderText.BackColor = System.Drawing.Color.Transparent;
            this.panelHeaderText.Controls.Add(this._lblSub);
            this.panelHeaderText.Controls.Add(this._lblTitle);
            this.panelHeaderText.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeaderText.Location = new System.Drawing.Point(16, 14);
            this.panelHeaderText.Margin = new System.Windows.Forms.Padding(0);
            this.panelHeaderText.Name = "panelHeaderText";
            this.panelHeaderText.Size = new System.Drawing.Size(1068, 66);
            this.panelHeaderText.TabIndex = 0;
            // 
            // _lblSub
            // 
            this._lblSub.AutoSize = true;
            this._lblSub.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this._lblSub.Location = new System.Drawing.Point(2, 42);
            this._lblSub.Name = "_lblSub";
            this._lblSub.Size = new System.Drawing.Size(393, 19);
            this._lblSub.TabIndex = 1;
            this._lblSub.Text = "Suche, Favoriten, Tracking, Zufallsstart und schnelle Navigation.";
            // 
            // _lblTitle
            // 
            this._lblTitle.AutoSize = true;
            this._lblTitle.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this._lblTitle.Location = new System.Drawing.Point(0, 0);
            this._lblTitle.Name = "_lblTitle";
            this._lblTitle.Size = new System.Drawing.Size(329, 37);
            this._lblTitle.TabIndex = 0;
            this._lblTitle.Text = "Launcher Control Center";
            // 
            // panelStats
            // 
            this.panelStats.ColumnCount = 2;
            this.panelStats.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.panelStats.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.panelStats.Controls.Add(this._lblStats, 0, 0);
            this.panelStats.Controls.Add(this._lblHint, 1, 0);
            this.panelStats.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelStats.Location = new System.Drawing.Point(0, 120);
            this.panelStats.Margin = new System.Windows.Forms.Padding(0);
            this.panelStats.Name = "panelStats";
            this.panelStats.Padding = new System.Windows.Forms.Padding(16, 8, 16, 8);
            this.panelStats.RowCount = 1;
            this.panelStats.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelStats.Size = new System.Drawing.Size(1100, 40);
            this.panelStats.TabIndex = 1;
            // 
            // _lblStats
            // 
            this._lblStats.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblStats.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this._lblStats.Location = new System.Drawing.Point(19, 8);
            this._lblStats.Name = "_lblStats";
            this._lblStats.Size = new System.Drawing.Size(634, 24);
            this._lblStats.TabIndex = 0;
            this._lblStats.Text = "Stats";
            this._lblStats.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblHint
            // 
            this._lblHint.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblHint.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this._lblHint.Location = new System.Drawing.Point(659, 8);
            this._lblHint.Name = "_lblHint";
            this._lblHint.Size = new System.Drawing.Size(422, 24);
            this._lblHint.TabIndex = 1;
            this._lblHint.Text = "Ctrl+F Suche, Ctrl+R Surprise, F5 Refresh, Esc loeschen";
            this._lblHint.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // flpApps
            // 
            this.flpApps.AutoScroll = true;
            this.flpApps.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpApps.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpApps.Location = new System.Drawing.Point(0, 160);
            this.flpApps.Margin = new System.Windows.Forms.Padding(0);
            this.flpApps.Name = "flpApps";
            this.flpApps.Padding = new System.Windows.Forms.Padding(12);
            this.flpApps.Size = new System.Drawing.Size(1100, 640);
            this.flpApps.TabIndex = 2;
            this.flpApps.WrapContents = false;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1100, 800);
            this.Controls.Add(this.layoutRoot);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MinimumSize = new System.Drawing.Size(980, 720);
            this.Name = "Main";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Zahlen Launcher";
            this.Shown += new System.EventHandler(this.Main_Shown);
            this.layoutRoot.ResumeLayout(false);
            this._header.ResumeLayout(false);
            this.panelControlRow.ResumeLayout(false);
            this.panelControlRow.PerformLayout();
            this.panelHeaderText.ResumeLayout(false);
            this.panelHeaderText.PerformLayout();
            this.panelStats.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private TableLayoutPanel layoutRoot;
        private Panel _header;
        private FlowLayoutPanel panelControlRow;
        private TextBox _txtSearch;
        private ComboBox _cbCategory;
        private ComboBox _cbSort;
        private CheckBox _chkFavoritesOnly;
        private CheckBox _chkAvailableOnly;
        private Button _btnRefresh;
        private Button _btnSurprise;
        private Button _btnRoot;
        private Button _btnTheme;
        private Panel panelHeaderText;
        private Label _lblSub;
        private Label _lblTitle;
        private TableLayoutPanel panelStats;
        private Label _lblStats;
        private Label _lblHint;
        private FlowLayoutPanel flpApps;
        private ToolTip _tips;
    }
}

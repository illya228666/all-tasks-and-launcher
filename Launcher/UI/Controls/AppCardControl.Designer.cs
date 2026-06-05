namespace Launcher.UI.Controls
{
    partial class AppCardControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblCategory = new System.Windows.Forms.Label();
            this.lblFile = new System.Windows.Forms.Label();
            this.lblUsage = new System.Windows.Forms.Label();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnFolder = new System.Windows.Forms.Button();
            this.btnPath = new System.Windows.Forms.Button();
            this.btnFavorite = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoEllipsis = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblTitle.Location = new System.Drawing.Point(10, 8);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(200, 22);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "App";
            // 
            // lblCategory
            // 
            this.lblCategory.AutoEllipsis = true;
            this.lblCategory.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblCategory.Location = new System.Drawing.Point(10, 30);
            this.lblCategory.Name = "lblCategory";
            this.lblCategory.Size = new System.Drawing.Size(250, 16);
            this.lblCategory.TabIndex = 1;
            this.lblCategory.Text = "Category";
            // 
            // lblFile
            // 
            this.lblFile.AutoEllipsis = true;
            this.lblFile.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblFile.Location = new System.Drawing.Point(10, 46);
            this.lblFile.Name = "lblFile";
            this.lblFile.Size = new System.Drawing.Size(250, 16);
            this.lblFile.TabIndex = 2;
            this.lblFile.Text = "File";
            // 
            // lblUsage
            // 
            this.lblUsage.AutoEllipsis = true;
            this.lblUsage.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblUsage.Location = new System.Drawing.Point(10, 64);
            this.lblUsage.Name = "lblUsage";
            this.lblUsage.Size = new System.Drawing.Size(250, 16);
            this.lblUsage.TabIndex = 3;
            this.lblUsage.Text = "Usage";
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(10, 104);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(64, 32);
            this.btnStart.TabIndex = 4;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = true;
            // 
            // btnFolder
            // 
            this.btnFolder.Location = new System.Drawing.Point(80, 104);
            this.btnFolder.Name = "btnFolder";
            this.btnFolder.Size = new System.Drawing.Size(64, 32);
            this.btnFolder.TabIndex = 5;
            this.btnFolder.Text = "Ordner";
            this.btnFolder.UseVisualStyleBackColor = true;
            // 
            // btnPath
            // 
            this.btnPath.Location = new System.Drawing.Point(150, 104);
            this.btnPath.Name = "btnPath";
            this.btnPath.Size = new System.Drawing.Size(64, 32);
            this.btnPath.TabIndex = 6;
            this.btnPath.Text = "Path";
            this.btnPath.UseVisualStyleBackColor = true;
            // 
            // btnFavorite
            // 
            this.btnFavorite.Location = new System.Drawing.Point(220, 104);
            this.btnFavorite.Name = "btnFavorite";
            this.btnFavorite.Size = new System.Drawing.Size(42, 32);
            this.btnFavorite.TabIndex = 7;
            this.btnFavorite.Text = "Fav+";
            this.btnFavorite.UseVisualStyleBackColor = true;
            // 
            // AppCardControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.btnFavorite);
            this.Controls.Add(this.btnPath);
            this.Controls.Add(this.btnFolder);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.lblUsage);
            this.Controls.Add(this.lblFile);
            this.Controls.Add(this.lblCategory);
            this.Controls.Add(this.lblTitle);
            this.Margin = new System.Windows.Forms.Padding(8);
            this.Name = "AppCardControl";
            this.Size = new System.Drawing.Size(272, 162);
            this.ResumeLayout(false);

        }

        #endregion

        private Label lblTitle;
        private Label lblCategory;
        private Label lblFile;
        private Label lblUsage;
        private Button btnStart;
        private Button btnFolder;
        private Button btnPath;
        private Button btnFavorite;
    }
}

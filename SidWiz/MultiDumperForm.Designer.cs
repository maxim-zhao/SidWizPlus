namespace SidWiz
{
    partial class MultiDumperForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.Subsongs = new System.Windows.Forms.ListBox();
            this.OKButton = new System.Windows.Forms.Button();
            this.ProgressBar = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // Subsongs
            // 
            this.Subsongs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Subsongs.FormattingEnabled = true;
            this.Subsongs.IntegralHeight = false;
            this.Subsongs.Location = new System.Drawing.Point(12, 12);
            this.Subsongs.Name = "Subsongs";
            this.Subsongs.Size = new System.Drawing.Size(287, 174);
            this.Subsongs.TabIndex = 0;
            this.Subsongs.DoubleClick += new System.EventHandler(this.OKButtonClick);
            // 
            // OKButton
            // 
            this.OKButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OKButton.Enabled = false;
            this.OKButton.Location = new System.Drawing.Point(224, 192);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(75, 23);
            this.OKButton.TabIndex = 2;
            this.OKButton.Text = "OK";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click += new System.EventHandler(this.OKButtonClick);
            // 
            // ProgressBar
            // 
            this.ProgressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ProgressBar.Location = new System.Drawing.Point(12, 192);
            this.ProgressBar.Name = "ProgressBar";
            this.ProgressBar.Size = new System.Drawing.Size(206, 23);
            this.ProgressBar.TabIndex = 3;
            // 
            // MultiDumperForm
            // 
            this.AcceptButton = this.OKButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(311, 228);
            this.Controls.Add(this.ProgressBar);
            this.Controls.Add(this.OKButton);
            this.Controls.Add(this.Subsongs);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MultiDumperForm";
            this.ShowIcon = false;
            this.Text = "Multidumper subsong selection";
            this.Load += new System.EventHandler(this.SubsongSelectionForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox Subsongs;
        private System.Windows.Forms.Button OKButton;
        private System.Windows.Forms.ProgressBar ProgressBar;
    }
}
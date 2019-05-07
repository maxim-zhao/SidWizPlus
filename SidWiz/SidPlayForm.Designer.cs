using System.ComponentModel;
using System.Windows.Forms;

namespace SidWiz
{
    partial class SidPlayForm
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
            this.Subsongs.ItemHeight = 25;
            this.Subsongs.Items.AddRange(new object[] {
            " "});
            this.Subsongs.Location = new System.Drawing.Point(15, 15);
            this.Subsongs.Margin = new System.Windows.Forms.Padding(6);
            this.Subsongs.Name = "Subsongs";
            this.Subsongs.Size = new System.Drawing.Size(998, 364);
            this.Subsongs.TabIndex = 0;
            this.Subsongs.DoubleClick += new System.EventHandler(this.OkButtonClick);
            // 
            // OKButton
            // 
            this.OKButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OKButton.Enabled = false;
            this.OKButton.Location = new System.Drawing.Point(863, 391);
            this.OKButton.Margin = new System.Windows.Forms.Padding(6);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(150, 44);
            this.OKButton.TabIndex = 2;
            this.OKButton.Text = "OK";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click += new System.EventHandler(this.OkButtonClick);
            // 
            // ProgressBar
            // 
            this.ProgressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ProgressBar.Location = new System.Drawing.Point(15, 391);
            this.ProgressBar.Margin = new System.Windows.Forms.Padding(6);
            this.ProgressBar.Name = "ProgressBar";
            this.ProgressBar.Size = new System.Drawing.Size(836, 44);
            this.ProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.ProgressBar.TabIndex = 4;
            // 
            // SidPlayForm
            // 
            this.AcceptButton = this.OKButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1028, 450);
            this.Controls.Add(this.ProgressBar);
            this.Controls.Add(this.OKButton);
            this.Controls.Add(this.Subsongs);
            this.Margin = new System.Windows.Forms.Padding(6);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SidPlayForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "SidPlay subsong selection";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.SubsongSelectionForm_Closing);
            this.Load += new System.EventHandler(this.SubsongSelectionForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox Subsongs;
        private System.Windows.Forms.Button OKButton;
        private ProgressBar ProgressBar;
    }
}
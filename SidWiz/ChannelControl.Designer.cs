namespace SidWiz
{
    partial class ChannelControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChannelControl));
            this.TitleLabel = new System.Windows.Forms.Label();
            this.Close = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.Left = new System.Windows.Forms.Button();
            this.Right = new System.Windows.Forms.Button();
            this.ConfigureToggleButton = new System.Windows.Forms.Button();
            this.PropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.PictureBox = new System.Windows.Forms.PictureBox();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // TitleLabel
            // 
            this.TitleLabel.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.TitleLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TitleLabel.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TitleLabel.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.TitleLabel.Location = new System.Drawing.Point(0, 0);
            this.TitleLabel.Name = "TitleLabel";
            this.TitleLabel.Size = new System.Drawing.Size(951, 56);
            this.TitleLabel.TabIndex = 8;
            this.TitleLabel.Text = "Title";
            this.TitleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Close
            // 
            this.Close.Dock = System.Windows.Forms.DockStyle.Right;
            this.Close.Image = ((System.Drawing.Image)(resources.GetObject("Close.Image")));
            this.Close.Location = new System.Drawing.Point(899, 0);
            this.Close.Name = "Close";
            this.Close.Size = new System.Drawing.Size(52, 56);
            this.Close.TabIndex = 9;
            this.Close.UseVisualStyleBackColor = true;
            this.Close.Click += new System.EventHandler(this.Close_Click);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.Left);
            this.panel1.Controls.Add(this.Right);
            this.panel1.Controls.Add(this.ConfigureToggleButton);
            this.panel1.Controls.Add(this.Close);
            this.panel1.Controls.Add(this.TitleLabel);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(951, 56);
            this.panel1.TabIndex = 10;
            // 
            // Left
            // 
            this.Left.Dock = System.Windows.Forms.DockStyle.Right;
            this.Left.Image = ((System.Drawing.Image)(resources.GetObject("Left.Image")));
            this.Left.Location = new System.Drawing.Point(743, 0);
            this.Left.Name = "Left";
            this.Left.Size = new System.Drawing.Size(52, 56);
            this.Left.TabIndex = 12;
            this.Left.UseVisualStyleBackColor = true;
            this.Left.Click += new System.EventHandler(this.Left_Click);
            // 
            // Right
            // 
            this.Right.Dock = System.Windows.Forms.DockStyle.Right;
            this.Right.Image = ((System.Drawing.Image)(resources.GetObject("Right.Image")));
            this.Right.Location = new System.Drawing.Point(795, 0);
            this.Right.Name = "Right";
            this.Right.Size = new System.Drawing.Size(52, 56);
            this.Right.TabIndex = 11;
            this.Right.UseVisualStyleBackColor = true;
            this.Right.Click += new System.EventHandler(this.Right_Click);
            // 
            // ConfigureToggleButton
            // 
            this.ConfigureToggleButton.Dock = System.Windows.Forms.DockStyle.Right;
            this.ConfigureToggleButton.Image = ((System.Drawing.Image)(resources.GetObject("ConfigureToggleButton.Image")));
            this.ConfigureToggleButton.Location = new System.Drawing.Point(847, 0);
            this.ConfigureToggleButton.Name = "ConfigureToggleButton";
            this.ConfigureToggleButton.Size = new System.Drawing.Size(52, 56);
            this.ConfigureToggleButton.TabIndex = 10;
            this.ConfigureToggleButton.UseVisualStyleBackColor = true;
            this.ConfigureToggleButton.Click += new System.EventHandler(this.ConfigureToggleButton_Click);
            // 
            // PropertyGrid
            // 
            this.PropertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PropertyGrid.Location = new System.Drawing.Point(0, 56);
            this.PropertyGrid.Name = "PropertyGrid";
            this.PropertyGrid.Size = new System.Drawing.Size(951, 468);
            this.PropertyGrid.TabIndex = 11;
            this.PropertyGrid.Visible = false;
            // 
            // PictureBox
            // 
            this.PictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PictureBox.Location = new System.Drawing.Point(0, 56);
            this.PictureBox.Name = "PictureBox";
            this.PictureBox.Size = new System.Drawing.Size(951, 468);
            this.PictureBox.TabIndex = 12;
            this.PictureBox.TabStop = false;
            this.PictureBox.Resize += new System.EventHandler(this.PictureBox_Resize);
            // 
            // ChannelControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.PropertyGrid);
            this.Controls.Add(this.PictureBox);
            this.Controls.Add(this.panel1);
            this.Name = "ChannelControl";
            this.Size = new System.Drawing.Size(951, 524);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PictureBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button ConfigureToggleButton;
        private System.Windows.Forms.Label TitleLabel;
        private System.Windows.Forms.PropertyGrid PropertyGrid;
        private System.Windows.Forms.Button Close;
        private System.Windows.Forms.Button Left;
        private System.Windows.Forms.Button Right;
        private System.Windows.Forms.PictureBox PictureBox;
    }
}

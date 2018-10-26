namespace SidWiz
{
    partial class LayoutControl
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LayoutControl));
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.button1 = new System.Windows.Forms.Button();
			this.chkAlt = new System.Windows.Forms.CheckBox();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(0, 5);
			this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(24, 17);
			this.label1.TabIndex = 33;
			this.label1.Text = "01";
			// 
			// label2
			// 
			this.label2.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.label2.Location = new System.Drawing.Point(4, 60);
			this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label2.Name = "label2";
			this.label2.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
			this.label2.Size = new System.Drawing.Size(165, 16);
			this.label2.TabIndex = 34;
			this.label2.Text = "File Name";
			this.label2.Click += new System.EventHandler(this.label2_Click);
			// 
			// button1
			// 
			this.button1.BackColor = System.Drawing.Color.White;
			this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.button1.ForeColor = System.Drawing.Color.Black;
			this.button1.Location = new System.Drawing.Point(3, 25);
			this.button1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(159, 28);
			this.button1.TabIndex = 35;
			this.button1.UseVisualStyleBackColor = false;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// chkAlt
			// 
			this.chkAlt.AutoSize = true;
			this.chkAlt.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.chkAlt.Location = new System.Drawing.Point(76, 4);
			this.chkAlt.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this.chkAlt.Name = "chkAlt";
			this.chkAlt.Size = new System.Drawing.Size(81, 21);
			this.chkAlt.TabIndex = 36;
			this.chkAlt.Text = "Alt Sync";
			this.toolTip1.SetToolTip(this.chkAlt, resources.GetString("chkAlt.ToolTip"));
			this.chkAlt.UseVisualStyleBackColor = true;
			// 
			// LayoutControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.Controls.Add(this.chkAlt);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Margin = new System.Windows.Forms.Padding(0);
			this.Name = "LayoutControl";
			this.Size = new System.Drawing.Size(167, 80);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.Label label1;
        public System.Windows.Forms.Label label2;
        public System.Windows.Forms.Button button1;
        private System.Windows.Forms.ToolTip toolTip1;
        public System.Windows.Forms.CheckBox chkAlt;

    }
}

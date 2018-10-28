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
            this.components = new System.ComponentModel.Container();
            this.filenameButton = new System.Windows.Forms.Button();
            this.LabelTextBox = new System.Windows.Forms.TextBox();
            this.LineWidthControl = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label3 = new System.Windows.Forms.Label();
            this.algorithmsCombo = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.highPassFilterFrequency = new System.Windows.Forms.TextBox();
            this.HighPassFilterCheckbox = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.LookaheadControl = new System.Windows.Forms.NumericUpDown();
            this.colorButton1 = new SidWiz.ColorButton(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.LineWidthControl)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.LookaheadControl)).BeginInit();
            this.SuspendLayout();
            // 
            // filenameButton
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.filenameButton, 2);
            this.filenameButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.filenameButton.Location = new System.Drawing.Point(3, 3);
            this.filenameButton.Name = "filenameButton";
            this.filenameButton.Size = new System.Drawing.Size(630, 39);
            this.filenameButton.TabIndex = 0;
            this.filenameButton.Text = "Filename";
            this.filenameButton.UseVisualStyleBackColor = true;
            this.filenameButton.Click += new System.EventHandler(this.FilenameButtonClick);
            // 
            // LabelTextBox
            // 
            this.LabelTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LabelTextBox.Location = new System.Drawing.Point(321, 48);
            this.LabelTextBox.Multiline = true;
            this.LabelTextBox.Name = "LabelTextBox";
            this.LabelTextBox.Size = new System.Drawing.Size(312, 39);
            this.LabelTextBox.TabIndex = 2;
            // 
            // LineWidthControl
            // 
            this.LineWidthControl.DecimalPlaces = 1;
            this.LineWidthControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LineWidthControl.Location = new System.Drawing.Point(321, 93);
            this.LineWidthControl.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.LineWidthControl.Name = "LineWidthControl";
            this.LineWidthControl.Size = new System.Drawing.Size(312, 31);
            this.LineWidthControl.TabIndex = 4;
            this.LineWidthControl.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 90);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(67, 25);
            this.label1.TabIndex = 3;
            this.label1.Text = "Width";
            // 
            // label2
            // 
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Location = new System.Drawing.Point(3, 45);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(312, 45);
            this.label2.TabIndex = 1;
            this.label2.Text = "Label";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.label5, 0, 6);
            this.tableLayoutPanel1.Controls.Add(this.filenameButton, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.LineWidthControl, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.LabelTextBox, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.label3, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.algorithmsCombo, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.label4, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.highPassFilterFrequency, 1, 5);
            this.tableLayoutPanel1.Controls.Add(this.HighPassFilterCheckbox, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.LookaheadControl, 1, 6);
            this.tableLayoutPanel1.Controls.Add(this.colorButton1, 1, 3);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 7;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28531F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28531F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28531F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28531F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28531F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28531F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28816F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(636, 319);
            this.tableLayoutPanel1.TabIndex = 7;
            // 
            // label3
            // 
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Location = new System.Drawing.Point(3, 135);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(312, 45);
            this.label3.TabIndex = 5;
            this.label3.Text = "Color";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // algorithmsCombo
            // 
            this.algorithmsCombo.DisplayMember = "Name";
            this.algorithmsCombo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.algorithmsCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.algorithmsCombo.Location = new System.Drawing.Point(321, 183);
            this.algorithmsCombo.Name = "algorithmsCombo";
            this.algorithmsCombo.Size = new System.Drawing.Size(312, 33);
            this.algorithmsCombo.TabIndex = 8;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 180);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(102, 25);
            this.label4.TabIndex = 7;
            this.label4.Text = "Algorithm";
            // 
            // highPassFilterFrequency
            // 
            this.highPassFilterFrequency.Dock = System.Windows.Forms.DockStyle.Fill;
            this.highPassFilterFrequency.Enabled = false;
            this.highPassFilterFrequency.Location = new System.Drawing.Point(321, 228);
            this.highPassFilterFrequency.Name = "highPassFilterFrequency";
            this.highPassFilterFrequency.Size = new System.Drawing.Size(312, 31);
            this.highPassFilterFrequency.TabIndex = 10;
            // 
            // HighPassFilterCheckbox
            // 
            this.HighPassFilterCheckbox.AutoSize = true;
            this.HighPassFilterCheckbox.Location = new System.Drawing.Point(3, 228);
            this.HighPassFilterCheckbox.Name = "HighPassFilterCheckbox";
            this.HighPassFilterCheckbox.Size = new System.Drawing.Size(187, 29);
            this.HighPassFilterCheckbox.TabIndex = 11;
            this.HighPassFilterCheckbox.Text = "High pass filter";
            this.HighPassFilterCheckbox.UseVisualStyleBackColor = true;
            this.HighPassFilterCheckbox.CheckedChanged += new System.EventHandler(this.highPassFilterCheckBox_CheckedChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(3, 270);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(186, 25);
            this.label5.TabIndex = 12;
            this.label5.Text = "Trigger lookahead";
            // 
            // LookaheadControl
            // 
            this.LookaheadControl.Location = new System.Drawing.Point(321, 273);
            this.LookaheadControl.Name = "LookaheadControl";
            this.LookaheadControl.Size = new System.Drawing.Size(120, 31);
            this.LookaheadControl.TabIndex = 13;
            // 
            // colorButton1
            // 
            this.colorButton1.BackColor = System.Drawing.Color.White;
            this.colorButton1.Color = System.Drawing.Color.White;
            this.colorButton1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.colorButton1.ForeColor = System.Drawing.Color.Black;
            this.colorButton1.Location = new System.Drawing.Point(321, 138);
            this.colorButton1.Name = "colorButton1";
            this.colorButton1.Size = new System.Drawing.Size(312, 39);
            this.colorButton1.TabIndex = 14;
            this.colorButton1.Text = "White";
            this.colorButton1.UseVisualStyleBackColor = false;
            // 
            // ChannelControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "ChannelControl";
            this.Size = new System.Drawing.Size(636, 319);
            ((System.ComponentModel.ISupportInitialize)(this.LineWidthControl)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.LookaheadControl)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button filenameButton;
        private System.Windows.Forms.TextBox LabelTextBox;
        private System.Windows.Forms.NumericUpDown LineWidthControl;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.ComboBox algorithmsCombo;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox highPassFilterFrequency;
        private System.Windows.Forms.CheckBox HighPassFilterCheckbox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown LookaheadControl;
        private ColorButton colorButton1;
    }
}

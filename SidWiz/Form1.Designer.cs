namespace SidWiz
{
    partial class Form1
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
			this.Start = new System.Windows.Forms.Button();
			this.Stop = new System.Windows.Forms.Button();
			this.label6 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.checkBox2 = new System.Windows.Forms.CheckBox();
			this.chkGrid = new System.Windows.Forms.CheckBox();
			this.label20 = new System.Windows.Forms.Label();
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.button3 = new System.Windows.Forms.Button();
			this.button4 = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.comboBox1 = new System.Windows.Forms.ComboBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.tboxHeight = new System.Windows.Forms.TextBox();
			this.tboxWidth = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.numThick = new System.Windows.Forms.NumericUpDown();
			this.tboxFps = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.tboxScale = new System.Windows.Forms.TextBox();
			this.numColumns = new System.Windows.Forms.NumericUpDown();
			this.numVoices = new System.Windows.Forms.NumericUpDown();
			this.chkBlock = new System.Windows.Forms.CheckBox();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.enableFFBox = new System.Windows.Forms.CheckBox();
			this.ffOutArgs = new System.Windows.Forms.TextBox();
			this.chkColorCycle = new System.Windows.Forms.CheckBox();
			this.chkSm = new System.Windows.Forms.CheckBox();
			this.groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numThick)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numColumns)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numVoices)).BeginInit();
			this.SuspendLayout();
			// 
			// Start
			// 
			this.Start.Location = new System.Drawing.Point(13, 258);
			this.Start.Margin = new System.Windows.Forms.Padding(4);
			this.Start.Name = "Start";
			this.Start.Size = new System.Drawing.Size(73, 28);
			this.Start.TabIndex = 16;
			this.Start.Text = "Start";
			this.Start.UseVisualStyleBackColor = true;
			this.Start.Click += new System.EventHandler(this.Start_Click);
			// 
			// Stop
			// 
			this.Stop.Location = new System.Drawing.Point(94, 258);
			this.Stop.Margin = new System.Windows.Forms.Padding(4);
			this.Stop.Name = "Stop";
			this.Stop.Size = new System.Drawing.Size(69, 28);
			this.Stop.TabIndex = 17;
			this.Stop.Text = "Stop";
			this.Stop.UseVisualStyleBackColor = true;
			this.Stop.Click += new System.EventHandler(this.Stop_Click);
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(8, 20);
			this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(58, 17);
			this.label6.TabIndex = 0;
			this.label6.Text = "#Voices";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(8, 53);
			this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(70, 17);
			this.label7.TabIndex = 1;
			this.label7.Text = "#Columns";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(8, 86);
			this.label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(43, 17);
			this.label8.TabIndex = 2;
			this.label8.Text = "Scale";
			// 
			// checkBox2
			// 
			this.checkBox2.AutoSize = true;
			this.checkBox2.Checked = true;
			this.checkBox2.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBox2.Location = new System.Drawing.Point(144, 150);
			this.checkBox2.Margin = new System.Windows.Forms.Padding(4);
			this.checkBox2.Name = "checkBox2";
			this.checkBox2.Size = new System.Drawing.Size(73, 21);
			this.checkBox2.TabIndex = 10;
			this.checkBox2.Text = "Output";
			this.toolTip1.SetToolTip(this.checkBox2, "Displaying output makes the process take longer, so disable this for faster perfo" +
        "rmance.");
			this.checkBox2.UseVisualStyleBackColor = true;
			this.checkBox2.CheckedChanged += new System.EventHandler(this.checkBox2_CheckedChanged);
			// 
			// chkGrid
			// 
			this.chkGrid.AutoSize = true;
			this.chkGrid.Checked = true;
			this.chkGrid.CheckState = System.Windows.Forms.CheckState.Checked;
			this.chkGrid.Location = new System.Drawing.Point(76, 150);
			this.chkGrid.Margin = new System.Windows.Forms.Padding(4);
			this.chkGrid.Name = "chkGrid";
			this.chkGrid.Size = new System.Drawing.Size(57, 21);
			this.chkGrid.TabIndex = 9;
			this.chkGrid.Text = "Grid";
			this.toolTip1.SetToolTip(this.chkGrid, "Displays a grid between channels.");
			this.chkGrid.UseVisualStyleBackColor = true;
			// 
			// label20
			// 
			this.label20.AutoSize = true;
			this.label20.Location = new System.Drawing.Point(167, 86);
			this.label20.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label20.Name = "label20";
			this.label20.Size = new System.Drawing.Size(44, 17);
			this.label20.TabIndex = 6;
			this.label20.Text = "Width";
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(171, 258);
			this.button1.Margin = new System.Windows.Forms.Padding(4);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(95, 28);
			this.button1.TabIndex = 18;
			this.button1.Text = "About";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// button2
			// 
			this.button2.BackColor = System.Drawing.SystemColors.Window;
			this.button2.Location = new System.Drawing.Point(229, 143);
			this.button2.Margin = new System.Windows.Forms.Padding(4);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(132, 28);
			this.button2.TabIndex = 11;
			this.button2.Text = "Master Audio File";
			this.toolTip1.SetToolTip(this.button2, "This is the file you\'ll hear in the end video.");
			this.button2.UseVisualStyleBackColor = false;
			this.button2.Click += new System.EventHandler(this.button2_Click_1);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(167, 20);
			this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(103, 17);
			this.label1.TabIndex = 4;
			this.label1.Text = "Line Thickness";
			this.toolTip1.SetToolTip(this.label1, "Sets line thickness in pixels.");
			// 
			// button3
			// 
			this.button3.Location = new System.Drawing.Point(13, 218);
			this.button3.Margin = new System.Windows.Forms.Padding(4);
			this.button3.Name = "button3";
			this.button3.Size = new System.Drawing.Size(129, 28);
			this.button3.TabIndex = 14;
			this.button3.Text = "Load Template";
			this.toolTip1.SetToolTip(this.button3, "Loads a template.  This will fill the channels and all their related values back " +
        "in from the template.  The filenames are relative to the folder you\'ve selected." +
        "");
			this.button3.UseVisualStyleBackColor = true;
			this.button3.Click += new System.EventHandler(this.button3_Click);
			// 
			// button4
			// 
			this.button4.Location = new System.Drawing.Point(150, 218);
			this.button4.Margin = new System.Windows.Forms.Padding(4);
			this.button4.Name = "button4";
			this.button4.Size = new System.Drawing.Size(116, 28);
			this.button4.TabIndex = 15;
			this.button4.Text = "Save Template";
			this.toolTip1.SetToolTip(this.button4, "Saves all program info to a file to be used with other folders.  File names are r" +
        "elative to the selected folder.");
			this.button4.UseVisualStyleBackColor = true;
			this.button4.Click += new System.EventHandler(this.button4_Click);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Enabled = false;
			this.label2.Location = new System.Drawing.Point(167, 53);
			this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(94, 17);
			this.label2.TabIndex = 5;
			this.label2.Text = "Normalization";
			this.toolTip1.SetToolTip(this.label2, resources.GetString("label2.ToolTip"));
			// 
			// comboBox1
			// 
			this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBox1.Enabled = false;
			this.comboBox1.FormattingEnabled = true;
			this.comboBox1.Items.AddRange(new object[] {
            "None",
            "Individual",
            "Overall"});
			this.comboBox1.Location = new System.Drawing.Point(278, 49);
			this.comboBox1.Margin = new System.Windows.Forms.Padding(4);
			this.comboBox1.Name = "comboBox1";
			this.comboBox1.Size = new System.Drawing.Size(78, 24);
			this.comboBox1.TabIndex = 5;
			this.toolTip1.SetToolTip(this.comboBox1, resources.GetString("comboBox1.ToolTip"));
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.tboxHeight);
			this.groupBox1.Controls.Add(this.tboxWidth);
			this.groupBox1.Controls.Add(this.label4);
			this.groupBox1.Controls.Add(this.numThick);
			this.groupBox1.Controls.Add(this.tboxFps);
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.tboxScale);
			this.groupBox1.Controls.Add(this.numColumns);
			this.groupBox1.Controls.Add(this.numVoices);
			this.groupBox1.Controls.Add(this.chkBlock);
			this.groupBox1.Controls.Add(this.label6);
			this.groupBox1.Controls.Add(this.comboBox1);
			this.groupBox1.Controls.Add(this.button2);
			this.groupBox1.Controls.Add(this.checkBox2);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.chkGrid);
			this.groupBox1.Controls.Add(this.label7);
			this.groupBox1.Controls.Add(this.label20);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.label8);
			this.groupBox1.Location = new System.Drawing.Point(9, 0);
			this.groupBox1.Margin = new System.Windows.Forms.Padding(4);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Padding = new System.Windows.Forms.Padding(4);
			this.groupBox1.Size = new System.Drawing.Size(368, 181);
			this.groupBox1.TabIndex = 42;
			this.groupBox1.TabStop = false;
			// 
			// tboxHeight
			// 
			this.tboxHeight.Location = new System.Drawing.Point(278, 114);
			this.tboxHeight.Name = "tboxHeight";
			this.tboxHeight.Size = new System.Drawing.Size(78, 22);
			this.tboxHeight.TabIndex = 7;
			this.tboxHeight.Text = "720";
			// 
			// tboxWidth
			// 
			this.tboxWidth.Location = new System.Drawing.Point(278, 80);
			this.tboxWidth.Name = "tboxWidth";
			this.tboxWidth.Size = new System.Drawing.Size(78, 22);
			this.tboxWidth.TabIndex = 6;
			this.tboxWidth.Text = "1280";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(167, 118);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(49, 17);
			this.label4.TabIndex = 7;
			this.label4.Text = "Height";
			// 
			// numThick
			// 
			this.numThick.Location = new System.Drawing.Point(278, 17);
			this.numThick.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
			this.numThick.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numThick.Name = "numThick";
			this.numThick.Size = new System.Drawing.Size(78, 22);
			this.numThick.TabIndex = 4;
			this.numThick.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
			// 
			// tboxFps
			// 
			this.tboxFps.Location = new System.Drawing.Point(87, 115);
			this.tboxFps.Name = "tboxFps";
			this.tboxFps.Size = new System.Drawing.Size(60, 22);
			this.tboxFps.TabIndex = 3;
			this.tboxFps.Text = "60";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(8, 118);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(73, 17);
			this.label3.TabIndex = 3;
			this.label3.Text = "Framerate";
			// 
			// tboxScale
			// 
			this.tboxScale.Location = new System.Drawing.Point(87, 82);
			this.tboxScale.Name = "tboxScale";
			this.tboxScale.Size = new System.Drawing.Size(60, 22);
			this.tboxScale.TabIndex = 2;
			this.tboxScale.Text = "2";
			this.toolTip1.SetToolTip(this.tboxScale, "Number of samples to display in one voice.  Lower values can cause problems with " +
        "bassy tracks, and higher values can cause higher-pitch audio to become a solid b" +
        "lock.");
			// 
			// numColumns
			// 
			this.numColumns.Location = new System.Drawing.Point(87, 50);
			this.numColumns.Maximum = new decimal(new int[] {
            128,
            0,
            0,
            0});
			this.numColumns.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numColumns.Name = "numColumns";
			this.numColumns.Size = new System.Drawing.Size(60, 22);
			this.numColumns.TabIndex = 1;
			this.numColumns.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numColumns.ValueChanged += new System.EventHandler(this.comboColumns_SelectedIndexChanged);
			// 
			// numVoices
			// 
			this.numVoices.Location = new System.Drawing.Point(87, 18);
			this.numVoices.Maximum = new decimal(new int[] {
            128,
            0,
            0,
            0});
			this.numVoices.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numVoices.Name = "numVoices";
			this.numVoices.Size = new System.Drawing.Size(60, 22);
			this.numVoices.TabIndex = 0;
			this.numVoices.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numVoices.ValueChanged += new System.EventHandler(this.cmbVoices_SelectedIndexChanged);
			// 
			// chkBlock
			// 
			this.chkBlock.AutoSize = true;
			this.chkBlock.Location = new System.Drawing.Point(8, 150);
			this.chkBlock.Margin = new System.Windows.Forms.Padding(4);
			this.chkBlock.Name = "chkBlock";
			this.chkBlock.Size = new System.Drawing.Size(64, 21);
			this.chkBlock.TabIndex = 8;
			this.chkBlock.Text = "Block";
			this.toolTip1.SetToolTip(this.chkBlock, "If checked, the area between the wave and the middle is filled in.\r\n\r\nGives the w" +
        "aves a 2d shape instead of being lines.");
			this.chkBlock.UseVisualStyleBackColor = true;
			// 
			// enableFFBox
			// 
			this.enableFFBox.AutoSize = true;
			this.enableFFBox.Location = new System.Drawing.Point(13, 189);
			this.enableFFBox.Name = "enableFFBox";
			this.enableFFBox.Size = new System.Drawing.Size(81, 21);
			this.enableFFBox.TabIndex = 12;
			this.enableFFBox.Text = "FFmpeg";
			this.toolTip1.SetToolTip(this.enableFFBox, "Uses FFmpeg as an encoder. Requires an external executable of it.");
			this.enableFFBox.UseVisualStyleBackColor = true;
			this.enableFFBox.CheckedChanged += new System.EventHandler(this.enableFFBox_CheckedChanged);
			// 
			// ffOutArgs
			// 
			this.ffOutArgs.Enabled = false;
			this.ffOutArgs.Location = new System.Drawing.Point(101, 189);
			this.ffOutArgs.Name = "ffOutArgs";
			this.ffOutArgs.Size = new System.Drawing.Size(276, 22);
			this.ffOutArgs.TabIndex = 13;
			this.ffOutArgs.Text = "-c:v h264 -b:v 2000k -c:a aac -b:a 320k";
			this.toolTip1.SetToolTip(this.ffOutArgs, "Conversion command line arguments. Such as video codec, bitrate, etc.");
			// 
			// chkColorCycle
			// 
			this.chkColorCycle.AutoSize = true;
			this.chkColorCycle.Location = new System.Drawing.Point(274, 223);
			this.chkColorCycle.Margin = new System.Windows.Forms.Padding(4);
			this.chkColorCycle.Name = "chkColorCycle";
			this.chkColorCycle.Size = new System.Drawing.Size(97, 21);
			this.chkColorCycle.TabIndex = 19;
			this.chkColorCycle.Text = "ColorCycle";
			this.chkColorCycle.UseVisualStyleBackColor = true;
			// 
			// chkSm
			// 
			this.chkSm.AutoSize = true;
			this.chkSm.Location = new System.Drawing.Point(274, 263);
			this.chkSm.Margin = new System.Windows.Forms.Padding(4);
			this.chkSm.Name = "chkSm";
			this.chkSm.Size = new System.Drawing.Size(111, 21);
			this.chkSm.TabIndex = 20;
			this.chkSm.Text = "Small Layout";
			this.chkSm.UseVisualStyleBackColor = true;
			this.chkSm.CheckedChanged += new System.EventHandler(this.chkSm_CheckedChanged);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(389, 299);
			this.Controls.Add(this.ffOutArgs);
			this.Controls.Add(this.enableFFBox);
			this.Controls.Add(this.chkSm);
			this.Controls.Add(this.chkColorCycle);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.button4);
			this.Controls.Add(this.button3);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.Stop);
			this.Controls.Add(this.Start);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "Form1";
			this.Text = "SidWiz 2.1";
			this.Load += new System.EventHandler(this.Form1_Load);
			this.LocationChanged += new System.EventHandler(this.location_Changed);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.numThick)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numColumns)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numVoices)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button Start;
        private System.Windows.Forms.Button Stop;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.CheckBox chkGrid;
        private System.Windows.Forms.CheckBox checkBox2;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.CheckBox chkBlock;
        private System.Windows.Forms.CheckBox chkColorCycle;
        private System.Windows.Forms.CheckBox chkSm;
        private System.Windows.Forms.NumericUpDown numVoices;
        private System.Windows.Forms.TextBox tboxScale;
        private System.Windows.Forms.NumericUpDown numColumns;
        private System.Windows.Forms.TextBox tboxFps;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox enableFFBox;
        private System.Windows.Forms.TextBox ffOutArgs;
        private System.Windows.Forms.NumericUpDown numThick;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tboxHeight;
        private System.Windows.Forms.TextBox tboxWidth;
    }
}


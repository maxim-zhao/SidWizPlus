using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SidWiz
{
    public partial class LayoutControl : UserControl
    {
        public String filePath = "none";
        public LayoutControl()
        {
            InitializeComponent();
        }


        private void button1_Click(object sender, EventArgs e)
        {
            ColorDialog colorDialog1 = new ColorDialog();
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                button1.BackColor = colorDialog1.Color;
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Wave files (*.wav)|*.wav";
            ofd.ShowDialog();
            if (System.IO.Path.GetExtension(ofd.FileName) == ".wav")
            {
                label2.Text = System.IO.Path.GetFileName(ofd.FileName);
                filePath = ofd.FileName;
            }
            else
            {
                MessageBox.Show("This is not a .wav file, or no file was selected.");
                return;
            }

        }
	}
}

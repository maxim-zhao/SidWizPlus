using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SidWiz
{
    public partial class LayoutForm : Form
    {
        UserControl li = new LayoutControl();
        public LayoutForm()
        {
            InitializeComponent();
        }

        private void LayoutForm_Load(object sender, EventArgs e)
        {
            this.ClientSize = new System.Drawing.Size(li.Width, li.Height);
            this.Controls.Add(li);
        }



    }
}

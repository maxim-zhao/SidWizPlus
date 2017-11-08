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
    public partial class WaitForm : Form
    {
        public WaitForm()
        {
            InitializeComponent();
        }

        public void Progress(string a, double b)
        {
            descLabel.Text = a;
            progressBar.Value = (int) (b * 1000);
        }
    }
}

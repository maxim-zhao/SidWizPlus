using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SidWiz
{
    public partial class SidWizPlusGUI : Form
    {
        private int _columns = 1;
        private readonly List<ChannelControl> _channels = new List<ChannelControl>();

        public SidWizPlusGUI()
        {
            InitializeComponent();
        }

        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var control = new ChannelControl();
            _channels.Add(control);
            LayoutPanel.Controls.Add(control);
            LayoutChannels();
        }

        private void LayoutChannels()
        {
            if (_channels.Count == 0)
            {
                return;
            }
            var rows = _channels.Count / _columns + (_channels.Count % _columns == 0 ? 0 : 1);
            var width = LayoutPanel.Width / _columns;
            var height = LayoutPanel.Height / rows;
            for (int i = 0; i < _channels.Count; ++i)
            {
                var control = _channels[i];
                control.Left = width * (i % _columns);
                control.Top = height * (i / _columns);
                control.Width = width;
                control.Height = height;
            }
        }

        private void addToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ++_columns;
            LayoutChannels();
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_columns > 1)
            {
                --_columns;
            }
            LayoutChannels();
        }

        private void LayoutPanel_Resize(object sender, EventArgs e)
        {
            LayoutChannels();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void openWAVsToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void ColumnsControl_ValueChanged(object sender, EventArgs e)
        {
            _columns = (int) ColumnsControl.Value;
            LayoutChannels();
        }
    }
}

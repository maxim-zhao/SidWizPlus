using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
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

        private void LayoutPanel_Resize(object sender, EventArgs e)
        {
            LayoutChannels();
        }

        private void ColumnsControl_ValueChanged(object sender, EventArgs e)
        {
            _columns = (int) ColumnsControl.Value;
            LayoutChannels();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog()
            {
                CheckFileExists = true,
                Filter =
                    "All supported files (*.wav;*.vgm)|*.wav;*.vgm|" +
                    "Wave audio files (*.wav)|*.wav|" +
                    "VGM music files (*.vgm)|*.vgm|" +
                    "All files (*.*)|*.*",
                Multiselect = true
            })
            {
                if (ofd.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                var errors = new List<string>();
                foreach (var filename in ofd.FileNames)
                {
                    var path = Path.GetFullPath(filename);
                    switch (Path.GetExtension(filename)?.ToLowerInvariant())
                    {
                        case ".vgm":
                            loadVgm(path);
                            break;
                        case ".wav":
                            loadWav(path);
                            break;
                        default:
                            errors.Add($"Could not load \"{filename}\"");
                            break;
                    }
                }

                if (errors.Count > 0)
                {
                    MessageBox.Show(this, "Error(s) loading files:\n" + string.Join("\n", errors));
                }
            }
        }

        private void loadWav(string filename)
        {
            // We create a new ChannelControl
            var control = new ChannelControl();
            control.Filename = filename;
            _channels.Add(control);
            LayoutPanel.Controls.Add(control);
            LayoutChannels();
        }

        private void loadVgm(string filename)
        {
            throw new NotImplementedException();
        }

        private void colorButton3_BackColorChanged(object sender, EventArgs e)
        {
            LayoutPanel.BackColor = colorButton3.Color;
        }
    }
}

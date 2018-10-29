using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using LibSidWiz.Triggers;

namespace SidWiz
{
    public partial class ChannelControl : UserControl
    {
        private string _filename;

        public ChannelControl()
        {
            InitializeComponent();

            if (!DesignMode)
            {
                // Make sure we have LibSidWiz loaded
                var i = typeof(ITriggerAlgorithm);
                algorithmsCombo.Items.AddRange(AppDomain.CurrentDomain
                    .GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .Where(t => i.IsAssignableFrom(t) && t != i)
                    .ToArray<object>());
                algorithmsCombo.SelectedItem = typeof(PeakSpeedTrigger);
            }
        }

        public string Filename
        {
            get => _filename;
            set
            {
                _filename = value;
                TitleLabel.Text = Path.GetFileNameWithoutExtension(_filename);
            }
        }

        private void FilenameButtonClick(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog() {Filter = "WAV files (*.wav)|*.wav|All files (*.*)|*.*"})
            {
                if (ofd.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                _filename = ofd.FileName;
            }
        }

        private void highPassFilterCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            highPassFilterFrequency.Enabled = HighPassFilterCheckbox.Checked;
        }

        private void ConfigureToggleButton_Click(object sender, EventArgs e)
        {
            tableLayoutPanel1.Visible = !tableLayoutPanel1.Visible;
        }
    }
}

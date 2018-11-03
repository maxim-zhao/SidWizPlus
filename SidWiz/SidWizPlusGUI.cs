using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibSidWiz;
using LibSidWiz.Triggers;

namespace SidWiz
{
    public partial class SidWizPlusGui : Form
    {
        private int _columns = 1;
        private readonly List<ChannelControl> _channels = new List<ChannelControl>();
        private string _multiDumperPath;

        public SidWizPlusGui()
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
            _columns = (int) Columns.Value;
            LayoutChannels();
        }

        private void AddAFileClick(object sender, EventArgs e)
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
                    switch (Path.GetExtension(filename).ToLowerInvariant())
                    {
                        case ".vgm":
                            LoadVgm(path);
                            break;
                        case ".wav":
                            LoadWav(path);
                            break;
                        default:
                            errors.Add($"Could not load \"{filename}\" - unknown extension");
                            break;
                    }
                }

                if (errors.Count > 0)
                {
                    MessageBox.Show(this, "Error(s) loading files:\n" + string.Join("\n", errors));
                }
            }
        }

        private void LoadWav(string filename)
        {
            // We create a new ChannelControl
            var control = new ChannelControl(this);
            _channels.Add(control);
            LayoutPanel.Controls.Add(control);
            LayoutChannels();
            // We create the Channel object a little later to allow it to render properly
            control.Channel = new Channel
            {
                Filename = filename,
                Algorithm = new PeakSpeedTrigger()
            };
        }

        private void LoadVgm(string filename)
        {
            // Get path if we don't already have it
            // TODO Save on shutdown
            // TODO make it editable?
            if (!File.Exists(_multiDumperPath))
            {
                using (var ofd = new OpenFileDialog
                {
                    Title = "Please locate MultiDumper",
                    Filter = "Multidumper (multidumper.exe)|multidumper.exe|All files (*.*)|*.*"
                })
                {
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        _multiDumperPath = ofd.FileName;
                    }
                }
            }

            try
            {
                // Normalize path
                filename = Path.GetFullPath(filename);
                // Let's run it
                using (var p = Process.Start(new ProcessStartInfo
                {
                    FileName = _multiDumperPath,
                    Arguments = $"\"{filename}\" 0",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                }))
                {
                    // We don't actually consume its stdout, we just want to have it not shown as it makes it much slower...
                    if (p != null)
                    {
                        p.BeginOutputReadLine();
                        p.WaitForExit();
                    }
                }

                // Then check for generated files
                var directory = Path.GetDirectoryName(filename);
                if (directory != null)
                {
                    foreach (var file in Directory.EnumerateFiles(directory,
                        Path.GetFileNameWithoutExtension(filename) + " - *.wav"))
                    {
                        LoadWav(file);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error running MultiDumper: {ex}");
            }
        }

        private void colorButton3_BackColorChanged(object sender, EventArgs e)
        {
            //LayoutPanel.BackColor = colorButton3.Color;
        }

        private void AutoScale_Click(object sender, EventArgs e)
        {
            var loadTasks = _channels.Select(control => control.LoadTask).ToArray();
            while (loadTasks.Any(t => t == null || !t.IsCompleted))
            {
                // We wait for them to finish loading
                Task.WaitAll(loadTasks, TimeSpan.FromSeconds(0.1));
                Application.DoEvents();
            }
            // Then apply it
            if (float.TryParse(VerticalScaling.Text, out var percentage))
            {
                var scale = percentage / 100 / _channels.Max(control => control.Channel.Max);
                foreach (var channel in _channels.Select(control => control.Channel))
                {
                    channel.Scale = scale;
                }
            }
        }

        internal void RemoveChannel(ChannelControl control)
        {
            _channels.Remove(control);
            LayoutChannels();
            control.Dispose();
        }

        internal void MoveChannel(ChannelControl control, int amount)
        {
            var index = _channels.IndexOf(control);
            if (index == -1)
            {
                return;
            }
            var newIndex = index + amount;
            if (newIndex < 0 || newIndex >= _channels.Count)
            {
                return;
            }
            _channels.RemoveAt(index);
            _channels.Insert(newIndex, control);
            LayoutChannels();
        }
    }
}

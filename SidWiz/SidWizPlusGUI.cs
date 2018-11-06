using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using LibSidWiz;
using LibSidWiz.Triggers;

namespace SidWiz
{
    public partial class SidWizPlusGui : Form
    {
        private int _columns = 1;
        private readonly List<Channel> _channels = new List<Channel>();
        private string _multiDumperPath;

        public SidWizPlusGui()
        {
            InitializeComponent();
        }

        private void ColumnsControl_ValueChanged(object sender, EventArgs e)
        {
            _columns = (int) Columns.Value;
            Render();
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
                foreach (var filename in ofd.FileNames.OrderByAlphaNumeric(x => x))
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
            // We create a new Channel
            var channel = new Channel
            {
                Filename = filename,
                Algorithm = new PeakSpeedTrigger()
            };
            channel.Changed += ChannelOnChanged;
            channel.LoadDataAsync(); // in a worker thread
            _channels.Add(channel);
        }

        private void ChannelOnChanged(Channel channel, bool filenameChanged)
        {
            // If the filename changed then we do a load
            if (filenameChanged)
            {
                channel.LoadDataAsync();
            }
            Render();
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

        private void AutoScale_Click(object sender, EventArgs e)
        {
            // Apply to all channels which have loaded
            if (float.TryParse(VerticalScaling.Text, out var percentage) && _channels.Count > 0)
            {
                var scale = percentage / 100 / _channels.Max(channel => channel.Max);
                foreach (var channel in _channels)
                {
                    channel.Scale = scale;
                }
            }
        }

        private void Render()
        {
            var width = int.Parse(WidthControl.Text);
            var height = int.Parse(HeightControl.Text);
            var marginTop = (int) MarginTopControl.Value;
            var marginLeft = (int) MarginLeftControl.Value;
            var marginRight = (int) MarginRightControl.Value;
            var marginBottom = (int) MarginBottomControl.Value;
            var bounds = new Rectangle(
                marginLeft,
                marginTop,
                width - marginLeft - marginRight,
                height - marginTop - marginBottom);
            var renderer = new WaveformRenderer
            {
                BackgroundColor = BackgroundColorButton.Color,
                BackgroundImage = BackgroundImageControl.Image,
                Width = width,
                Height = height,
                Columns = _columns,
                FramesPerSecond = (int) FrameRateControl.Value,
                RenderingBounds = bounds,
                Grid = GridEnabled.Checked ? new WaveformRenderer.GridConfig
                {
                    Color = GridColor.Color,
                    DrawBorder = GridBorders.Checked,
                    Width = (float) GridWidth.Value
                } : null,
                ZeroLine = ZeroLineEnabled.Checked ? new WaveformRenderer.ZeroLineConfig
                {
                    Color = ZeroLineColor.Color,
                    Width = (float)ZeroLineWidth.Value
                } : null
                // TODO more?
            };
            foreach (var channel in _channels)
            {
                renderer.AddChannel(channel);
            }

            var bitmap = renderer.RenderFrame();
            var oldImage = Preview.Image;
            Preview.Image = bitmap;
            oldImage?.Dispose();
        }

        private void LeftButton_Click(object sender, EventArgs e)
        {
            MoveChannel(PropertyGrid.SelectedObject as Channel, -1);
        }
        private void RightButton_Click(object sender, EventArgs e)
        {
            MoveChannel(PropertyGrid.SelectedObject as Channel, +1);
        }

        private void MoveChannel(Channel channel, int delta)
        {
            var index = _channels.IndexOf(channel);
            if (index == -1)
            {
                return;
            }
            var newIndex = index + delta;
            if (newIndex < 0 || newIndex >= _channels.Count)
            {
                return;
            }
            _channels.RemoveAt(index);
            _channels.Insert(newIndex, channel);
            Render();
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            if (!(PropertyGrid.SelectedObject is Channel channel))
            {
                return;
            }
            channel.Changed -= ChannelOnChanged;
            var index = _channels.IndexOf(channel);
            if (index > -1)
            {
                _channels.RemoveAt(index);
                PropertyGrid.SelectedObject = null;
                Render();
            }
        }

        private void Preview_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }
            // Determine which channel was clicked
            var column = e.X * _columns / Preview.Width;
            var row = e.Y * _channels.Count / (Preview.Height * _columns);
            var index = row * _columns + column;
            if (index >= _channels.Count)
            {
                PropertyGrid.SelectedObject = null;
            }
            else
            {
                PropertyGrid.SelectedObject = _channels[index];
            }
        }

        private void CopySettingsButton_Click(object sender, EventArgs e)
        {
            var source = PropertyGrid.SelectedObject as Channel;
            if (source == null)
            {
                return;
            }

            foreach (var propertyInfo in typeof(Channel)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanWrite && 
                    p.Name != nameof(Channel.Filename) && 
                    p.Name != nameof(Channel.Name)))
            {
                var sourceValue = propertyInfo.GetValue(source);

                foreach (var channel in _channels.Where(channel => channel != source))
                {
                    propertyInfo.SetValue(channel, sourceValue);
                }
            }
        }

        private void UpdatePreview(object sender, EventArgs e)
        {
            Render();
        }

        private void BackgroundImageControl_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog()
            {
                Title = "Select an image",
                Filter =
                    "Image files (*.png;*.gif;*.jpg;*.jpeg;*.bmp;*.wmf)|*.png;*.gif;*.jpg;*.jpeg;*.bmp;*.wmf|All files (*.*)|*.*"
            })
            {
                if (ofd.ShowDialog(this) != DialogResult.OK)
                {
                    BackgroundImageControl.Image = null;
                }
                else
                {
                    BackgroundImageControl.ImageLocation = ofd.FileName;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using LibSidWiz;
using LibSidWiz.Outputs;
using LibSidWiz.Triggers;
using Newtonsoft.Json;

namespace SidWiz
{
    public partial class SidWizPlusGui : Form
    {
        private int _columns = 1;
        private readonly List<Channel> _channels = new List<Channel>();

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private class Settings
        {
            public string FFMPEGPath { get; set; }
            public string MultiDumperPath { get; set; }
        }

        private Settings _settings = new Settings();

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
            var multiDumperExtensions = new[]
            {
                "ay", "gbs", "gym", "hes", "kss", "nsf", "nsfe", "sap", "sfm", "sgc", "spc", "vgm", "spu"
            };
            var multiDumperMask = "*." + string.Join("; *.", multiDumperExtensions);

            using (var ofd = new OpenFileDialog()
            {
                CheckFileExists = true,
                Filter =
                    $"All supported files (*.wav;{multiDumperMask})|*.wav;{multiDumperMask}|" +
                    "Wave audio files (*.wav)|*.wav|" + 
                    $"MultiDumper compatible files ({multiDumperMask})|{multiDumperMask}|" +
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
                    var extension = Path.GetExtension(filename).ToLowerInvariant();
                    switch (extension)
                    {
                        case ".wav":
                            LoadWav(path);
                            break;
                        default:
                            if (multiDumperExtensions.Contains(extension))
                            {
                                LoadMultiDumper(path);
                            }
                            else
                            {
                                errors.Add($"Could not load \"{filename}\" - unknown extension");
                            }
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

            BeginInvoke(new Action(Render));
        }

        private void LoadMultiDumper(string filename)
        {
            LocateProgram("multidumper.exe", _settings.MultiDumperPath, p => _settings.MultiDumperPath = p);
            try
            {
                // Normalize path
                filename = Path.GetFullPath(filename);
                // Let's run it
                using (var p = Process.Start(new ProcessStartInfo
                {
                    FileName = _settings.MultiDumperPath,
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

        private void LocateProgram(string filename, string currentValue, Action<string> saveToSettings)
        {
            // Get path if we don't already have it
            if (!File.Exists(currentValue))
            {
                // Check if it's in the program directory
                var directory = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
                if (directory != null)
                {
                    var path = Path.Combine(directory, filename);
                    if (File.Exists(path))
                    {
                        saveToSettings(path);
                        SaveSettings();
                        return;
                    }
                }
                // Else browse for it
                using (var ofd = new OpenFileDialog
                {
                    Title = $"Please locate {filename}",
                    Filter = $"{filename}|{filename}|All files (*.*)|*.*"
                })
                {
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        saveToSettings(ofd.FileName);
                        SaveSettings();
                    }
                }
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
            // Create a renderer
            var renderer = CreateWaveformRenderer();

            // Render a bitmap
            var bitmap = renderer.RenderFrame((float)PreviewTrackbar.Value / PreviewTrackbar.Maximum);

            // Swap it with whatever is in the preview control
            var oldImage = Preview.Image;
            Preview.Image = bitmap;
            oldImage?.Dispose();
        }

        private WaveformRenderer CreateWaveformRenderer()
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
                Grid = GridEnabled.Checked
                    ? new WaveformRenderer.GridConfig
                    {
                        Color = GridColor.Color,
                        DrawBorder = GridBorders.Checked,
                        Width = (float) GridWidth.Value
                    }
                    : null,
            };
            if (_channels.Count > 0)
            {
                // We don't support multiple sampling rates, but this lets us ignore "empty" tracks.
                renderer.SamplingRate = _channels.Max(c => c.SampleRate);
            }
            foreach (var channel in _channels)
            {
                renderer.AddChannel(channel);
            }

            return renderer;
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
                    p.GetSetMethod() != null &&
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

        private void FfmpegLocation_Click(object sender, EventArgs e)
        {
            LocateProgram("ffmpeg.exe", _settings.FFMPEGPath, p => _settings.FFMPEGPath = p);
            FfmpegLocation.Text = _settings.FFMPEGPath;
        }

        private void RenderButton_Click(object sender, EventArgs e)
        {
            var outputs = new List<IGraphicsOutput>();

            if (PreviewCheckBox.Checked)
            {
                outputs.Add(new PreviewOutput((int) PreviewFrameskip.Value));
            }

            if (EncodeCheckBox.Checked)
            {
                using (var sfd = new SaveFileDialog
                {
                    Title = "Select destination",
                    Filter = "Video files (*.mp4;*.mkv;*.avi;*.qt)|*.mp4;*.mkv;*.avi;*.qt|All files (*.*)|*.*"
                })
                {
                    if (sfd.ShowDialog(this) != DialogResult.OK)
                    {
                        // Cancel the whole operation
                        return;
                    }

                    var outputFilename = sfd.FileName;

                    if (AutogenerateMasterMix.Checked && !string.IsNullOrEmpty(outputFilename))
                    {
                        var filename = outputFilename + ".wav";
                        Mixer.MixToFile(_channels, filename, MasterMixReplayGain.Checked);
                        MasterAudioPath.Text = filename;
                    }
                    
                    outputs.Add(new FfmpegOutput(
                        FfmpegLocation.Text,
                        outputFilename,
                        int.Parse(WidthControl.Text),
                        int.Parse(HeightControl.Text),
                        (int) FrameRateControl.Value,
                        FfmpegParameters.Text,
                        MasterAudioPath.Text));
                }
            }

            if (outputs.Count == 0)
            {
                // Nothing to do
                return;
            }

            try
            {
                // TODO: show some progress if no preview?
                // TODO: need to make GUI updates thread safe then o this on a background thread
                var renderer = CreateWaveformRenderer();
                renderer.Render(outputs);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
            finally
            {
                foreach (var graphicsOutput in outputs)
                {
                    graphicsOutput.Dispose();
                }
            }
        }

        private void AutogenerateMasterMix_CheckedChanged(object sender, EventArgs e)
        {
            MasterMixReplayGain.Enabled = AutogenerateMasterMix.Checked;
        }

        private void MasterAudioPath_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog
            {
                Title = "Select master audio file",
                Filter = "Wave audio files (*.wav)|*.wav|All files (*.*)|*.*"
            })
            {
                if (ofd.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                MasterAudioPath.Text = ofd.FileName;
            }
        }

        private void SaveSettings()
        {
            var path = GetSettingsPath();
            var directory = Path.GetDirectoryName(path);
            if (directory == null)
            {
                return;
            }
            Directory.CreateDirectory(directory);
            using (var file = File.CreateText(path))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(file, _settings);
            }
        }

        private static string GetSettingsPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SidWizPlus",
                "settings.json");
        }

        private void SidWizPlusGui_Load(object sender, EventArgs e)
        {
            try
            {
                _settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(GetSettingsPath()));
                FfmpegLocation.Text = _settings.FFMPEGPath;
            }
            catch (Exception)
            {
                // Swallow it
            }
        }
    }
}

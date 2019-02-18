using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibSidWiz;
using LibSidWiz.Outputs;
using LibSidWiz.Triggers;
using Newtonsoft.Json;

namespace SidWiz
{
    public partial class SidWizPlusGui : Form
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private class ProgramLocationSettings
        {
            public string FFMPEGPath { get; set; }
            public string MultiDumperPath { get; set; }
        }

        private ProgramLocationSettings _programLocationSettings = new ProgramLocationSettings();

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
        private class Settings
        {
            private bool _ignoreFromControls;
            public int Columns { get; set; } = 1;
            public List<Channel> Channels { get; } = new List<Channel>();
            public float AutoScaleHeight { get; set; } = 100;
            public int Width { get; set; } = 1280;
            public int Height { get; set; } = 720;
            public int MarginTop { get; set; }
            public int MarginLeft { get; set; }
            public int MarginRight { get; set; }
            public int MarginBottom { get; set; }
            public int FrameRate { get; set; } = 60;
            public Color BackgroundColor { get; set; } = Color.Black;
            public string BackgroundImageFilename { get; set; }
            public PreviewSettings Preview { get; } = new PreviewSettings {Enabled = true, Frameskip = 1};
            public EncodeSettings EncodeVideo { get; } = new EncodeSettings {Enabled = false};
            public MasterAudioSettings MasterAudio { get; } = new MasterAudioSettings {IsAutomatic = true, ApplyReplayGain = true};

            public class MasterAudioSettings
            {
                public bool IsAutomatic { get; set; }
                public bool ApplyReplayGain { get; set; }
                public string Path { get; set; }
            }

            public class EncodeSettings
            {
                public bool Enabled { get; set; }
                public string FfmpegParameters { get; set; }
            }

            public class PreviewSettings
            {
                public bool Enabled { get; set; }
                public int Frameskip { get; set; }
            }

            public void FromControls(SidWizPlusGui form)
            {
                if (_ignoreFromControls)
                {
                    return;
                }
                AutoScaleHeight = float.Parse(form.VerticalScaling.Text);
                Width = int.Parse(form.WidthControl.Text);
                Height = int.Parse(form.HeightControl.Text);
                Columns = (int) form.Columns.Value;
                MarginTop = (int) form.MarginTopControl.Value;
                MarginLeft = (int) form.MarginLeftControl.Value;
                MarginRight = (int) form.MarginRightControl.Value;
                MarginBottom = (int) form.MarginBottomControl.Value;
                FrameRate = (int) form.FrameRateControl.Value;
                BackgroundColor = form.BackgroundColorButton.Color;
                Preview.Enabled = form.PreviewCheckBox.Checked;
                Preview.Frameskip = (int) form.PreviewFrameskip.Value;
                EncodeVideo.Enabled = form.EncodeCheckBox.Checked;
                EncodeVideo.FfmpegParameters = form.FfmpegParameters.Text;
                MasterAudio.IsAutomatic = form.AutogenerateMasterMix.Checked;
                MasterAudio.ApplyReplayGain = form.MasterMixReplayGain.Checked;
                MasterAudio.Path = form.MasterAudioPath.Text;
            }

            public void ToControls(SidWizPlusGui form)
            {
                // Disable control notifications as we load values into them
                _ignoreFromControls = true;
                form.VerticalScaling.Text = AutoScaleHeight.ToString(CultureInfo.CurrentCulture);
                form.WidthControl.Text = Width.ToString();
                form.HeightControl.Text = Height.ToString();
                form.Columns.Value = Columns;
                form.MarginTopControl.Value = MarginTop;
                form.MarginLeftControl.Value = MarginLeft;
                form.MarginRightControl.Value = MarginRight;
                form.MarginBottomControl.Value = MarginBottom;
                form.FrameRateControl.Value = FrameRate;
                form.BackgroundColorButton.Color = BackgroundColor;
                form.PreviewCheckBox.Checked = Preview.Enabled;
                form.PreviewFrameskip.Value = Preview.Frameskip;
                form.EncodeCheckBox.Checked = EncodeVideo.Enabled;
                form.FfmpegParameters.Text = EncodeVideo.FfmpegParameters;
                form.AutogenerateMasterMix.Checked = MasterAudio.IsAutomatic;
                form.MasterMixReplayGain.Checked = MasterAudio.ApplyReplayGain;
                form.MasterAudioPath.Text = MasterAudio.Path;
                _ignoreFromControls = false;
            }

            public Rectangle GetBounds()
            {
                return new Rectangle(
                    MarginLeft,
                    MarginTop,
                    Width - MarginLeft - MarginRight,
                    Height - MarginTop - MarginBottom);
            }
        }

        private readonly Settings _settings = new Settings();

        // We do rendering on a worker thread, this means we have some complicated interactions
        // to make sure it doesn't render more or less than needed. This lot deals with that.
        private readonly object _renderLock = new object();
        private bool _renderNeeded;
        private bool _renderActive;
        private float _renderPosition;

        public SidWizPlusGui()
        {
            InitializeComponent();
        }

        private void AddAFileClick(object sender, EventArgs e)
        {
            var multiDumperMask = "*." + string.Join(
                "; *.", 
                "ay", "gbs", "gym", "hes", "kss", "nsf", "nsfe", "sap", "sfm", "sgc", "spc", "vgm", "vgz", "spu");

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
                            AddChannel(path);
                            break;
                        default:
                            if (multiDumperMask.Contains("*" + extension))
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

        private void AddChannelButton_Click(object sender, EventArgs e)
        {
            AddChannel("");
        }

        private void CloneChannelButton_Click(object sender, EventArgs e)
        {
            lock (_settings)
            {
                var source = PropertyGrid.SelectedObject as Channel;
                var index = _settings.Channels.IndexOf(source);
                if (index == -1)
                {
                    return;
                }

                // Duplicate the channel
                var channel = new Channel();
                foreach (var propertyInfo in typeof(Channel)
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(p => p.CanWrite && p.GetSetMethod() != null))
                {
                    var sourceValue = propertyInfo.GetValue(source);
                    propertyInfo.SetValue(channel, sourceValue);
                }
                // Insert it after the selected one
                _settings.Channels.Insert(index + 1, channel);
                // We attach to the event last, so we must also trigger it to load the data.
                channel.Changed += ChannelOnChanged;
                channel.LoadDataAsync();
            }

            Render();
        }


        private void AddChannel(string filename)
        {
            // We create a new Channel
            var channel = new Channel
            {
                Algorithm = new PeakSpeedTrigger(),
                LabelColor = Color.White,
                LabelFont = new Font(DefaultFont, FontStyle.Regular)
            };
            channel.Changed += ChannelOnChanged;
            lock (_settings)
            {
                _settings.Channels.Add(channel);
            }
            // Setting the filename triggers a load
            channel.Filename = filename;
            // We trigger a render to show the "loading" state
            Render();
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
            LocateProgram("multidumper.exe", _programLocationSettings.MultiDumperPath, p => _programLocationSettings.MultiDumperPath = p);
            try
            {
                // Normalize path
                filename = Path.GetFullPath(filename);

                using (var form = new MultiDumperForm(filename, _programLocationSettings.MultiDumperPath))
                {
                    if (form.ShowDialog(this) != DialogResult.OK || form.Filenames == null)
                    {
                        return;
                    }

                    foreach (var wavFile in form.Filenames)
                    {
                        AddChannel(wavFile);
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
            lock (_settings)
            {
                if (_settings.Channels.Count <= 0)
                {
                    return;
                }

                // Compute the scale for the channel with the highest value
                var scale = _settings.AutoScaleHeight / 100 / _settings.Channels.Max(channel => channel.Max);
                foreach (var channel in _settings.Channels)
                {
                    channel.Scale = scale;
                }
            }
        }

        private void Render()
        {
            // If this is called while a render is in process, then wait until that is done, and do one at the end.
            lock (_renderLock)
            {
                // We update this here so the worker thread will get the latest value.
                _renderPosition = (float) PreviewTrackbar.Value / PreviewTrackbar.Maximum;

                // We have two flags to signal the need to render.
                // One indicates that we need to render; this can be set while rendering
                // to cause it to render again when done.
                if (_renderNeeded)
                {
                    return;
                }
                _renderNeeded = true;

                // The second flag indicates that we have started the task to render.
                // This ensures we don't start two render tasks at the same time.
                if (_renderActive)
                {
                    return;
                }
                _renderActive = true;
            }

            // And finally we start the task.
            Task.Factory.StartNew(() =>
            {
                // We repeatedly render while the flag says we need to render.
                for (;;)
                {
                    float renderPosition;
                    lock (_renderLock)
                    {
                        _renderNeeded = false;
                        renderPosition = _renderPosition;
                    }

                    // Create a renderer
                    var renderer = CreateWaveformRenderer();

                    // Render a bitmap
                    var bitmap = renderer.RenderFrame(renderPosition);

                    BeginInvoke(new Action(() =>
                    {
                        // Swap it with whatever is in the preview control
                        var oldImage = Preview.Image;
                        Preview.Image = bitmap;
                        Preview.Refresh();
                        oldImage?.Dispose();
                    }));

                    // If the flag got set while we were rendering, do it again
                    lock (_renderLock)
                    {
                        if (_renderNeeded)
                        {
                            continue;
                        }
                        _renderActive = false;
                        break;
                    }
                }
            });
        }

        private WaveformRenderer CreateWaveformRenderer()
        {
            // This can be on a worker thread so we need to lock...
            lock (_settings)
            {
                var renderer = new WaveformRenderer
                {
                    BackgroundColor = _settings.BackgroundColor,
                    BackgroundImage = File.Exists(_settings.BackgroundImageFilename)
                        ? Image.FromFile(_settings.BackgroundImageFilename)
                        : null,
                    Width = _settings.Width,
                    Height = _settings.Height,
                    Columns = _settings.Columns,
                    FramesPerSecond = _settings.FrameRate,
                    RenderingBounds = _settings.GetBounds(),
                };

                if (_settings.Channels.Count > 0)
                {
                    // We don't support multiple sampling rates, but this lets us ignore "empty" tracks.
                    renderer.SamplingRate = _settings.Channels.Max(c => c.SampleRate);
                }

                foreach (var channel in _settings.Channels)
                {
                    renderer.AddChannel(channel);
                }

                return renderer;
            }
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
            lock (_settings)
            {
                var index = _settings.Channels.IndexOf(channel);
                if (index == -1)
                {
                    return;
                }

                var newIndex = index + delta;
                if (newIndex < 0 || newIndex >= _settings.Channels.Count)
                {
                    return;
                }

                _settings.Channels.RemoveAt(index);
                _settings.Channels.Insert(newIndex, channel);
            }

            Render();
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            if (!(PropertyGrid.SelectedObject is Channel channel))
            {
                return;
            }
            channel.Changed -= ChannelOnChanged;
            lock (_settings)
            {
                _settings.Channels.Remove(channel);
            }
            channel.Dispose();
            PropertyGrid.SelectedObject = null;
            Render();
        }

        private void Preview_MouseClick(object sender, MouseEventArgs e)
        {
            // Determine which channel was clicked
            // This is tricky because the preview is scaling the image to fit, but we don't know the details
            // So we need to map the click into the image space
            // First we map the click to the preview space 
            var x = (double) e.X / Preview.Width;
            var y = (double) e.Y / Preview.Height;
            // Next we map that to image space
            var imageAspectRatio = (double) Preview.Image.Width / Preview.Image.Height;
            var previewAspectRatio = (double) Preview.Width / Preview.Height;
            if (previewAspectRatio > imageAspectRatio)
            {
                // Preview is wider, we have pillarboxing
                // So the y one is correct, x needs to be modified:
                // +--+-----+--+
                // |  |     |  |
                // +--+-----+--+
                x = (x - 0.5) * previewAspectRatio / imageAspectRatio + 0.5;
                if (x < 0 || x > 1)
                {
                    return;
                }
            }
            else
            {
                // Image is wider, we have letterboxing
                y = (y - 0.5) * imageAspectRatio  / previewAspectRatio + 0.5;
                if (y < 0 || y > 1)
                {
                    return;
                }
            }
            // Then we map that to the row/column space
            lock (_settings)
            {
                var column = (int) (_settings.Columns * x);
                var numRows = _settings.Channels.Count / _settings.Columns +
                              (_settings.Channels.Count % _settings.Columns == 0 ? 0 : 1);
                var row = (int) (numRows * y);
                var index = row * _settings.Columns + column;
                if (index >= _settings.Channels.Count)
                {
                    PropertyGrid.SelectedObject = null;
                }
                else
                {
                    PropertyGrid.SelectedObject = _settings.Channels[index];
                    tabControl.SelectedTab = channelsTab;
                }
            }
        }

        private void CopySettingsButton_Click(object sender, EventArgs e)
        {
            if (!(PropertyGrid.SelectedObject is Channel source))
            {
                return;
            }

            lock (_settings)
            {
                foreach (var propertyInfo in typeof(Channel)
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(p => p.CanWrite &&
                                p.GetSetMethod() != null &&
                                p.Name != nameof(Channel.Filename) &&
                                p.Name != nameof(Channel.Label)))
                {
                    var sourceValue = propertyInfo.GetValue(source);

                    foreach (var channel in _settings.Channels.Where(channel => channel != source))
                    {
                        propertyInfo.SetValue(channel, sourceValue);
                    }
                }
            }
        }

        private void ControlValueChanged(object sender, EventArgs e)
        {
            try
            {
                lock (_settings)
                {
                    _settings.FromControls(this);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }

            Render();
        }

        private void BackgroundImageControl_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog()
            {
                Title = "Select an image",
                Filter = "Image files (*.png;*.gif;*.jpg;*.jpeg;*.bmp;*.wmf)|*.png;*.gif;*.jpg;*.jpeg;*.bmp;*.wmf|All files (*.*)|*.*"
            })
            {
                if (ofd.ShowDialog(this) != DialogResult.OK)
                {
                    BackgroundImageControl.Image = null;
                    lock (_settings)
                    {
                        _settings.BackgroundImageFilename = null;
                    }
                }
                else
                {
                    BackgroundImageControl.ImageLocation = ofd.FileName;
                    lock (_settings)
                    {
                        _settings.BackgroundImageFilename = ofd.FileName;
                    }
                }
                Render();
            }
        }

        private void FfmpegLocation_Click(object sender, EventArgs e)
        {
            LocateProgram("ffmpeg.exe", _programLocationSettings.FFMPEGPath, p => _programLocationSettings.FFMPEGPath = p);
            FfmpegLocation.Text = _programLocationSettings.FFMPEGPath;
        }

        private void RenderButton_Click(object sender, EventArgs e)
        {
            var outputs = new List<IGraphicsOutput>();

            lock (_settings)
            {
                if (_settings.Preview.Enabled)
                {
                    outputs.Add(new PreviewOutput(_settings.Preview.Frameskip));
                }

                if (_settings.EncodeVideo.Enabled)
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

                        if (_settings.MasterAudio.IsAutomatic && !string.IsNullOrEmpty(outputFilename))
                        {
                            var filename = outputFilename + ".wav";
                            Mixer.MixToFile(_settings.Channels, filename, MasterMixReplayGain.Checked);
                            MasterAudioPath.Text = filename;
                            _settings.MasterAudio.Path = filename;
                        }

                        outputs.Add(new FfmpegOutput(
                            _programLocationSettings.FFMPEGPath,
                            outputFilename,
                            _settings.Width,
                            _settings.Height,
                            _settings.FrameRate,
                            _settings.EncodeVideo.FfmpegParameters,
                            _settings.MasterAudio.Path));
                    }
                }

                if (outputs.Count == 0)
                {
                    // Nothing to do
                    return;
                }
            }

            try
            {
                // TODO: show some progress if no preview?
                // TODO: need to make GUI updates thread safe then do this on a background thread
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
            File.WriteAllText(path, JsonConvert.SerializeObject(_programLocationSettings));
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
                HighDpiHelper.AdjustControlImagesDpiScale(this);
                _programLocationSettings = JsonConvert.DeserializeObject<ProgramLocationSettings>(File.ReadAllText(GetSettingsPath()));
                FfmpegLocation.Text = _programLocationSettings.FFMPEGPath;
                lock (_settings)
                {
                    _settings.ToControls(this);
                }
            }
            catch (Exception)
            {
                // Swallow it
            }
        }

        private void RemoveEmptyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lock (_settings)
            {
                foreach (var channel in _settings.Channels.Where(c => c.SampleCount == 0).ToList())
                {
                    _settings.Channels.Remove(channel);
                    if (PropertyGrid.SelectedObject == channel)
                    {
                        PropertyGrid.SelectedObject = null;
                    }
                    channel.Dispose();
                }
            }

            Render();
        }

        private void RemoveAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lock (_settings)
            {
                foreach (var channel in _settings.Channels)
                {
                    channel.Dispose();
                }
                _settings.Channels.Clear();
            }
            PropertyGrid.SelectedObject = null;
            Render();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog
            {
                Filter = "SidWizPlus settings (*.sidwizplus.json)|*.sidwizplus.json|All files (*.*)|*.*"
            })
            {
                if (sfd.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                lock (_settings)
                {
                    File.WriteAllText(sfd.FileName, JsonConvert.SerializeObject(_settings, new JsonSerializerSettings
                    {
                        Formatting = Formatting.Indented,
                    }));
                }
            }
        }

        private void LoadButton_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog
            {
                Filter = "SidWizPlus settings (*.sidwizplus.json)|*.sidwizplus.json|All files (*.*)|*.*"
            })
            {
                if (ofd.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                lock (_settings)
                {
                    JsonConvert.PopulateObject(File.ReadAllText(ofd.FileName), _settings);

                    foreach (var channel in _settings.Channels)
                    {
                        channel.Changed += ChannelOnChanged;
                        channel.LoadDataAsync();
                    }

                    _settings.ToControls(this);
                }
            }
        }
    }
}

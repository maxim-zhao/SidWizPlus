using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using LibSidWiz;
using LibSidWiz.Outputs;
using LibSidWiz.Triggers;
using Newtonsoft.Json;
using SkiaSharp;

namespace SidWizPlusGUI
{
    public partial class SidWizPlusGui : Form
    {
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
        [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
        public class ProgramSettings
        {
            [Category("FFMPEG")]
            [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
            [DisplayName("Path")]
            [Description("Path to FFMPEG. Download from https://ffmpeg.org/download.html")]
            public string FfmpegPath { get; set; }
            [Category("FFMPEG")]
            [DisplayName("Global Extra Parameters")]
            [Description("Extra parameters for FFMPEG. These are stored in your user settings, and will apply to all encodes. Per-encode settings can be entered on the Video tab.")]
            public string FfmpegExtraParameters { get; set; }

            [Category("MultiDumper")]
            [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
            [DisplayName("Path")]
            [Description("Path to Multidumper. Download from https://github.com/maxim-zhao/multidumper/releases")]
            public string MultiDumperPath { get; set; }
            [Category("MultiDumper")]
            [DisplayName("Sampling Rate")]
            [Description("Sampling rate for generated WAV files. Default is 44100. Requires a build of MultiDumper that supports this!")]
            [DefaultValue(44100)]
            public int MultiDumperSamplingRate { get; set; } = 44100;
            [Category("MultiDumper")]
            [DisplayName("Loop Count")]
            [Description("Default is 2 (so looped sections will be played twice in their entirety). Requires a build of MultiDumper that supports this!")]
            [DefaultValue(2)]
            public int MultiDumperLoopCount { get; set; } = 2;
            [Category("MultiDumper")]
            [DisplayName("Fade out time (ms)")]
            [Description("Default is 8000 (8s). Requires a build of MultiDumper that supports this!")]
            [DefaultValue(8000)]
            public int MultiDumperFadeMs { get; set; } = 8000;
            [Category("MultiDumper")]
            [DisplayName("Gap time (ms)")]
            [Description("Default is 1000 (1s). Requires a build of MultiDumper that supports this!")]
            [DefaultValue(1000)]
            public int MultiDumperGapMs { get; set; } = 1000;

            [Category("SidPlay")]
            [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
            [DisplayName("Path")]
            public string SidPlayPath { get; set; }

            [Browsable(false)]
            public Channel DefaultChannelSettings { get; set; } = new(true)
            {
                Algorithm = new PeakSpeedTrigger(),
                LabelColor = Color.White,
                LabelFont = new Font(DefaultFont, FontStyle.Regular)
            };
        }

        private ProgramSettings _programSettings = new();

        // ReSharper disable MemberCanBePrivate.Local
        private class Settings
        {
            private bool _ignoreFromControls;
            public int Columns { get; set; } = 1;
            public List<Channel> Channels { get; } = [];
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
            public PreviewSettings Preview { get; } = new() {Enabled = true, Frameskip = 1};
            public EncodeSettings EncodeVideo { get; } = new() {Enabled = false, VideoCodec = "libx264", AudioCodec = "aac", ExtraParameters = ""};
            public int RenderThreads { get; set; } = Environment.ProcessorCount;

            public MasterAudioSettings MasterAudio { get; } = new() {IsAutomatic = true, ApplyReplayGain = true};

            public class MasterAudioSettings
            {
                public bool IsAutomatic { get; set; }
                public bool ApplyReplayGain { get; set; }
                public string Path { get; set; }
            }

            public class EncodeSettings
            {
                public bool Enabled { get; set; }
                public string VideoCodec { get; set; }
                public string AudioCodec { get; set; }
                public string ExtraParameters { get; set; }
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
                EncodeVideo.VideoCodec = form.VideoCodec.Text.Split([' '], 2).FirstOrDefault() ?? "libx264";
                EncodeVideo.AudioCodec = form.AudioCodec.Text.Split([' '], 2).FirstOrDefault() ?? "aac";
                EncodeVideo.ExtraParameters = form.ExtraFFMPEGParameters.Text;
                MasterAudio.IsAutomatic = form.AutogenerateMasterMix.Checked;
                MasterAudio.ApplyReplayGain = form.MasterMixReplayGain.Checked;
                MasterAudio.Path = form.MasterAudioPath.Text;
                RenderThreads = (int) form.RenderThreadsControl.Value;
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
                form.VideoCodec.Text = EncodeVideo.VideoCodec;
                form.AudioCodec.Text = EncodeVideo.AudioCodec;
                form.ExtraFFMPEGParameters.Text = EncodeVideo.ExtraParameters;
                form.AutogenerateMasterMix.Checked = MasterAudio.IsAutomatic;
                form.MasterMixReplayGain.Checked = MasterAudio.ApplyReplayGain;
                form.MasterAudioPath.Text = MasterAudio.Path;
                form.RenderThreadsControl.Value = RenderThreads;
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
        // ReSharper restore MemberCanBePrivate.Local

        private readonly Settings _settings = new();

        // We do rendering on a worker thread, this means we have some complicated interactions
        // to make sure it doesn't render more or less than needed. This lot deals with that.
        private readonly object _renderLock = new();
        private bool _renderNeeded;
        private bool _renderActive;
        private float _renderPosition;

        // We use this to allow cancelling the render
        private MainFormProgressOutput _progress;
        private FileSystemWatcher _settingsWatcher;

        public SidWizPlusGui()
        {
            InitializeComponent();
        }

        private class FileTypeHandler
        {
            private readonly Action<string> _handler;
            private readonly HashSet<string> _extensions;

            public FileTypeHandler(string name, Action<string> handler, params string[] extensions)
            {
                Name = name;
                _handler = handler;
                _extensions = [..extensions];
                Filter = "*." + string.Join("; *.", extensions);
            }

            public string Name { get; }
            public string Filter { get; }

            public bool TryHandle(string filename)
            {
                var extension = Path.GetExtension(filename)?.ToLowerInvariant();
                if (extension == null)
                {
                    return false;
                }

                if (extension.StartsWith("."))
                {
                    extension = extension.Substring(1);
                }

                if (_extensions.Contains(extension))
                {
                    _handler(filename);
                    return true;
                }

                return false;
            }
        }

        private void AddAFileClick(object sender, EventArgs e)
        {
            var handlers = new[]
            {
                // ReSharper disable once StringLiteralTypo
                new FileTypeHandler("Multidumper compatible files", LoadMultiDumper, "ay", "gbs", "gym", "hes", "kss",
                    "nsf", "nsfe", "sap", "sfm", "sgc", "spc", "vgm", "vgz", "spu"),
                new FileTypeHandler("Wave audio files", AddChannel, "wav", "mp3"),
                new FileTypeHandler("SID files", LoadSid, "sid")
            };

            var allFilesMask = string.Join("; ", handlers.Select(h => h.Filter));

            var filter = string.Join("|", new[]
                {
                    $"All supported files ({allFilesMask})",
                    allFilesMask
                }
                .Concat(handlers.SelectMany(h => new[] {$"{h.Name} ({h.Filter})", h.Filter}))
                .Concat(
                [
                    "All files", "*.*"
                ]));

            using var ofd = new OpenFileDialog();
            ofd.CheckFileExists = true;
            ofd.Filter = filter;
            ofd.Multiselect = true;
            if (ofd.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var errors = new List<string>();
            foreach (var filename in ofd.FileNames.OrderByAlphaNumeric(x => x))
            {
                var path = Path.GetFullPath(filename);
                if (!handlers.Any(h => h.TryHandle(path)))
                {
                    errors.Add($"Could not load \"{filename}\" - unknown extension");
                }
            }

            if (errors.Count > 0)
            {
                MessageBox.Show(this, "Error(s) loading files:\n" + string.Join("\n", errors));
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
                if (index == -1 || source == null)
                {
                    return;
                }

                // Duplicate the channel
                var channel = new Channel(true);
                channel.FromJson(source.ToJson(), false);
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
            var channel = new Channel(true);
            channel.FromJson(_programSettings.DefaultChannelSettings.ToJson(), true);
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

            BeginInvoke(new Action(() =>
            {
                // We check if the trackbar range needs to be changed
                lock (_settings)
                {
                    var frameRate = _settings.FrameRate;
                    var maxLength = _settings.Channels.Max(ch => ch.Length);
                    var frames = maxLength.TotalSeconds * frameRate;
                    PreviewTrackbar.Maximum = (int) frames;
                    PreviewTrackbar.LargeChange = frameRate;
                    PreviewTrackbar.TickFrequency = frameRate;
                }

                Render();
            }));
        }

        private void LoadMultiDumper(string filename)
        {
            LocateProgram("multidumper.exe", _programSettings.MultiDumperPath,
                p => _programSettings.MultiDumperPath = p);
            try
            {
                // Normalize path
                filename = Path.GetFullPath(filename);

                using var form = new MultiDumperForm(filename, _programSettings.MultiDumperPath, _programSettings.MultiDumperSamplingRate, _programSettings.MultiDumperLoopCount, _programSettings.MultiDumperFadeMs, _programSettings.MultiDumperGapMs);
                if (form.ShowDialog(this) != DialogResult.OK || form.Filenames == null)
                {
                    return;
                }

                foreach (var wavFile in form.Filenames)
                {
                    AddChannel(wavFile);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error running MultiDumper: {ex}");
            }
        }

        private void LoadSid(string filename)
        {
            LocateProgram("sidplayfp.exe", _programSettings.SidPlayPath,
                p => _programSettings.SidPlayPath = p);
            try
            {
                using var form = new SidPlayForm(filename, _programSettings.SidPlayPath);
                if (form.ShowDialog(this) != DialogResult.OK || form.Filenames == null)
                {
                    return;
                }

                foreach (var wavFile in form.Filenames)
                {
                    AddChannel(wavFile);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error running SidPlay: {ex}");
            }
        }

        private void LocateProgram(string filename, string currentValue, Action<string> saveToSettings)
        {
            // Get path if we don't already have it
            if (string.IsNullOrEmpty(currentValue) || !File.Exists(currentValue))
            {
                // Check if it's in the program directory
                var directory = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
                if (directory != null)
                {
                    var path = Path.Combine(directory, filename);
                    if (File.Exists(path))
                    {
                        saveToSettings(path);
                        SaveProgramSettings();
                        return;
                    }
                }

                // Else browse for it
                using var ofd = new OpenFileDialog();
                ofd.Title = $"Please locate {filename}";
                ofd.Filter = $"{filename}|{filename}|All files (*.*)|*.*";
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    saveToSettings(ofd.FileName);
                    SaveProgramSettings();
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
                        // ReSharper disable once AccessToModifiedClosure
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
            // This can be on a worker thread, so we need to lock...
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
                    RenderingBounds = _settings.GetBounds()
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
            if (PropertyGrid.SelectedObject is not Channel channel)
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
            PropertyGrid.SelectedObject = GetClickedChannel(e.X, e.Y);
            if (PropertyGrid.SelectedObject != null)
            {
                tabControl.SelectedTab = channelsTab;
            }
        }

        private Channel GetClickedChannel(int clickX, int clickY)
        {
            // Determine which channel was clicked
            // This is tricky because the preview is scaling the image to fit, but we don't know the details
            // So we need to map the click into the image space
            // First we map the click to the preview space in the range 0..1 in each dimension
            var x = (double) clickX / Preview.Width;
            var y = (double) clickY / Preview.Height;
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
                if (x is < 0 or > 1)
                {
                    return null;
                }
            }
            else
            {
                // Image is wider, we have letterboxing
                y = (y - 0.5) * imageAspectRatio / previewAspectRatio + 0.5;
                if (y is < 0 or > 1)
                {
                    return null;
                }
            }

            lock (_settings)
            {
                // Then we map that into the range for the borders in the image
                // 0,0------------------+
                // |                    |
                // |    0,0--------+    |
                // |    |          |    |
                // |    +--------1,1    |
                // |                    |
                // +--------------------1,1
                x = (x - (double)_settings.MarginLeft / _settings.Width) * _settings.Width /
                    (_settings.Width - _settings.MarginLeft - _settings.MarginRight);
                if (x is < 0.0 or > 1.0)
                {
                    return null;
                }

                y = (y - (double)_settings.MarginTop / _settings.Height) * _settings.Height /
                    (_settings.Height - _settings.MarginTop - _settings.MarginBottom);
                if (y is < 0.0 or > 1.0)
                {
                    return null;
                }

                // Then we map that to the row/column space
                var column = (int) (_settings.Columns * x);
                var numRows = _settings.Channels.Count / _settings.Columns +
                              (_settings.Channels.Count % _settings.Columns == 0 ? 0 : 1);
                var row = (int) (numRows * y);
                var index = row * _settings.Columns + column;
                if (index >= _settings.Channels.Count || index < 0)
                {
                    return null;
                }
                return _settings.Channels[index];
            }
        }

        private void CopySettingsButton_Click(object sender, EventArgs e)
        {
            if (PropertyGrid.SelectedObject is not Channel source)
            {
                return;
            }

            lock (_settings)
            {
                var json = source.ToJson();
                foreach (var channel in _settings.Channels.Where(channel => channel != source))
                {
                    channel.FromJson(json, true);
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
            using var ofd = new OpenFileDialog();
            ofd.Title = "Select an image";
            ofd.Filter = "Image files (*.png;*.gif;*.jpg;*.jpeg;*.bmp;*.wmf)|*.png;*.gif;*.jpg;*.jpeg;*.bmp;*.wmf|All files (*.*)|*.*";
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

        private void RenderButton_Click(object sender, EventArgs e)
        {
            if (_progress != null)
            {
                // Cancel the rendering
                _progress.Cancel();
                return;
            }

            _progress = new MainFormProgressOutput(this);
            var outputs = new List<IGraphicsOutput> {_progress};

            lock (_settings)
            {
                if (_settings.Preview.Enabled)
                {
                    outputs.Add(new PreviewOutput(_settings.Preview.Frameskip));
                }

                if (_settings.EncodeVideo.Enabled)
                {
                    LocateProgram("ffmpeg.exe", _programSettings.FfmpegPath,
                        p => _programSettings.FfmpegPath = p);

                    using var saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Title = "Select destination";
                    saveFileDialog.Filter = "Video files (*.mp4;*.mkv;*.avi;*.qt)|*.mp4;*.mkv;*.avi;*.qt|All files (*.*)|*.*";
                    if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
                    {
                        // Cancel the whole operation
                        _progress = null;
                        foreach (var output in outputs)
                        {
                            output.Dispose();
                        }
                        return;
                    }

                    var outputFilename = saveFileDialog.FileName;

                    if (_settings.MasterAudio.IsAutomatic)
                    {
                        try
                        {
                            var filename = outputFilename + ".wav";
                            Mixer.MixToFile(_settings.Channels, filename, MasterMixReplayGain.Checked);
                            MasterAudioPath.Text = filename;
                            _settings.MasterAudio.Path = filename;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(this, $"Failed to mix audio: {ex.Message}");
                        }
                    }
                    else
                    {
                        _settings.MasterAudio.Path = MasterAudioPath.Text;
                    }

                    outputs.Add(new FfmpegOutput(
                        _programSettings.FfmpegPath,
                        outputFilename,
                        _settings.Width,
                        _settings.Height,
                        _settings.FrameRate,
                        string.Join(" ", _programSettings.FfmpegExtraParameters, _settings.EncodeVideo.ExtraParameters),
                        _settings.MasterAudio.Path,
                        _settings.EncodeVideo.VideoCodec,
                        _settings.EncodeVideo.AudioCodec,
                        true));
                }
            }

            RenderButton.Text = "Cancel render";

            // Start a background thread to do the rendering work
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var renderer = CreateWaveformRenderer();
                    renderer.Render(outputs, _settings.RenderThreads, false);
                }
                catch (Exception exception)
                {
                    BeginInvoke(new Action(() => MessageBox.Show(this, exception.Message)));
                }
                finally
                {
                    foreach (var graphicsOutput in outputs)
                    {
                        graphicsOutput.Dispose();
                    }

                    _progress = null;
                    BeginInvoke(new Action(() => RenderButton.Text = "Render"));
                }
            });
        }

        private class MainFormProgressOutput : IGraphicsOutput
        {
            private readonly SidWizPlusGui _form;
            private readonly Stopwatch _stopwatch;
            private int _frameIndex;
            private DateTime _updateTime = DateTime.MinValue;
            private readonly TimeSpan _updateInterval = TimeSpan.FromMilliseconds(100);
            private bool _cancelRequested;

            public MainFormProgressOutput(SidWizPlusGui form)
            {
                _form = form;
                _stopwatch = Stopwatch.StartNew();
            }

            public void Cancel()
            {
                _cancelRequested = true;
            }

            public void Dispose()
            {
                _form.BeginInvoke(new Action(() =>
                {
                    if (_form.IsDisposed || !_form.Visible)
                    {
                        return;
                    }

                    _form.Text = "SidWizPlusGUI";
                }));
            }

            public void Write(SKImage image, byte[] data, double fractionComplete, TimeSpan length)
            {
                if (_cancelRequested)
                {
                    throw new Exception("Render cancelled");
                }

                ++_frameIndex;

                // We don't need the data, just the progress
                var now = DateTime.UtcNow;
                if (now - _updateTime < _updateInterval)
                {
                    return;
                }

                _updateTime = now;
                var elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;
                var fps = _frameIndex / elapsedSeconds;
                var eta = TimeSpan.FromSeconds(elapsedSeconds / fractionComplete - elapsedSeconds);
                _form.BeginInvoke(new Action(() =>
                {
                    if (_form.IsDisposed || !_form.Visible)
                    {
                        return;
                    }

                    _form.Text = $"SidWizPlusGUI - {fractionComplete:P} of {length} @ {fps:F}fps, ETA {eta:g}";
                }));
            }
        }

        private void AutogenerateMasterMix_CheckedChanged(object sender, EventArgs e)
        {
            MasterMixReplayGain.Enabled = AutogenerateMasterMix.Checked;
        }

        private void SetMasterAudioPath(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog();
            ofd.Title = "Select master audio file";
            ofd.Filter = "Wave audio files (*.wav)|*.wav|All files (*.*)|*.*";
            if (ofd.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            MasterAudioPath.Text = ofd.FileName;
        }

        private void LoadProgramSettings(string settingsPath)
        {
            try
            {
                _programSettings = JsonConvert.DeserializeObject<ProgramSettings>(File.ReadAllText(settingsPath));
            }
            catch (Exception)
            {
                // Ignore it
            }
        }

        private void SaveProgramSettings()
        {
            var path = GetSettingsPath();
            var directory = Path.GetDirectoryName(path);
            if (directory == null)
            {
                return;
            }

            Directory.CreateDirectory(directory);
            File.WriteAllText(path, JsonConvert.SerializeObject(_programSettings));

            // It may be filled in automatically, so we need to refresh the UI
            ProgramSettingsGrid.SelectedObject = _programSettings;
        }

        private static string GetSettingsPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SidWizPlus",
                "settings.json");
        }

        private void Initialize(object sender, EventArgs e)
        {
            HighDpiHelper.AdjustControlImagesDpiScale(this);
            var settingsPath = GetSettingsPath();
            LoadProgramSettings(settingsPath);
            if (File.Exists(settingsPath))
            {
                _settingsWatcher = new FileSystemWatcher
                {
                    Path = Path.GetDirectoryName(settingsPath),
                    Filter = Path.GetFileName(settingsPath)
                };
                _settingsWatcher.Changed += (_, _) => LoadProgramSettings(settingsPath);
            }

            ProgramSettingsGrid.BeginInvoke(new Action(() => { ProgramSettingsGrid.SelectedObject = _programSettings; }));

            lock (_settings)
            {
                _settings.ToControls(this);
            }

            // Use exe icon as form icon
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            // Replicate the right-click menu into the toolbar automatically, so I don't have to maintain both
            int indexToInsertAt = toolStrip1.Items.IndexOf(ChannelToolstripItemsSeparator) + 1;
            foreach (var item in contextMenuStrip1.Items.Cast<ToolStripItem>().Reverse())
            {
                ToolStripItem newItem = null;
                switch (item)
                {
                    case ToolStripMenuItem when item == removeChannelToolStripMenuItem:
                        // This is handled separately
                        continue;
                    case ToolStripMenuItem:
                        newItem = new ToolStripButton
                        {
                            Image = item.Image,
                            Text = item.Text,
                            DisplayStyle = ToolStripItemDisplayStyle.Image
                        };
                        newItem.Click += (_, _) => item.PerformClick();
                        break;
                    case ToolStripSeparator:
                        newItem = new ToolStripSeparator();
                        break;
                }

                if (newItem != null)
                {
                    toolStrip1.Items.Insert(indexToInsertAt, newItem);
                }
            }
        }

        private void RemoveSilentChannels(object sender, EventArgs e)
        {
            lock (_settings)
            {
                foreach (var channel in _settings.Channels.Where(c => c.IsSilent).ToList())
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

        private void RemoveAllChannels(object sender, EventArgs e)
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

        private void SaveProject(object sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog();
            sfd.Filter = "SidWizPlus settings (*.sidwizplus.json)|*.sidwizplus.json|All files (*.*)|*.*";
            if (sfd.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            lock (_settings)
            {
                File.WriteAllText(sfd.FileName, JsonConvert.SerializeObject(_settings, new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented
                }));
            }
        }

        private void LoadProject(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog();
            ofd.Filter = "SidWizPlus settings (*.sidwizplus.json)|*.sidwizplus.json|All files (*.*)|*.*";
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

        private void CopyChannelSettings(object sender, EventArgs e)
        {
            if (PropertyGrid.SelectedObject is not Channel source)
            {
                return;
            }

            Clipboard.SetText(source.ToJson());
        }

        private void PasteChannelSettings(object sender, EventArgs e)
        {
            if (PropertyGrid.SelectedObject is not Channel channel)
            {
                return;
            }

            try
            {
                channel.FromJson(Clipboard.GetText(), true);
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, $"Error while pasting: \n\n{exception}");
            }
        }

        private void SplitChannel(object sender, EventArgs e)
        {
            if (PropertyGrid.SelectedObject is not Channel channel)
            {
                return;
            }

            if (channel.IsMono())
            {
                if (MessageBox.Show(
                    this,
                    "Data is not stereo, do you want to clone the channel instead?",
                    "Split channel",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                {
                    CloneChannelButton_Click(sender, e);
                }

                return;
            }

            // We clone the channel for the right side
            var right = new Channel(true);
            var json = channel.ToJson();
            right.FromJson(json, false);
            right.Side = Channel.Sides.Right;
            right.Changed += ChannelOnChanged;
            right.LoadDataAsync();

            // We switch the existing channel to the left side
            channel.Side = Channel.Sides.Left;
            channel.LoadDataAsync();

            lock (_settings)
            {
                var index = _settings.Channels.IndexOf(channel);
                _settings.Channels.Insert(index + 1, right);
            }

            // We trigger a render to show the "loading" state
            Render();
        }

        private void RemoveAllLabels(object sender, EventArgs e)
        {
            lock (_settings)
            {
                foreach (var channel in _settings.Channels)
                {
                    channel.Label = "";
                }
            }
        }

        private void HandleClosing(object sender, FormClosingEventArgs e)
        {
            _settingsWatcher?.Dispose();
            SaveProgramSettings();
        }

        private void SaveAsDefaultSettings(object sender, EventArgs e)
        {
            if (PropertyGrid.SelectedObject is not Channel source)
            {
                return;
            }

            _programSettings.DefaultChannelSettings.FromJson(source.ToJson(), true);
            SaveProgramSettings();
        }

        private void ResetToDefaultSettings(object sender, EventArgs e)
        {
            if (PropertyGrid.SelectedObject is not Channel channel)
            {
                return;
            }

            lock (_settings)
            {
                channel.FromJson(_programSettings.DefaultChannelSettings.ToJson(), true);
            }
        }

        private void PropertyGrid_SelectedObjectsChanged(object sender, EventArgs e)
        {
            PropertyGrid.Visible = PropertyGrid.SelectedObject != null;
            ChannelsHelpLabel.Visible = PropertyGrid.SelectedObject == null;
        }

        private void VideoCodec_DropDown(object sender, EventArgs e)
        {
            if (!File.Exists(_programSettings.FfmpegPath) || AudioCodec.Items.Count > 0 || VideoCodec.Items.Count > 0)
            {
                return;
            }

            try
            {
                // We run FFMPEG to find what codecs it supports
                using var process = new ProcessWrapper(_programSettings.FfmpegPath, "-encoders", false, true);
                process.WaitForExit();
                foreach (var grouping in process
                    .Lines()
                    .Select(x => Regex.Match(x, "^ (?<type>[AV])[^ ]+ (?<name>[^ ]+) +(?<description>.+)$"))
                    .Where(m => m.Success && !m.Groups["name"].Value.Contains("="))
                    .Select(m => new Codec(m.Groups["type"].Value, m.Groups["description"].Value, m.Groups["name"].Value))
                    .GroupBy(c => c.Type))
                {
                    var combo = grouping.Key switch
                    {
                        "A" => AudioCodec,
                        "V" => VideoCodec,
                        _ => null
                    };
                    combo?.Items.Clear();
                    combo?.Items.AddRange(grouping.OrderBy(x => x.ToString()).ToArray<object>());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error querying FFMPEG: {ex.Message}");
            }
        }

        private record Codec(string Type, string Description, string Name)
        {
            public string Type { get; } = Type;
            public string Description { get; } = Description;
            public string Name { get; } = Name;

            public override string ToString()
            {
                return $"{Name} {Description}";
            }
        }
    }
}
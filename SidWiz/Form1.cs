using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NReplayGain;
using SidWiz.Outputs;
using SidWiz.Triggers;

namespace SidWiz
{
    public partial class Form1 : Form
    {
        private readonly string[] _args;
        private string _ffPath;

        public Form1(string[] args)
        {
            _args = args;
            InitializeComponent();
        }

        private void Start_Click(object sender, EventArgs e)
        {
            if (txtFile1.Text == "")
            {
                MessageBox.Show("There is no file selected for channel 1!");
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            if (_ffPath == "")
            {
                sfd.Filter = "AVI Files (.avi)|*.avi";
            }
            else
            {
                sfd.Filter = "MP4 Files (.mp4)|*.mp4|All files (*.*)|*.*";
            }

            sfd.ShowDialog();
            if (sfd.FileName == "")
            {
                return;
            }

            Enabled = false;

            try
            {
                Go(
                    filename: sfd.FileName,
                    filenames: GetInputFilenames(),
                    width: int.Parse(widthTextBox.Text),
                    height: int.Parse(heightTextBox.Text),
                    fps: (int) numFps.Value,
                    background: null,
                    logo: null,
                    vgmFile: null,
                    previewFrameskip: 16,
                    highPassFrequency: -1,
                    scale: 1,
                    triggerAlgorithm: typeof(PeakSpeedTrigger),
                    viewSamples: int.Parse(samplesTextBox.Text),
                    numColumns: (int) numColumns.Value,
                    ffMpegPath: _ffPath,
                    ffMpegExtraArgs: ffOutArgs.Text, 
                    masterAudioFilename: null,
                    autoScale: -1, 
                    gridColor: Color.Empty, gridWidth: 0, gridOuter: false, 
                    zeroLineColor: Color.Empty, 
                    zeroLineWidth: 0, 
                    lineWidth: 3);
            }
            finally
            {
                Enabled = true;
            }
        }

        private IList<string> GetInputFilenames()
        {
            return groupBox3.Controls
                .OfType<TextBox>()
                .OrderBy(c => c.TabIndex)
                .Select(c => c.Text)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        private static void Go(string filename, ICollection<string> filenames, int width, int height, int fps,
            string background,
            string logo, string vgmFile, int previewFrameskip, float highPassFrequency, float scale,
            Type triggerAlgorithm, int viewSamples, int numColumns, string ffMpegPath, string ffMpegExtraArgs,
            string masterAudioFilename, float autoScale, Color gridColor, float gridWidth, bool gridOuter,
            Color zeroLineColor, float zeroLineWidth, float lineWidth)
        {
            filename = Path.GetFullPath(filename);
            var waitForm = new WaitForm();
            waitForm.Show();

            int sampleRate;
            using (var reader = new WaveFileReader(filenames.First()))
            {
                sampleRate = reader.WaveFormat.SampleRate;
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            int stepsPerFile = 1 + (highPassFrequency > 0 ? 1 : 0) + 2;
            int totalProgress = filenames.Count * stepsPerFile;
            int progress = 0;

            var loadTask = Task.Run(() =>
            {
                // Do a parallel read of all files
                var channels = filenames.AsParallel().Select((wavFilename, channelIndex) =>
                {
                    var reader = new WaveFileReader(wavFilename);
                    var buffer = new float[reader.SampleCount];

                    // We read the file and convert to mono
                    reader.ToSampleProvider().ToMono().Read(buffer, 0, (int) reader.SampleCount);
                    Interlocked.Increment(ref progress);

                    // We don't care about ones where the samples are all equal
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (buffer.Length == 0 || buffer.All(s => s == buffer[0]))
                    {
                        // So we skip steps here
                        reader.Dispose();
                        Interlocked.Add(ref progress, stepsPerFile - 1);
                        return null;
                    }

                    if (highPassFrequency > 0)
                    {
                        // Apply the high pass filter
                        var filter = BiQuadFilter.HighPassFilter(reader.WaveFormat.SampleRate, highPassFrequency, 1);
                        for (int i = 0; i < buffer.Length; ++i)
                        {
                            buffer[i] = filter.Transform(buffer[i]);
                        }

                        Interlocked.Increment(ref progress);
                    }

                    float max = float.MinValue;
                    foreach (var sample in buffer)
                    {
                        max = Math.Max(max, Math.Abs(sample));
                    }

                    return new {Data = buffer, WavReader = reader, Max = max};
                }).Where(ch => ch != null).ToList();

                if (autoScale > 0 || scale > 1)
                {
                    // Calculate the multiplier
                    float multiplier = 1.0f;
                    if (autoScale > 0)
                    {
                        multiplier = autoScale / channels.Max(channel => channel.Max);
                    }

                    if (scale > 1)
                    {
                        multiplier *= scale;
                    }

                    // ...and we apply it
                    channels.AsParallel().Select(channel => channel.Data).ForAll(samples =>
                    {
                        for (int i = 0; i < samples.Length; ++i)
                        {
                            samples[i] *= multiplier;
                        }

                        Interlocked.Increment(ref progress);
                    });
                }

                return channels.ToList();
            });

            while (!loadTask.IsCompleted)
            {
                Application.DoEvents();
                Thread.Sleep(1);
                waitForm.Progress("Reading data...", (double) progress / totalProgress);
            }

            var voiceData = loadTask.Result.Select(channel => channel.Data).ToList();

            waitForm.Close();

            // Emit normalised data to a WAV file for later mixing
            if (masterAudioFilename == null)
            {
                // Generate a temp filename
                masterAudioFilename = filename + ".wav";
                // Mix the audio. We should probably not be re-reading it here... should do this in one pass.
                foreach (var reader in loadTask.Result.Select(channel => channel.WavReader))
                {
                    reader.Position = 0;
                }
                var mixer = new MixingSampleProvider(loadTask.Result.Select(channel => channel.WavReader.ToSampleProvider()));
                var length = (int) loadTask.Result.Max(channel => channel.WavReader.SampleCount);
                var mixedData = new float[length * mixer.WaveFormat.Channels];
                mixer.Read(mixedData, 0, mixedData.Length);
                // Then we want to deinterleave it
                var leftChannel = new float[length];
                var rightChannel = new float[length];
                for (int i = 0; i < length; ++i)
                {
                    leftChannel[i] = mixedData[i * 2];
                    rightChannel[i] = mixedData[i * 2 + 1];
                }
                // Then Replay Gain it
                // The +3 is to make it at "YouTube loudness", which is a lot louder than ReplayGain defaults to.
                var replayGain = new TrackGain(sampleRate);
                replayGain.AnalyzeSamples(leftChannel, rightChannel);
                float multiplier = (float)Math.Pow(10, (replayGain.GetGain() + 3) / 20);
                Debug.WriteLine($"ReplayGain multiplier is {multiplier}");
                // And apply it
                for (int i = 0; i < mixedData.Length; ++i)
                {
                    mixedData[i] *= multiplier;
                }
                WaveFileWriter.CreateWaveFile(
                    masterAudioFilename, 
                    new FloatArraySampleProvider(mixedData, sampleRate).ToWaveProvider());
            }

            var backgroundImage = new BackgroundRenderer(width, height, Color.Black);
            if (background != null)
            {
                using (var bm = Image.FromFile(background))
                {
                    backgroundImage.Add(new ImageInfo(bm, ContentAlignment.MiddleCenter, true, DockStyle.None, 0.5f));
                }
            }

            if (logo != null)
            {
                using (var bm = Image.FromFile(logo))
                {
                    backgroundImage.Add(new ImageInfo(bm, ContentAlignment.BottomRight, false, DockStyle.None, 1));
                }
            }

            if (vgmFile != null)
            {
                var gd3 = Gd3Tag.LoadFromVgm(vgmFile);
                var gd3Text = gd3.ToString();
                if (gd3Text.Length > 0)
                {
                    backgroundImage.Add(new TextInfo(gd3Text, "Tahoma", 16, ContentAlignment.BottomLeft, FontStyle.Regular,
                        DockStyle.Bottom, Color.White));
                }
            }

            var renderer = new WaveformRenderer
            {
                BackgroundImage = backgroundImage.Image,
                Columns = numColumns,
                FramesPerSecond = fps,
                Width = width,
                Height = height,
                SamplingRate = sampleRate,
                RenderedLineWidthInSamples = viewSamples,
                RenderingBounds = backgroundImage.WaveArea
            };
            if (gridColor != Color.Empty && gridWidth > 0)
            {
                renderer.Grid = new WaveformRenderer.GridConfig
                {
                    Color = gridColor,
                    Width = gridWidth,
                    IncludeOuter = gridOuter
                };
            }

            if (zeroLineColor != Color.Empty && zeroLineWidth > 0)
            {
                renderer.ZeroLine = new WaveformRenderer.ZeroLineConfig
                {
                    Color = zeroLineColor,
                    Width = zeroLineWidth
                };
            }

            foreach (var channel in voiceData)
            {
                renderer.AddChannel(new Channel(channel, Color.White, lineWidth, "Hello world", Activator.CreateInstance(triggerAlgorithm) as ITriggerAlgorithm));
            }

            var outputs = new List<IGraphicsOutput>();
            if (ffMpegPath != null)
            {
                outputs.Add(new FfmpegOutput(ffMpegPath, filename, width, height, fps, ffMpegExtraArgs, masterAudioFilename));
            }

            if (previewFrameskip > 0)
            {
                outputs.Add(new PreviewOutput(previewFrameskip));
            }

            try
            {
                renderer.Render(outputs);
            }
            catch (Exception)
            {
                // Should mean it was cancelled
            }
            finally
            {
                foreach (var graphicsOutput in outputs)
                {
                    graphicsOutput.Dispose();
                }
            }
        }

        //populates cmbClr boxes with a list of every color - sorts by hue and sat/bright 
        private void Form1_Load(object sender, EventArgs e)
        {
            numVoices.Value = 1;
            numColumns.Value = 1;
            //ArrayList ColorList = new ArrayList();
            Type colorType = typeof(Color);
            PropertyInfo[] propInfoList = colorType.GetProperties(BindingFlags.Static |
                                                                  BindingFlags.DeclaredOnly | BindingFlags.Public);
            List<Color> list = new List<Color>();
            foreach (PropertyInfo c in propInfoList)
            {
                list.Add(Color.FromName(c.Name));
            }

            List<Color> sortedList = list.OrderBy(o => (o.GetHue() + (o.GetSaturation() * o.GetBrightness()))).ToList();


            foreach (Color c in sortedList)
            {
                cmbClr1.Items.Add(c.Name);
                cmbClr2.Items.Add(c.Name);
                cmbClr3.Items.Add(c.Name);
                cmbClr4.Items.Add(c.Name);
                cmbClr5.Items.Add(c.Name);
                cmbClr6.Items.Add(c.Name);
                cmbClr7.Items.Add(c.Name);
                cmbClr8.Items.Add(c.Name);
                cmbClr9.Items.Add(c.Name);
            }

            cmbClr1.Items.RemoveAt(0);
            cmbClr1.SelectedIndex = 1;
            cmbClr2.Items.RemoveAt(0);
            cmbClr2.SelectedIndex = 1;
            cmbClr3.Items.RemoveAt(0);
            cmbClr3.SelectedIndex = 1;
            cmbClr4.Items.RemoveAt(0);
            cmbClr4.SelectedIndex = 1;
            cmbClr5.Items.RemoveAt(0);
            cmbClr5.SelectedIndex = 1;
            cmbClr6.Items.RemoveAt(0);
            cmbClr6.SelectedIndex = 1;
            cmbClr7.Items.RemoveAt(0);
            cmbClr7.SelectedIndex = 1;
            cmbClr8.Items.RemoveAt(0);
            cmbClr8.SelectedIndex = 1;
            cmbClr9.Items.RemoveAt(0);
            cmbClr9.SelectedIndex = 1;

            // Parse args
            string destFile = null;
            string background = null;
            string logo = null;
            string vgmfile = null;
            string multidumper = null;
            int previewFrameskip = 0;
            double highPassFrequency = -1;
            double scale = 1.0;
            IList<string> inputWavs = null;
            Type triggerAlgorithm = typeof(PeakSpeedTrigger);
            float autoScale = -1;
            string masterAudioFile = null;
            Color gridColor = Color.Empty;
            float gridWidth = 3;
            bool gridOuter = false;
            Color zeroLineColor = Color.Empty;
            float zeroLineWidth = 1;
            float lineWidth = 3;
            for (int i = 0; i < _args.Length - 1; i += 2)
            {
                var arg = _args[i].ToLowerInvariant();
                var value = _args[i + 1];
                switch (arg)
                {
                    case "--ffmpeg":
                        _ffPath = value;
                        break;
                    case "--ffmpegoptions":
                        ffOutArgs.Text = value;
                        break;
                    case "--numvoices":
                        numVoices.Value = int.Parse(value);
                        break;
                    case "--columns":
                        numColumns.Value = int.Parse(value);
                        break;
                    case "--samples":
                        samplesTextBox.Text = value;
                        break;
                    case "--fps":
                        numFps.Value = int.Parse(value);
                        break;
                    case "--width":
                        widthTextBox.Text = value;
                        break;
                    case "--height":
                        heightTextBox.Text = value;
                        break;
                    case "--file":
                        // We support wildcards...
                        inputWavs = Directory.EnumerateFiles(Directory.GetCurrentDirectory(), value).OrderBy(x => x).ToList();
                        break;
                    case "--output":
                        destFile = value;
                        break;
                    case "--background":
                        background = value;
                        break;
                    case "--logo":
                        logo = value;
                        break;
                    case "--vgm":
                        vgmfile = Path.GetFullPath(value);
                        break;
                    case "--multidumper":
                        multidumper = value;
                        break;
                    case "--previewframeskip":
                        previewFrameskip = int.Parse(value);
                        break;
                    case "--highpassfilter":
                        highPassFrequency = Convert.ToDouble(value);
                        break;
                    case "--scale":
                        scale = Convert.ToDouble(value);
                        break;
                    case "--triggeralgorithm":
                        triggerAlgorithm = AppDomain.CurrentDomain
                            .GetAssemblies()
                            .SelectMany(a => a.GetTypes())
                            .FirstOrDefault(t => 
                                typeof(ITriggerAlgorithm).IsAssignableFrom(t) &&
                                t.Name.ToLowerInvariant().Equals(value.ToLowerInvariant()));
                        break;
                    case "--autoscale":
                        autoScale = float.Parse(value) / 100;
                        break;
                    case "--masteraudio":
                        masterAudioFile = value;
                        break;
                    case "--gridcolor":
                        gridColor = ParseColor(value);
                        break;
                    case "--gridwidth":
                        gridWidth = float.Parse(value);
                        break;
                    case "--gridouter":
                        gridOuter = value == "1" || value.ToLowerInvariant().StartsWith("t");
                        break;
                    case "--zerolinecolor":
                        zeroLineColor = ParseColor(value);
                        break;
                    case "--zerolinewidth":
                        zeroLineWidth = float.Parse(value);
                        break;
                    case "--linewidth":
                        lineWidth = float.Parse(value);
                        break;
                }
            }

            if (multidumper != null && vgmfile != null && inputWavs == null)
            {
                // Check if we have WAVs
                inputWavs = Directory.EnumerateFiles(
                    Path.GetDirectoryName(vgmfile), 
                    Path.GetFileNameWithoutExtension(vgmfile) + " - *.wav").ToList();
                if (!inputWavs.Any())
                {
                    // Let's run it
                    using (var p = Process.Start(new ProcessStartInfo
                    {
                        FileName = multidumper,
                        Arguments = $"\"{vgmfile}\" 0",
                        RedirectStandardOutput = true,
                        UseShellExecute = false
                    }))
                    {
                        p.BeginOutputReadLine();
                        p.WaitForExit();
                    }

                    // And try again
                    inputWavs = Directory.EnumerateFiles(
                            Path.GetDirectoryName(vgmfile),
                            Path.GetFileNameWithoutExtension(vgmfile) + " - *.wav")
                        .OrderByAlphaNumeric(s => s)
                        .ToList();
                }
            }


            if (destFile != null)
            {
                Go(
                    destFile, 
                    inputWavs, 
                    int.Parse(widthTextBox.Text), 
                    int.Parse(heightTextBox.Text),
                    (int)numFps.Value, 
                    background, 
                    logo, 
                    vgmfile, 
                    previewFrameskip, 
                    (float)highPassFrequency, 
                    (float)scale, 
                    triggerAlgorithm,
                    int.Parse(samplesTextBox.Text),
                    int.Parse(numColumns.Text),
                    _ffPath,
                    ffOutArgs.Text,
                    masterAudioFile,
                    autoScale,
                    gridColor, 
                    gridWidth, 
                    gridOuter,
                    zeroLineColor,
                    zeroLineWidth,
                    lineWidth);
                Close();
            }
            else if (inputWavs != null)
            {
                foreach (var file in inputWavs)
                {
                    // We find the first unpopulated text box...
                    groupBox3.Controls.OfType<TextBox>().OrderBy(c => c.TabIndex).First(c => c.Text.Length == 0).Text =
                        file;
                    numVoices.Value = groupBox3.Controls.OfType<TextBox>().Count(c => c.Text.Length > 0);
                }
            }
        }

        private Color ParseColor(string value)
        {
            // If it looks like hex, use that.
            // We support 3, 6 or 8 hex chars.
            var match = Regex.Match(value, "^#?(?<hex>[0-9a-fA-F]{3}([0-9a-fA-F]{3})?([0-9a-fA-F]{2})?)$");
            if (match.Success)
            {
                var hex = match.Groups["hex"].Value;
                if (hex.Length == 3)
                {
                    hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
                }

                if (hex.Length == 6)
                {
                    hex = "ff" + hex;
                }
                int alpha = Convert.ToInt32(hex.Substring(0, 2), 16);
                int red = Convert.ToInt32(hex.Substring(2, 2), 16);
                int green = Convert.ToInt32(hex.Substring(4, 2), 16);
                int blue = Convert.ToInt32(hex.Substring(6, 2), 16);
                return Color.FromArgb(alpha, red, green, blue);
            }
            // Then try named colors
            var property = typeof(Color)
                .GetProperties(BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public)
                .FirstOrDefault(p =>
                    p.PropertyType == typeof(Color) &&
                    p.Name.Equals(value, StringComparison.InvariantCultureIgnoreCase));
            if (property == null)
            {
                throw new Exception($"Could not parse colour {value}");
            }

            return (Color)property.GetValue(null);
        }

        //references sender, so only need this one. Draws the color rectangles in cmbClr boxes.
        private void cmbClr_DrawItem(object sender, DrawItemEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle rect = e.Bounds;
            if (e.Index >= 0)
            {
                string n = ((ComboBox) sender).Items[e.Index].ToString();
                Color c = Color.FromName(n);
                Brush b = new SolidBrush(c);
                g.FillRectangle(b, rect.X, rect.Y + 5, rect.Width / 2, rect.Height - 10);
                var combo = (ComboBox) sender;

                if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                {
                    e.Graphics.FillRectangle(new SolidBrush(Color.LightBlue), rect.X + 110, rect.Y, rect.Width,
                        rect.Height);
                }
                else
                {
                    e.Graphics.FillRectangle(new SolidBrush(SystemColors.Window), rect.X + 110, rect.Y, rect.Width,
                        rect.Height);
                }

                e.Graphics.DrawString(combo.Items[e.Index].ToString(),
                    e.Font,
                    new SolidBrush(Color.Black),
                    new Point(e.Bounds.X + 110, e.Bounds.Y + 5));
            }
        }

        //if any of the indexes change, highlight the line we're on as all rendering is done manually.

        private void cmbClr1_SelectedIndexChanged(object sender, EventArgs e)
        {
            label1.Text = "V1 Color: " + cmbClr1.Text;
        }

        private void cmbClr2_SelectedIndexChanged(object sender, EventArgs e)
        {
            label2.Text = "V2 Color: " + cmbClr2.Text;
        }

        private void cmbClr3_SelectedIndexChanged(object sender, EventArgs e)
        {
            label3.Text = "V3 Color: " + cmbClr3.Text;
        }

        private void cmbClr4_SelectedIndexChanged(object sender, EventArgs e)
        {
            label4.Text = "V4 Color: " + cmbClr4.Text;
        }

        private void cmbClr5_SelectedIndexChanged(object sender, EventArgs e)
        {
            label5.Text = "V5 Color: " + cmbClr5.Text;
        }

        private void cmbClr6_SelectedIndexChanged(object sender, EventArgs e)
        {
            label9.Text = "V6 Color: " + cmbClr6.Text;
        }

        private void cmbClr7_SelectedIndexChanged(object sender, EventArgs e)
        {
            label10.Text = "V7 Color: " + cmbClr7.Text;
        }

        private void cmbClr8_SelectedIndexChanged(object sender, EventArgs e)
        {
            label11.Text = "V8 Color: " + cmbClr8.Text;
        }

        private void cmbClr9_SelectedIndexChanged(object sender, EventArgs e)
        {
            label21.Text = "V9 Color: " + cmbClr9.Text;
        }

        private void Stop_Click(object sender, EventArgs e)
        {
            Start.Enabled = true;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                btnFile2.Enabled = false;
                btnFile3.Enabled = false;
                btnFile4.Enabled = false;
                btnFile5.Enabled = false;
                btnFile6.Enabled = false;
                btnFile7.Enabled = false;
                btnFile8.Enabled = false;
                btnFile9.Enabled = false;

                if (Path.GetExtension(txtFile1.Text) != ".wav")
                {
                    MessageBox.Show("There is no file selected for Voice 1 yet.");
                    checkBox1.Checked = false;
                    return;
                }

                string baseName = Path.GetFullPath(txtFile1.Text);
                baseName = baseName.Remove(baseName.Length - 5, 5);
                txtFile2.Text = baseName + "2.wav";
                txtFile3.Text = baseName + "3.wav";
                txtFile4.Text = baseName + "4.wav";
                txtFile5.Text = baseName + "5.wav";
                txtFile6.Text = baseName + "6.wav";
                txtFile7.Text = baseName + "7.wav";
                txtFile8.Text = baseName + "8.wav";
                txtFile9.Text = baseName + "9.wav";
            }
            else
            {
                btnFile2.Enabled = true;
                btnFile3.Enabled = true;
                btnFile4.Enabled = true;
                btnFile5.Enabled = true;
                btnFile6.Enabled = true;
                btnFile7.Enabled = true;
                btnFile8.Enabled = true;
                btnFile9.Enabled = true;
                txtFile2.Text = "";
                txtFile3.Text = "";
                txtFile4.Text = "";
                txtFile5.Text = "";
                txtFile6.Text = "";
                txtFile7.Text = "";
                txtFile8.Text = "";
                txtFile9.Text = "";
            }
        }

        private void TryLoadWaveFile(TextBox a)
        {
            using (var ofd = new OpenFileDialog {Filter = "Wave files (*.wav)|*.wav"})
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    a.Text = ofd.FileName;
                }
            }
        }

        private void btnFile1_Click(object sender, EventArgs e)
        {
            TryLoadWaveFile(txtFile1);
        }

        private void btnFile2_Click(object sender, EventArgs e)
        {
            TryLoadWaveFile(txtFile2);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "SidWiz 1.0 by Rolf R Bakke\r\nSidWiz 2 by RushJet1\r\nSidWiz 2.1 by Pigu\r\nAVIFile Wrapper by Corinna John\r\nWAVFile class by CalicoSkies");
        }

        private void btnFile3_Click_1(object sender, EventArgs e)
        {
            TryLoadWaveFile(txtFile3);
        }

        private void btnFile4_Click(object sender, EventArgs e)
        {
            TryLoadWaveFile(txtFile4);
        }

        private void btnFile5_Click(object sender, EventArgs e)
        {
            TryLoadWaveFile(txtFile5);
        }

        private void btnFile6_Click(object sender, EventArgs e)
        {
            TryLoadWaveFile(txtFile6);
        }

        private void btnFile7_Click(object sender, EventArgs e)
        {
            TryLoadWaveFile(txtFile7);
        }

        private void btnFile8_Click(object sender, EventArgs e)
        {
            TryLoadWaveFile(txtFile8);
        }

        private void btnFile9_Click(object sender, EventArgs e)
        {
            TryLoadWaveFile(txtFile9);
        }

        private void samplesTextBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                int s = Math.Max(Math.Min(int.Parse(samplesTextBox.Text), 65536), 2);
                samplesTextBox.Text = s.ToString();
            }
            catch
            {
                samplesTextBox.Text = "1024";
            }
        }

        private void enableFFBox_CheckedChanged(object sender, EventArgs e)
        {
            if (enableFFBox.Checked)
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Title = "Select FFmpeg executable";
                ofd.Filter = "Application (*.exe)|*.exe";
                ofd.ShowDialog();
                if (Path.GetExtension(ofd.FileName) == ".exe")
                {
                    _ffPath = Path.GetFullPath(ofd.FileName);
                    ffOutArgsLabel.Enabled = true;
                    ffOutArgs.Enabled = true;
                }
                else
                {
                    _ffPath = "";
                    enableFFBox.Checked = false;
                    ffOutArgsLabel.Enabled = false;
                    ffOutArgs.Enabled = false;
                }
            }
        }
    }

    internal class FloatArraySampleProvider : ISampleProvider
    {
        private readonly float[] _data;
        private int _index;

        public FloatArraySampleProvider(float[] data, int samplingRate)
        {
            _data = data;
            _index = 0;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(samplingRate, 2);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            offset += _index;
            count = Math.Min(_data.Length - offset, count);
            if (count > 0)
            {
                // Array.Copy(_data, offset, buffer, 0, count);
                // Can't use Array.Copy here because NAudio is cheating under the covers by having arrays of different types "pointing" at the same memory
                for (int i = 0; i < count; ++i)
                {
                    buffer[i] = _data[offset + i];
                }
            }
            else
            {
                count = 0;
            }

            _index += count;
            return count;
        }

        public WaveFormat WaveFormat { get; }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Drawing.Text;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SidWiz {
    public partial class Form1 : Form
    {
        private Form2 _frm;

        private readonly string[] _args;
        private string _ffPath = "";

        public Form1(string[] args)
        {
            _args = args;
            _frm = null;
            InitializeComponent();
        }

        private static int GetSample(IList<IList<int>> channels, int channelIndex, int sampleIndex)
        {
            // We may look "outside" the sample - so we return zeroes there
            return sampleIndex < 0 || sampleIndex >= channels[channelIndex].Count ? 0 : channels[channelIndex][sampleIndex];
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

            go(sfd.FileName, getInputFilenames(), Convert.ToInt32(widthTextBox.Text), Convert.ToInt32(heightTextBox.Text), Convert.ToInt32(numFps.Value), null, null, null);
        }

        private IList<string> getInputFilenames()
        {
            return groupBox3.Controls
                .OfType<TextBox>()
                .OrderBy(c => c.TabIndex)
                .Select(c => c.Text)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        private void go(string filename, IList<string> filenames, int width, int height, int fps, string background,
            string logo, string vgmFile)
        {
            filename = Path.GetFullPath(filename);
            Start.Enabled = false;
            var colors = new[]
            {
                Color.FromName(cmbClr1.Text),
                Color.FromName(cmbClr2.Text),
                Color.FromName(cmbClr3.Text),
                Color.FromName(cmbClr4.Text),
                Color.FromName(cmbClr5.Text),
                Color.FromName(cmbClr6.Text),
                Color.FromName(cmbClr7.Text),
                Color.FromName(cmbClr8.Text),
                Color.FromName(cmbClr9.Text),
            }.Select(c => c.ToArgb()).ToList();

            int viewSamp = int.Parse(samplesTextBox.Text);
            Enabled = false;
            WaitForm waitForm = new WaitForm();
            waitForm.Show();

            WAVFile wavFileTest = new WAVFile();
            wavFileTest.Open(filenames.First(), WAVFile.WAVFileMode.READ);
            int sampleLength = wavFileTest.NumSamples / wavFileTest.NumChannels;
            int sampleRate = wavFileTest.SampleRateHz;
            wavFileTest.Close();

            float allMin = int.MaxValue;
            float allMax = int.MinValue;
            var minmaxlock = new object();

            int totalProgress = filenames.Count * sampleLength;
            int progress = 0;

            var loadTask = Task.Run(() =>
            {
                // Populate result with nulls so we can replace them by index
                var result = new List<IList<float>>(Enumerable.Repeat<IList<float>>(null, filenames.Count));
                Parallel.For(0, filenames.Count, ch =>
                {
                    var wavFilename = filenames[ch];
                    var wavFile = new WAVFile();
                    wavFile.Open(wavFilename, WAVFile.WAVFileMode.READ);
                    try
                    {
                        var samples = new List<float>(sampleLength);
                        float min = float.MaxValue;
                        float max = float.MinValue;
                        for (long i = 0; i < sampleLength; i++)
                        {
                            if (i > wavFile.NumSamples / wavFile.NumChannels) break;
                            float t = 0;
                            for (int j = 0; j < wavFile.NumChannels; j++)
                            {
                                t += wavFile.GetNextSampleAs16Bit() / 32768.0f;
                            }

                            samples.Add(t / wavFile.NumChannels);

                            if (t > max)
                            {
                                max = t;
                            }

                            if (t < min)
                            {
                                min = t;
                            }

                            Interlocked.Increment(ref progress);
                        }

                        if (min == max)
                        {
                            // Silent - discard data
                            return;
                        }

                        result[ch] = samples;

                        lock (minmaxlock)
                        {
                            allMin = Math.Min(allMin, min);
                            allMax = Math.Max(allMax, max);
                        }
                    }
                    finally
                    {
                        wavFile.Close();
                    }
                });
                return result.Where(ch => ch != null).ToList();
            });

            while (!loadTask.IsCompleted)
            {
                Application.DoEvents();
                Thread.Sleep(1);
                waitForm.Progress("Reading data...", 1.0 * progress / totalProgress);
            }

            var voiceData = loadTask.Result;

            // Scale all channels so we reach the limits
            var scale = 1.0f / (allMax - allMin);

            foreach (var channel in voiceData)
            {
                // Scale it
                for (int i = 0; i < channel.Count; ++i)
                {
                    channel[i] *= scale;
                }
            }

            waitForm.Close();
            Enabled = true;

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
                var info = Gd3Parser.GetTagInfo(vgmFile);
                if (info.Length > 0)
                {
                    backgroundImage.Add(new TextInfo(info, "Arial", 16, ContentAlignment.BottomLeft, FontStyle.Regular, DockStyle.Bottom, Color.White));
                }
            }

            var renderer = new WaveformRenderer
            {
                BackgroundImage = backgroundImage.Image,
                Columns = (int) numColumns.Value,
                FramesPerSecond = fps,
                Width = width,
                Height = height,
                SamplingRate = sampleRate,
                RenderedLineWidthInSamples = viewSamp,
                RenderingBounds = backgroundImage.WaveArea
            };

            foreach (var channel in voiceData)
            {
                renderer.AddChannel(new Channel(channel, Color.White, 3, ""));
            }

            using (var output = new FfmpegOutput(_ffPath, filename, width, height, fps, ffOutArgs.Text, filenames))
            using (var preview = new PreviewOutput(16))
            {
                renderer.Render(new IGraphicsOutput[]{output, preview});
            }

            Start.Enabled = true; //you can click start again. this was causing some fun problems ;)
        }

//------end of start button click


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
            IList<string> inputWavs = null;
            for (int i = 0; i < _args.Length - 1; i += 2)
            {
                var arg = _args[i];
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
                        numVoices.Value = Convert.ToInt32(value);
                        break;
                    case "--columns":
                        numColumns.Value = Convert.ToInt32(value);
                        break;
                    case "--samples":
                        samplesTextBox.Text = value;
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
                }
            }

            if (multidumper != null && vgmfile != null && inputWavs == null)
            {
                // Check if we have WAVs
                inputWavs = Directory.EnumerateFiles(Path.GetDirectoryName(vgmfile), Path.GetFileNameWithoutExtension(vgmfile) + " - *.wav").ToList();
                if (!inputWavs.Any())
                {
                    // Let's run it
                    using (var p = Process.Start(new ProcessStartInfo{
                        FileName = multidumper,
                        Arguments = $"\"{vgmfile}\" 0",
                        RedirectStandardOutput = true,
                        UseShellExecute = false}))
                    {
                        p.BeginOutputReadLine();
                        p.WaitForExit();
                    }
                    // And try again
                    inputWavs = Directory.EnumerateFiles(Path.GetDirectoryName(vgmfile), Path.GetFileNameWithoutExtension(vgmfile) + " - *.wav").ToList();
                }
            }


            if (destFile != null)
            {
                go(destFile, inputWavs, Convert.ToInt32(widthTextBox.Text), Convert.ToInt32(heightTextBox.Text), Convert.ToInt32(numFps.Value), background, logo, vgmfile);
                Close();
            }
            else if (inputWavs != null)
            {
                foreach (var file in inputWavs)
                {
                    // We find the first unpopulated text box...
                    groupBox3.Controls.OfType<TextBox>().OrderBy(c => c.TabIndex).First(c => c.Text.Length == 0).Text = file;
                    numVoices.Value = groupBox3.Controls.OfType<TextBox>().Count(c => c.Text.Length > 0);
                }
            }
        }

        //references sender, so only need this one. Draws the color rectangles in cmbClr boxes.
        private void cmbClr_DrawItem(object sender, DrawItemEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle rect = e.Bounds;
            if (e.Index >= 0)
            {
                string n = ((ComboBox)sender).Items[e.Index].ToString();
                Color c = Color.FromName(n);
                Brush b = new SolidBrush(c);
                g.FillRectangle(b, rect.X, rect.Y + 5, rect.Width / 2, rect.Height - 10);
                var combo = (ComboBox) sender;

                if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                {
                    e.Graphics.FillRectangle(new SolidBrush(Color.LightBlue), rect.X + 110, rect.Y, rect.Width, rect.Height);
                }
                else
                {
                    e.Graphics.FillRectangle(new SolidBrush(SystemColors.Window), rect.X + 110, rect.Y, rect.Width, rect.Height);
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
            _frm.toolStripStatusLabel2.Text = "Ready";
            _frm.Close();
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
            MessageBox.Show("SidWiz 1.0 by Rolf R Bakke\r\nSidWiz 2 by RushJet1\r\nSidWiz 2.1 by Pigu\r\nAVIFile Wrapper by Corinna John\r\nWAVFile class by CalicoSkies");
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
            if(enableFFBox.Checked)
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
}

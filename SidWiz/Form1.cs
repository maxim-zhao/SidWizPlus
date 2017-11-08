using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Drawing.Imaging;
using AviFile;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Runtime.InteropServices;

namespace SidWiz {
    public partial class Form1 : Form
    {
        Form2 frm = null;

        int voices = 0;
        int width = 1280;
        int height = 720;
        int fps = 60;
        string ffPath = "";

        public Form1()
        {
            InitializeComponent();
        }


        private void pixel(int x, int y, Bitmap map)
        {
            if (x < 0 | y < 0 | x >= width | y >= height) return;
            map.SetPixel(x, y, Color.White);
        }

        private static short getSamp(short[,] samplist, int voice, long index)
        {
            return (index < 0 || index >= samplist.GetLength(1)) ? (short) 0 : samplist[voice, index];
        }

        /*        public void drawLine(int oldY, int newY, int x, Bitmap map) {
                    if (oldY > newY) {
                        int temp = oldY;
                        oldY = newY;
                        newY = temp;
                    }
                    for (int y = oldY; y <= newY; y++) {
                        pixel(x, y, map);
                        pixel(x-1, y, map);
                        pixel(x+1, y, map);
                        pixel(x, y-1, map);
                        pixel(x, y+1, map);
                    }
                }
        */

        private void Start_Click(object sender, EventArgs e)
        {
            if (txtFile1.Text == "")
            {
                MessageBox.Show("There is no file selected for channel 1!");
                return;
            }
            SaveFileDialog sfd = new SaveFileDialog();
            if (ffPath == "") sfd.Filter = "AVI Files (.avi)|*.avi";
            sfd.ShowDialog();
            if (sfd.FileName == "")
            {
                return;
            }
            voices = (int) numVoices.Value;
            Start.Enabled = false;
            Color clr = Color.FromName(cmbClr1.Text);
            Color Color1 = Color.FromName(cmbClr1.Text);
            Color Color2 = Color.FromName(cmbClr2.Text);
            Color Color3 = Color.FromName(cmbClr3.Text);
            Color Color4 = Color.FromName(cmbClr4.Text);
            Color Color5 = Color.FromName(cmbClr5.Text);
            Color Color6 = Color.FromName(cmbClr6.Text);
            Color Color7 = Color.FromName(cmbClr7.Text);
            Color Color8 = Color.FromName(cmbClr8.Text);
            Color Color9 = Color.FromName(cmbClr9.Text);

            //string fileName = "song";

            //int frameCounter = 0;
            long frameIndex = 0;
            long frameTriggerOffset = 0;
            int oldY = 0;
            int newY = 0;
            int viewSamp = int.Parse(samplesTextBox.Text);
            Enabled = false;
            WaitForm waitForm = new WaitForm();
            waitForm.Show();

            //byte[] voice1 = System.IO.File.ReadAllBytes("c:/sidwiz/" + fileName + "_v1.raw");
            //byte[] voice2 = System.IO.File.ReadAllBytes("c:/sidwiz/" + fileName + "_v2.raw");
            //byte[] voice3 = System.IO.File.ReadAllBytes("c:/sidwiz/" + fileName + "_v3.raw");
            //byte[] voice4 = System.IO.File.ReadAllBytes("c:/sidwiz/" + fileName + "_v4.raw");
            //byte[] voice5 = System.IO.File.ReadAllBytes("c:/sidwiz/" + fileName + "_v5.raw");


            WAVFile temp = new WAVFile();
            temp.Open(txtFile1.Text, WAVFile.WAVFileMode.READ);
            //byte[] readvoice = new byte[temp.NumSamples];
            long sampleLength = temp.NumSamples / temp.NumChannels;
            int sampleRate = temp.SampleRateHz;
            short[,] voiceData = new short[voices, sampleLength];
            temp.Close();

            int rc = 0;
            foreach (Control fBox in groupBox3.Controls)
            {
                if (fBox.GetType() == typeof(TextBox))
                {
                    if (fBox.TabIndex <= voices + 100)
                    {
                        int ch = fBox.TabIndex - 101;
                        temp.Open(fBox.Text, WAVFile.WAVFileMode.READ);
                        for (long i = 0; i < sampleLength; i++)
                        {
                            if (i > temp.NumSamples / temp.NumChannels) break;
                            int t = 0;
                            for (int j = 0; j < temp.NumChannels; j++)
                            {
                                t += temp.GetNextSampleAs16Bit();
                            }
                            voiceData[ch, i] = (short)(t / temp.NumChannels);
                            if (i % 65536 == 0)
                            {
                                waitForm.Progress(String.Format("Reading channel {0}", ch), ((i / (double) sampleLength) + rc) / (double) voices);
                                Application.DoEvents();
                            }
                        }
                        temp.Close();
                        rc++;
                    }

                }

            }

            waitForm.Close();
            Enabled = true;
            if (frm != null) frm.Close();
            frm = new Form2();
            frm.Show();
            frm.Size = new Size(width + 16, height + 40);
            frm.SetDesktopLocation(0, 0);

            /*for (int q = 0; q < voices; q++)
            {
                if (q != 0) readvoice = System.IO.File.ReadAllBytes("c:/sidwiz/" + fileName + "_v" + (q + 1) + ".raw");
                for (long i = 0; i < sampleLenght; i++)
                {
                    voiceData[q, i] = readvoice[i];
                }
                
            }*/
            //Bitmap[] storbuf = new Bitmap[(sampleLenght/3200)-4000];
            //int storcounter = 0;


            //load the first image
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            //create a new AVI file

            AviManager aviManager = null;
            VideoStream aviStream = null;
            Process ffProc = null;
            BinaryWriter ffWriter = null;
            if (ffPath == "")
            {
                aviManager = new AviManager(Path.GetFullPath(sfd.FileName), false);
                //add a new video stream and one frame to the new file
                aviStream = aviManager.AddVideoStream(true, fps, width * height * 3, width, height, PixelFormat.Format24bppRgb);
            }
            else
            {
                ffProc = new Process();
                ffProc.StartInfo.FileName = ffPath;
                ffProc.StartInfo.Arguments = String.Format("-y -f rawvideo -pixel_format bgr24 -video_size {0}x{1} -framerate {2} -i pipe: {3} \"{4}\"",
                    width, height, fps, ffOutArgs.Text, Path.GetFullPath(sfd.FileName));
                ffProc.StartInfo.UseShellExecute = false;
                ffProc.StartInfo.RedirectStandardInput = true;
                ffProc.Start();
                ffWriter = new BinaryWriter(ffProc.StandardInput.BaseStream);
            }

            while (frameIndex < sampleLength * fps / sampleRate)
            {
                long frameIndexSamples = frameIndex * sampleRate / fps;
                Bitmap framebuffer = new Bitmap(width, height, PixelFormat.Format24bppRgb);

                //Locking the bits and doing direct operations on the bitmap data is MUCH faster than using the "pixel()" command.
                //Doing this brought render-time down from 55ms to 30ms originally with a separate function, but the bits had to be locked and unlocked every time.
                //Dragging this function into the main one means that we can do all passes without having to lock/unlock the bits hundreds of times.
                //This made render time go from 30ms to about 5-7ms.  UI+AVI time takes 25ms so this is about double speed, effectively.

                BitmapData bitmapData = framebuffer.LockBits(new Rectangle(0, 0, framebuffer.Width, framebuffer.Height), ImageLockMode.ReadWrite, framebuffer.PixelFormat);


                int viewSamples = int.Parse(samplesTextBox.Text);
                int cols = (int)numColumns.Value;
                int viewWidth = framebuffer.Width / cols;
                double viewHeight = framebuffer.Height / Math.Ceiling((double)voices / (double)cols);
                int oldY2 = 0;
                int newY2 = 0;
                int z = 0;

                for (int i = 0; i < voices; i++)                          //Program runs this for each voice.  
                {
                    frameTriggerOffset = 0;                                 //syncronation
                    while (getSamp(voiceData, i, frameIndexSamples + frameTriggerOffset) > 0 && frameTriggerOffset < 3000) frameTriggerOffset++;
                    while (getSamp(voiceData, i, frameIndexSamples + frameTriggerOffset) < 0 && frameTriggerOffset < 3000) frameTriggerOffset++;
                    if (i == 0) clr = Color1;
                    if (i == 1) clr = Color2;
                    if (i == 2) clr = Color3;
                    if (i == 3) clr = Color4;
                    if (i == 4) clr = Color5;
                    if (i == 5) clr = Color6;
                    if (i == 6) clr = Color7;
                    if (i == 7) clr = Color8;
                    if (i == 8) clr = Color9;
                    
                    for (int x = 0; x < viewWidth; x++)
                    {   //draw waveform
                        //----------------------calculate positions
                        newY = Math.Min(Math.Max((int)((i / cols + 0.5) * viewHeight - (getSamp(voiceData, i,
                            frameIndexSamples + frameTriggerOffset + x * viewSamp / viewWidth - viewSamp / 2)) * viewHeight / 65536.0), 0), framebuffer.Height - 1);
                        if (x == 0) oldY = newY;
                        //----------------------setup for drawline
                        z = x + (i % cols) * framebuffer.Width / cols;
                        z = z == 0 ? 3 : z * 3;
                        if (oldY > newY)
                        {
                            oldY2 = newY;
                            newY2 = oldY;
                        }
                        else
                        {
                            oldY2 = oldY;
                            newY2 = newY;
                        }

                        //-------------drawline-------------------  sucks but is immensely faster, 30 ms -> 5 ms

                        unsafe
                        {

                            int bytesPerPixel = 3;
                            int heightInPixels = bitmapData.Height;
                            int widthInBytes = bitmapData.Width * bytesPerPixel;
                            byte* ptrFirstPixel = (byte*)bitmapData.Scan0;

                            //get pixel byte values for color.  Format is apparently BGR
                            byte B = clr.R;
                            byte G = clr.G;
                            byte R = clr.B;

                            for (int y = oldY2; y <= newY2; y++)
                            {

                                byte* currentLine = ptrFirstPixel + (y * bitmapData.Stride);  //get current line of pixels

                                //paints a "+" of pixels (5 total)
                                currentLine[z] = R;
                                currentLine[z + 1] = G;
                                currentLine[z + 2] = B;
                                if (z < bitmapData.Stride - 3)
                                {
                                    currentLine[z + 3] = R;
                                    currentLine[z + 4] = G;
                                    currentLine[z + 5] = B;
                                }
                                if (z >= 3)
                                {
                                    currentLine[z - 3] = R;
                                    currentLine[z - 2] = G;
                                    currentLine[z - 1] = B;
                                }
                                if (y < bitmapData.Height - 1)
                                {
                                    currentLine = ptrFirstPixel + ((y + 1) * bitmapData.Stride); //move down a line
                                    currentLine[z] = R;
                                    currentLine[z + 1] = G;
                                    currentLine[z + 2] = B;

                                }
                                if (y >= 1)
                                {
                                    currentLine = ptrFirstPixel + ((y - 1) * bitmapData.Stride); //move up a line
                                    currentLine[z] = R;
                                    currentLine[z + 1] = G;
                                    currentLine[z + 2] = B;
                                }
                            }
                        }

                        oldY = newY;
                    }

                }

                if (ffPath != "")
                {
                    IntPtr ptr = bitmapData.Scan0;
                    int s = Math.Abs(bitmapData.Stride) * bitmapData.Height;
                    byte[] data = new byte[s];
                    Marshal.Copy(ptr, data, 0, s);
                    ffWriter.Write(data);
                }

                framebuffer.UnlockBits(bitmapData);
                //------------------end of drawline

                //note that this code easily takes the longest time to execute at 25ms or so.  Look for ways to improve this.  Threading AVI output doesn't seem to work.

                if (Application.OpenForms.Count == 0) break;  //if the program is closed, jump out of loop and exit gracefully.

                Application.DoEvents(); //so the UI doesn't freeze

                //following code adds a frame of video data to the avistream

                frameIndex++;
                frm.pictureBox1.Image = framebuffer;
                frm.pictureBox1.Refresh();

                //get rid of red X
                typeof(Control).InvokeMember("SetState", BindingFlags.NonPublic |
              BindingFlags.InvokeMethod | BindingFlags.Instance, null,
              frm.pictureBox1, new object[] { 0x400000, false });

                if (ffPath == "")
                {
                    aviStream.AddFrame(framebuffer);    //add frame to AVI
                }
                framebuffer.Dispose();
                frm.toolStripStatusLabel2.Text = ((float) frameIndexSamples / (float) sampleLength * 100).ToString() + "%";  //percent counter at bottom

                if (Start.Enabled == true) break;
            }

            if (ffPath == "")
            {
                aviManager.Close();     //file is done being written, close the stream
            }
            else
            {
                ffWriter.Close();
                ffProc.Close();
            }
            Start.Enabled = true;   //you can click start again. this was causing some fun problems ;)
            frm.toolStripStatusLabel2.Text = "Ready";
        } //------end of start button click


        //populates cmbClr boxes with a list of every color - sorts by hue and sat/bright 
        private void Form1_Load(object sender, EventArgs e)
        {
            numVoices.Value = 1;
            numColumns.Value = 1;
            //ArrayList ColorList = new ArrayList();
            Type colorType = typeof(System.Drawing.Color);
            PropertyInfo[] propInfoList = colorType.GetProperties(BindingFlags.Static |
                                          BindingFlags.DeclaredOnly | BindingFlags.Public);
            List<Color> list = new List<Color>();
            foreach (PropertyInfo c in propInfoList)
            {
                list.Add(Color.FromName(c.Name));
            }
            List<Color> SortedList = list.OrderBy(o => (o.GetHue() + (o.GetSaturation() * o.GetBrightness()))).ToList();


            foreach (Color c in SortedList)
            {
                this.cmbClr1.Items.Add(c.Name);
                this.cmbClr2.Items.Add(c.Name);
                this.cmbClr3.Items.Add(c.Name);
                this.cmbClr4.Items.Add(c.Name);
                this.cmbClr5.Items.Add(c.Name);
                this.cmbClr6.Items.Add(c.Name);
                this.cmbClr7.Items.Add(c.Name);
                this.cmbClr8.Items.Add(c.Name);
                this.cmbClr9.Items.Add(c.Name);
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

        }

        //references sender, so only need this one. Draws the color rectangles in cmbClr boxes.
        private void cmbClr_DrawItem(object sender, DrawItemEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle rect = e.Bounds;
            if (e.Index >= 0)
            {
                string n = ((ComboBox)sender).Items[e.Index].ToString();
                Font f = new Font("Arial", 9, FontStyle.Regular);
                Color c = Color.FromName(n);
                Brush b = new SolidBrush(c);
                g.FillRectangle(b, rect.X, rect.Y + 5,
                                rect.Width / 2, rect.Height - 10);
                //g.DrawString(n, f, Brushes.Black, rect.X+110, rect.Top);
                var combo = sender as ComboBox;

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
            frm.toolStripStatusLabel2.Text = "Ready";
            frm.Close();
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
                String baseName = "";
                baseName = Path.GetFullPath(txtFile1.Text);
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

        private void tryLoadWaveFile(TextBox a)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Wave files (*.wav)|*.wav";
            ofd.ShowDialog();
            if (Path.GetExtension(ofd.FileName) == ".wav")
            {
                a.Text = ofd.FileName;
            }
            else
            {
                MessageBox.Show("This is not a .wav file, or no file was selected.");
            }
        }

        private void btnFile1_Click(object sender, EventArgs e)
        {
            tryLoadWaveFile(txtFile1);
        }

        private void btnFile2_Click(object sender, EventArgs e)
        {
            tryLoadWaveFile(txtFile2);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("SidWiz 1.0 by Rolf R Bakke\r\nSidWiz 2 by RushJet1\r\nSidWiz 2.1 by Pigu\r\nAVIFile Wrapper by Corinna John\r\nWAVFile class by CalicoSkies");
        }

        private void btnFile3_Click_1(object sender, EventArgs e)
        {
            tryLoadWaveFile(txtFile3);
        }

        private void btnFile4_Click(object sender, EventArgs e)
        {
            tryLoadWaveFile(txtFile4);
        }

        private void btnFile5_Click(object sender, EventArgs e)
        {
            tryLoadWaveFile(txtFile5);
        }

        private void btnFile6_Click(object sender, EventArgs e)
        {
            tryLoadWaveFile(txtFile6);
        }

        private void btnFile7_Click(object sender, EventArgs e)
        {
            tryLoadWaveFile(txtFile7);
        }

        private void btnFile8_Click(object sender, EventArgs e)
        {
            tryLoadWaveFile(txtFile8);
        }

        private void btnFile9_Click(object sender, EventArgs e)
        {
            tryLoadWaveFile(txtFile9);
        }

        private void widthTextBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                width = int.Parse(widthTextBox.Text);
            } catch
            {
                width = 1280;
                widthTextBox.Text = "1280";
            }
        }

        private void heightTextBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                height = int.Parse(heightTextBox.Text);
            }
            catch
            {
                height = 720;
                heightTextBox.Text = "720";
            }
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

        private void numFps_ValueChanged(object sender, EventArgs e)
        {
            fps = (int) numFps.Value;
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
                    ffPath = Path.GetFullPath(ofd.FileName);
                    ffOutArgsLabel.Enabled = true;
                    ffOutArgs.Enabled = true;
                }
                else
                {
                    ffPath = "";
                    enableFFBox.Checked = false;
                    ffOutArgsLabel.Enabled = false;
                    ffOutArgs.Enabled = false;
                }
            }
        }
    }
}

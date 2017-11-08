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

namespace SidWiz {
    public partial class Form1 : Form
    {
        Form2 frm = new Form2();

        int voices = 0;

        public Form1()
        {
            InitializeComponent();
        }


        public void pixel(int x, int y, Bitmap map)
        {
            if (x < 0 | y < 0 | x > 1279 | y > 719) return;
            map.SetPixel(x, y, Color.White);
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
            sfd.Filter = "AVI Files (.avi)|*.avi";
            sfd.ShowDialog();
            if (sfd.FileName == "")
            {
                return;
            }
            frm.Show();
            SetDesktopLocation(0, Location.Y);
            frm.SetDesktopLocation(Location.X + 300, Location.Y);
            voices = cmbVoices.SelectedIndex + 1;
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

            //string fileName = "song";

            //int frameCounter = 0;
            long frameIndex = 640;
            long frameTriggerOffset = 0;
            int oldY = 0;
            int newY = 0;

            //byte[] voice1 = System.IO.File.ReadAllBytes("c:/sidwiz/" + fileName + "_v1.raw");
            //byte[] voice2 = System.IO.File.ReadAllBytes("c:/sidwiz/" + fileName + "_v2.raw");
            //byte[] voice3 = System.IO.File.ReadAllBytes("c:/sidwiz/" + fileName + "_v3.raw");
            //byte[] voice4 = System.IO.File.ReadAllBytes("c:/sidwiz/" + fileName + "_v4.raw");
            //byte[] voice5 = System.IO.File.ReadAllBytes("c:/sidwiz/" + fileName + "_v5.raw");


            WAVFile temp = new WAVFile();
            temp.Open(txtFile1.Text, WAVFile.WAVFileMode.READ);
            //byte[] readvoice = new byte[temp.NumSamples];
            byte[,] voiceData = new byte[voices, temp.NumSamples];

            for (int i = 0; i < temp.NumSamples; i++){
                    if (temp.BitsPerSample == 8)
                    {
                        voiceData[0,i] = temp.GetNextSample_8bit();
                    }
                    else
                    {
                        voiceData[0,i] = temp.GetNextSampleAs8Bit();
                    }
            }
            
            long sampleLenght = temp.NumSamples;
            temp.Close();

            
            foreach (Control fBox in groupBox3.Controls)
            {
                if (fBox.GetType() == typeof(TextBox))
                {
                    if (fBox.TabIndex <= voices + 100)
                    {
                        temp.Open(fBox.Text, WAVFile.WAVFileMode.READ);
                        for (long i = 0; i < sampleLenght; i++)
                        {
                            if (i > temp.NumSamples) break;
                            if (temp.BitsPerSample == 8)
                            {
                                voiceData[fBox.TabIndex-101, i] = temp.GetNextSample_8bit();
                            }
                            else
                            {
                                voiceData[fBox.TabIndex-101, i] = temp.GetNextSampleAs8Bit();
                            }
                        }
                        temp.Close();
                    }
                    
                }
                
            }

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
            Bitmap bitmap = new Bitmap(1280, 720, PixelFormat.Format24bppRgb);
            //create a new AVI file


            AviManager aviManager = new AviManager("C:\\sidwiz\\new.avi", false);
            //add a new video stream and one frame to the new file
            VideoStream aviStream = aviManager.AddVideoStream(true, 60, 2764800, 1280, 720, PixelFormat.Format24bppRgb);

            while (frameIndex < (sampleLenght - 4000)) 
            {

                Bitmap framebuffer = new Bitmap(1280, 720, PixelFormat.Format24bppRgb);

                //Locking the bits and doing direct operations on the bitmap data is MUCH faster than using the "pixel()" command.
                //Doing this brought render-time down from 55ms to 30ms originally with a separate function, but the bits had to be locked and unlocked every time.
                //Dragging this function into the main one means that we can do all passes without having to lock/unlock the bits hundreds of times.
                //This made render time go from 30ms to about 5-7ms.  UI+AVI time takes 25ms so this is about double speed, effectively.

                BitmapData bitmapData = framebuffer.LockBits(new Rectangle(0, 0, framebuffer.Width, framebuffer.Height), ImageLockMode.ReadWrite, framebuffer.PixelFormat);



                int oldY2 = 0;
                int newY2 = 0;
                int z = 0;

                for (int i = 0; i < voices; i++)                          //Program runs this for each voice.  
                {


                    frameTriggerOffset = 0;                                 //syncronation
                    while (voiceData[i, frameIndex + frameTriggerOffset] < 128 && frameTriggerOffset < 3000) frameTriggerOffset++;
                    while (voiceData[i, frameIndex + frameTriggerOffset] >= 126 && frameTriggerOffset < 3000) frameTriggerOffset++;
                    if (i == 0) clr = Color1;
                    if (i == 1) clr = Color2;
                    if (i == 2) clr = Color3;
                    if (i == 3) clr = Color4;
                    if (i == 4) clr = Color5;
                    if (i == 5) clr = Color6;
                    if (i == 6) clr = Color7;
                    if (i == 7) clr = Color8;

                    int scale = (int)numericUpDown2.Value;
                    int scalar = 2;
                    int divisor = 1;
                    int offset = 0;
                    int halfpoint = (int)(((float)voices / 2) + .5);
                    float multi = (float)halfpoint / (float)(voices - halfpoint);
                    if (comboColumns.SelectedIndex == 1) divisor = 2;
                    if (divisor == 2) scalar = 1;
                    for (int x = 0; x / (scale / scalar) < framebuffer.Width / divisor; x++)
                    {   //draw waveform         

//----------------------calculate positions
                        if (divisor == 2)  //if 2 columns
                        {
                            try
                            {
                                if (i < halfpoint) newY = i * (240) + voiceData[i, Math.Abs(frameIndex + frameTriggerOffset + x - 1600)] - offset ;  //left side
                                if (i >= halfpoint) newY = (i - halfpoint) * 240 + voiceData[i, Math.Abs(frameIndex + frameTriggerOffset + x - 1600)] - offset; //right side
                            }
                            catch { break; }

                            if (x == 0) oldY = newY;
                            
                            if (i < halfpoint)
                            {      //left side (first half rounded up channels)
                                z = x / scale;
                                oldY2 = (int)((float)oldY * (float)(3.0 / (float)halfpoint));  //255 is max for 8-bit, this gives us scaling per voice.
                                newY2 = (int)((float)newY * (float)(3.0 / (float)halfpoint));
                                if (oldY2 > 718) oldY2 = 718;
                                if (newY2 > 718) newY2 = 718;
                                if (newY2 < 1) newY2 = 1;
                                if (oldY2 < 1) oldY2 = 1;
                            }
                            else
                            {                 //right side (second half rounded down)
                                z = (x / scale) + 640;
                                oldY2 = (int)((float)oldY * (float)(3.0 / (float)(voices-halfpoint)));  //255 is max for 8-bit, this gives us scaling per voice.
                                newY2 = (int)((float)newY * (float)(3.0 / (float)(voices-halfpoint)));
                                if (oldY2 > 718) oldY2 = 718;
                                if (newY2 > 718) newY2 = 718;
                                if (newY2 < 1) newY2 = 1;
                                if (oldY2 < 1) oldY2 = 1;
                            }

                        }
                        else   //if 1 column
                        {
                            try
                            {
                                newY = i * (240) + voiceData[i, Math.Abs(frameIndex + frameTriggerOffset + x - 1600)] - offset;
                                
                            }
                            catch { break; }

                            if (x == 0) oldY = newY;

                            z = x / (int)((float)scale/2); //divide by 2 because twice as wide
                            oldY2 = (int)((float)oldY * (float)(3.0 / (float)voices));  //255 is max for 8-bit, this gives us scaling per voice.
                            newY2 = (int)((float)newY * (float)(3.0 / (float)voices));
                            if (oldY2 > 718) oldY2 = 718;
                            if (newY2 > 718) newY2 = 718;
                            if (newY2 < 1) newY2 = 1;
                            if (oldY2 < 1) oldY2 = 1;
                        }
//----------------------setup for drawline
                        if (z == 0) z += 1;
                        z *= 3;           //3 bytes per pixel
                        if (oldY2 > newY2)
                        {
                            int tmp = oldY2;
                            oldY2 = newY2;
                            newY2 = tmp;
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
                                currentLine[z + 3] = R;
                                currentLine[z + 4] = G;
                                currentLine[z + 5] = B;
                                currentLine[z - 3] = R;
                                currentLine[z - 2] = G;
                                currentLine[z - 1] = B;
                                currentLine = ptrFirstPixel + ((y + 1) * bitmapData.Stride); //move down a line
                                currentLine[z] = R;
                                currentLine[z + 1] = G;
                                currentLine[z + 2] = B;
                                currentLine = ptrFirstPixel + ((y - 1) * bitmapData.Stride); //move up a line
                                currentLine[z] = R;
                                currentLine[z + 1] = G;
                                currentLine[z + 2] = B;
                                }
                        }

                        oldY = newY;
                    }

                }

                framebuffer.UnlockBits(bitmapData);
//------------------end of drawline

                //note that this code easily takes the longest time to execute at 25ms or so.  Look for ways to improve this.  Threading AVI output doesn't seem to work.

                if (Application.OpenForms.Count == 0) break;  //if the program is closed, jump out of loop and exit gracefully.

                Application.DoEvents(); //so the UI doesn't freeze

                //following code adds a frame of video data to the avistream

                frameIndex += 735;                                     //3200 is 1 frame at 30fps and 96kHz samplerate / stereo.  735 is 44100hz/60fps/mono.  hz / framerate
                frm.pictureBox1.Image = framebuffer;
                frm.pictureBox1.Refresh();
                
                //get rid of red X
                typeof(Control).InvokeMember("SetState", BindingFlags.NonPublic |
              BindingFlags.InvokeMethod | BindingFlags.Instance, null,
              frm.pictureBox1, new object[] { 0x400000, false });

                aviStream.AddFrame(framebuffer);    //add frame to AVI
                framebuffer.Dispose();
                frm.toolStripStatusLabel2.Text = ((float)frameIndex / (float)(sampleLenght - 4000) * 100).ToString() + "%";  //percent counter at bottom

                if (Start.Enabled == true) break;
            }

            aviManager.Close();     //file is done being written, close the stream
            Start.Enabled = true;   //you can click start again. this was causing some fun problems ;)
            frm.toolStripStatusLabel2.Text = "Ready";
        } //------end of start button click


        //populates cmbClr boxes with a list of every color - sorts by hue and sat/bright 
        private void Form1_Load(object sender, EventArgs e)
        {
            cmbVoices.SelectedIndex = 0;
            comboColumns.SelectedIndex = 0;
            //ArrayList ColorList = new ArrayList();
            Type colorType = typeof(System.Drawing.Color);
            PropertyInfo[] propInfoList = colorType.GetProperties(BindingFlags.Static |
                                          BindingFlags.DeclaredOnly | BindingFlags.Public);
            List<Color> list = new List<Color>();
            foreach(PropertyInfo c in propInfoList)
            {
                list.Add(Color.FromName(c.Name));
            }
            List<Color> SortedList = list.OrderBy(o => (o.GetHue() + (o.GetSaturation() * o.GetBrightness() ))).ToList();


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
                                rect.Width/2, rect.Height - 10);
                //g.DrawString(n, f, Brushes.Black, rect.X+110, rect.Top);
                var combo = sender as ComboBox;

                if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                {
                    e.Graphics.FillRectangle(new SolidBrush(Color.LightBlue), rect.X+110,rect.Y,rect.Width,rect.Height);
                }
                else
                {
                    e.Graphics.FillRectangle(new SolidBrush(SystemColors.Window), rect.X + 110, rect.Y, rect.Width, rect.Height);
                }

                e.Graphics.DrawString(combo.Items[e.Index].ToString(),
                                              e.Font,
                                              new SolidBrush(Color.Black),
                                              new Point(e.Bounds.X+110, e.Bounds.Y+5));
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

        private void Stop_Click(object sender, EventArgs e)
        {
            Start.Enabled = true;
            frm.toolStripStatusLabel2.Text = "Ready";
            frm.Hide();
        }

        private void comboColumns_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void cmbVoices_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (Control cb in groupBox2.Controls) {
                if (cb.GetType() == typeof(ComboBox))
                {
                    if (cb.TabIndex > cmbVoices.SelectedIndex + 31) cb.Enabled = false;
                    else cb.Enabled = true;
                }
                if (cb.GetType() == typeof(Label))
                {
                    if (cb.TabIndex > cmbVoices.SelectedIndex + 41) cb.Enabled = false;
                    else cb.Enabled = true;
                }
           }
            foreach (Control cb in groupBox3.Controls)
            {
                if (cb.GetType() == typeof(ComboBox))
                {
                    if (cb.TabIndex > cmbVoices.SelectedIndex + 101) cb.Enabled = false;
                    else cb.Enabled = true;
                }
                if (cb.GetType() == typeof(Label))
                {
                    if (cb.TabIndex > cmbVoices.SelectedIndex + 111) cb.Enabled = false;
                    else cb.Enabled = true;
                }
                if (cb.GetType() == typeof(Button))
                {
                    if (cb.TabIndex > cmbVoices.SelectedIndex + 121) cb.Enabled = false;
                    else cb.Enabled = true;
                }
            }
        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

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

                if (Path.GetExtension(txtFile1.Text) != ".wav")
                {
                    MessageBox.Show("There is no file selected for Voice 1 yet.");
                    checkBox1.Checked = false;
                    return;
                }
                String baseName = "";
                baseName = Path.GetFullPath(txtFile1.Text);
                baseName = baseName.Remove(baseName.Length - 5,5);
                txtFile2.Text = baseName + "2.wav";
                txtFile3.Text = baseName + "3.wav";
                txtFile4.Text = baseName + "4.wav";
                txtFile5.Text = baseName + "5.wav";
                txtFile6.Text = baseName + "6.wav";
                txtFile7.Text = baseName + "7.wav";
                txtFile8.Text = baseName + "8.wav";
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
                txtFile2.Text = "";
                txtFile3.Text = "";
                txtFile4.Text = "";
                txtFile5.Text = "";
                txtFile6.Text = "";
                txtFile7.Text = "";
                txtFile8.Text = "";
            }

        }

        private void btnFile1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Wave files (*.wav)|*.wav";
            ofd.ShowDialog();
            if (Path.GetExtension(ofd.FileName) == ".wav")
            {
                txtFile1.Text = ofd.FileName;
            }
            else
            {
                MessageBox.Show("This is not a .wav file, or no file was selected.");
                return;
            }

            
        }

        private void btnFile2_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Wave files (*.wav)|*.wav";
            ofd.ShowDialog();
            if (Path.GetExtension(ofd.FileName) == ".wav")
            {
                txtFile2.Text = ofd.FileName;
            }
            else
            {
                MessageBox.Show("This is not a .wav file, or no file was selected.");
                return;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("SidWiz 1.0 by Rolf R Bakke \r\nSidWiz 2 by RushJet1\r\nAVIFile Wrapper by Corinna John\r\nWAVFile class by CalicoSkies");
        }

        private void btnFile3_Click_1(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Wave files (*.wav)|*.wav";
            ofd.ShowDialog();
            if (Path.GetExtension(ofd.FileName) == ".wav")
            {
                txtFile3.Text = ofd.FileName;
            }
            else
            {
                MessageBox.Show("This is not a .wav file, or no file was selected.");
                return;
            }
        }

        private void btnFile4_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Wave files (*.wav)|*.wav";
            ofd.ShowDialog();
            if (Path.GetExtension(ofd.FileName) == ".wav")
            {
                txtFile4.Text = ofd.FileName;
            }
            else
            {
                MessageBox.Show("This is not a .wav file, or no file was selected.");
                return;
            }
        }

        private void btnFile5_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Wave files (*.wav)|*.wav";
            ofd.ShowDialog();
            if (Path.GetExtension(ofd.FileName) == ".wav")
            {
                txtFile5.Text = ofd.FileName;
            }
            else
            {
                MessageBox.Show("This is not a .wav file, or no file was selected.");
                return;
            }
        }

        private void btnFile6_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Wave files (*.wav)|*.wav";
            ofd.ShowDialog();
            if (Path.GetExtension(ofd.FileName) == ".wav")
            {
                txtFile6.Text = ofd.FileName;
            }
            else
            {
                MessageBox.Show("This is not a .wav file, or no file was selected.");
                return;
            }
        }

        private void btnFile7_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Wave files (*.wav)|*.wav";
            ofd.ShowDialog();
            if (Path.GetExtension(ofd.FileName) == ".wav")
            {
                txtFile7.Text = ofd.FileName;
            }
            else
            {
                MessageBox.Show("This is not a .wav file, or no file was selected.");
                return;
            }
        }

        private void btnFile8_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Wave files (*.wav)|*.wav";
            ofd.ShowDialog();
            if (Path.GetExtension(ofd.FileName) == ".wav")
            {
                txtFile8.Text = ofd.FileName;
            }
            else
            {
                MessageBox.Show("This is not a .wav file, or no file was selected.");
                return;
            }
        }

        

    }
}

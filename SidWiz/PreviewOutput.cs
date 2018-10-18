using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace SidWiz
{
    internal class PreviewOutput : IGraphicsOutput, IDisposable
    {
        private readonly int _frameSkip;
        private readonly Form2 _form;
        private int _frameIndex;
        private readonly Stopwatch _stopwatch;

        public PreviewOutput(int frameSkip)
        {
            _frameSkip = frameSkip;
            _form = new Form2();
            _form.Show();
            _form.SetDesktopLocation(0, 0);
            _stopwatch = Stopwatch.StartNew();
        }

        public void Write(byte[] data, Image image, double fractionComplete)
        {
            if (!_form.Visible)
            {
                throw new Exception("Display form closed");
            }

            if (++_frameIndex % _frameSkip != 0)
            {
                return;
            }

            _form.pictureBox1.Image = image;
            _form.pictureBox1.Refresh();
            var elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;
            var fps = _frameIndex / elapsedSeconds;
            var eta = TimeSpan.FromSeconds(elapsedSeconds / fractionComplete - elapsedSeconds);
            _form.toolStripStatusLabel2.Text = $"{fractionComplete:P} @ {fps:F}fps, ETA {eta:g}";
            Application.DoEvents();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _form?.Close();
            _form?.Dispose();
        }
    }
}
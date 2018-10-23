using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace LibSidWiz.Outputs
{
    public class PreviewOutput : IGraphicsOutput
    {
        private readonly int _frameSkip;
        private readonly PreviewOutputForm _form;
        private int _frameIndex;
        private readonly Stopwatch _stopwatch;

        public PreviewOutput(int frameSkip)
        {
            _frameSkip = frameSkip;
            _form = new PreviewOutputForm();
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

            // TODO: support this being called on another thread
            // - take a copy of the image
            // - BeginInvoke an action to render it
            // - Make GUI driver run the render on a worker thread
            // This may help with the framerate. It's never going to be that fast though...

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
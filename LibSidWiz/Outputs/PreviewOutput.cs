using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace LibSidWiz.Outputs
{
    public class PreviewOutput : IGraphicsOutput
    {
        private readonly int _frameSkip;
        private readonly bool _pumpMessageQueue;
        private readonly PreviewOutputForm _form;
        private int _frameIndex;
        private readonly Stopwatch _stopwatch;

        public PreviewOutput(int frameSkip, bool pumpMessageQueue = false)
        {
            _frameSkip = frameSkip;
            _pumpMessageQueue = pumpMessageQueue;
            _form = new PreviewOutputForm();
            _form.Show();
            _form.SetDesktopLocation(0, 0);
            _stopwatch = Stopwatch.StartNew();
        }

        public void Write(byte[] data, Image image, double fractionComplete)
        {
            if (!_form.Visible)
            {
                throw new Exception("Preview window closed");
            }

            if (++_frameIndex % _frameSkip != 0)
            {
                return;
            }

            // Copy the bitmap for use on the GUI thread
            var copy = new Bitmap(image);
            UpdatePreview(fractionComplete, copy);
        }

        private void UpdatePreview(double fractionComplete, Bitmap image)
        {
            var elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;
            var fps = _frameIndex / elapsedSeconds;
            var eta = TimeSpan.FromSeconds(elapsedSeconds / fractionComplete - elapsedSeconds);
            _form.BeginInvoke(new Action(() =>
            {
                if (_form.IsDisposed || !_form.Visible)
                {
                    return;
                }

                if (fractionComplete < 2e-6)
                {
                    image.Save($"foo.{fractionComplete}.png");
                }

                _form.pictureBox1.Image = image;
                _form.toolStripStatusLabel2.Text = $"{fractionComplete:P} @ {fps:F}fps, ETA {eta:g}";
            }));

            if (_pumpMessageQueue)
            {
                Application.DoEvents();
            }
        }

        public void Write(byte[] data, SKImage image, double fractionComplete)
        {
            if (!_form.Visible)
            {
                throw new Exception("Preview window closed");
            }

            if (++_frameIndex % _frameSkip != 0)
            {
                return;
            }

            // Copy the image to a Bitmap
            UpdatePreview(fractionComplete, image.ToBitmap());
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _form?.Invoke(new Action(() =>
            {
                _form?.Dispose();
            }));
        }
    }
}
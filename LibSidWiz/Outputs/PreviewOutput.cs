using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Taskbar;

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
            var elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;
            var fps = _frameIndex / elapsedSeconds;
            var eta = TimeSpan.FromSeconds(elapsedSeconds / fractionComplete - elapsedSeconds);
            _form.BeginInvoke(new Action(() =>
            {
                if (_form.IsDisposed || !_form.Visible)
                {
                    return;
                }
                _form.pictureBox1.Image = copy;
                _form.toolStripStatusLabel2.Text = $"{fractionComplete:P} @ {fps:F}fps, ETA {eta:g}";
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal, _form.Handle);
                TaskbarManager.Instance.SetProgressValue((int)(fractionComplete * 100), 100, _form.Handle);
            }));

            if (_pumpMessageQueue)
            {
                Application.DoEvents();
            }
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _form?.Close();
            _form?.Dispose();
        }
    }
}
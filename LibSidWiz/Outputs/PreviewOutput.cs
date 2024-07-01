using System;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Taskbar;
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
        private TimeSpan _lastFpsUpdateTime = TimeSpan.Zero;

        public PreviewOutput(int frameSkip, bool pumpMessageQueue = false)
        {
            _frameSkip = frameSkip;
            _pumpMessageQueue = pumpMessageQueue;
            _form = new PreviewOutputForm();
            _form.Show();
            _form.SetDesktopLocation(0, 0);
            _stopwatch = Stopwatch.StartNew();
        }

        public void Write(SKImage image, byte[] data, double fractionComplete, TimeSpan length)
        {
            if (!_form.Visible)
            {
                throw new Exception("Preview window closed");
            }

            // Post-increment so we take frame 0
            var showFrame = _frameIndex++ % _frameSkip == 0;
            var showFps = showFrame || (_stopwatch.Elapsed - _lastFpsUpdateTime).TotalMilliseconds > 100;
            if (showFps)
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
                    _form.toolStripStatusLabel2.Text = $"{fractionComplete:P} of {length} @ {fps:F}fps, ETA {eta:g}";
                    TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal, _form.Handle);
                    TaskbarManager.Instance.SetProgressValue((int)(fractionComplete * 100), 100, _form.Handle);
                }));
                _lastFpsUpdateTime = _stopwatch.Elapsed;
            }

            if (showFrame)
            {
                // Copy the bitmap for use on the GUI thread
                var copy = image.ToBitmap();
                _form.BeginInvoke(new Action(() =>
                {
                    if (_form.IsDisposed || !_form.Visible)
                    {
                        return;
                    }

                    _form.pictureBox1.Image = copy;
                }));
            }

            if (_pumpMessageQueue)
            {
                Application.DoEvents();
            }
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            try
            {
                _form?.BeginInvoke(() =>
                    {
                        _form?.Close();
                        _form?.Dispose();
                    }
                );
            }
            catch (Exception)
            {
                // We might get this if exiting the program
            }
        }
    }
}
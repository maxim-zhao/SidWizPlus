using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace LibSidWiz
{
    public class ProcessWrapper: IDisposable
    {
        private readonly Process _process;
        private readonly BlockingCollection<string> _lines = new BlockingCollection<string>(new ConcurrentQueue<string>());
        private readonly CancellationTokenSource _cancellationTokenSource;
        private int _streamCount;

        public ProcessWrapper(string filename, string arguments, bool captureStdErr = false, bool captureStdOut = true, bool showConsole = false)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = filename,
                    Arguments = arguments,
                    RedirectStandardOutput = captureStdOut,
                    RedirectStandardError = captureStdErr,
                    UseShellExecute = false,
                    CreateNoWindow = !showConsole
                },
                EnableRaisingEvents = true
            };
            if (_process == null)
            {
                throw new Exception($"Error running {filename} {arguments}");
            }

            if (captureStdOut)
            {
                _process.OutputDataReceived += OnText;
            }
            if (captureStdErr)
            {
                _process.ErrorDataReceived += OnText;
            }

            _process.Start();

            if (captureStdOut)
            {
                _process.BeginOutputReadLine();
                ++_streamCount;
            }
            if (captureStdErr)
            {
                _process.BeginErrorReadLine();
                ++_streamCount;
            }
        }

        private void OnText(object sender, DataReceivedEventArgs e)
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }
            try
            {
                _lines.TryAdd(e.Data, -1, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Discard it
            }
        }

        /// <summary>
        /// Blocks while waiting for the next line...
        /// </summary>
        public IEnumerable<string> Lines()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                string line;
                try
                {
                    // Blocking take
                    line = _lines.Take(_cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    yield break;
                }

                if (line == null)
                {
                    if (--_streamCount == 0)
                    {
                        // We see a null to indicate the end of each stream. We break on the last one.
                        yield break;
                    }
                    // Else drop it
                    continue;
                }

                yield return line;
            }
        }

        public void WaitForExit() => _process.WaitForExit();

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _lines.CompleteAdding();
            if (_process != null)
            {
                // Try to kill the process if it is alive
                if (!_process.HasExited)
                {
                    try
                    {
                        _process.EnableRaisingEvents = false;
                        _process.Kill();
                    }
                    catch (Exception)
                    {
                        // May throw if the process terminates first
                    }
                }

                _process.Dispose();
            }

            _cancellationTokenSource.Dispose();
            _lines.Dispose();
        }
    }
}

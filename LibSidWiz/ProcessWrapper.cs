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

        public ProcessWrapper(string filename, string arguments)
        {
            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = filename,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };
            if (_process == null)
            {
                throw new Exception($"Error running {filename} {arguments}");
            }
            _process.OutputDataReceived += (sender, e) =>
            {
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        _lines.TryAdd(e.Data, 0, _cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Discard it
                    }
                }
            };
            _process.Start();
            _process.BeginOutputReadLine();

            _cancellationTokenSource = new CancellationTokenSource();
        }

        private readonly BlockingCollection<string> _lines = new BlockingCollection<string>(new ConcurrentQueue<string>());
        private readonly CancellationTokenSource _cancellationTokenSource;

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
                    yield break;
                }

                yield return line;
            }
        }

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

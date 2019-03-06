using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

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
                _lines.Add(e.Data);
            };
            _process.Start();
            _process.BeginOutputReadLine();
        }

        private readonly BlockingCollection<string> _lines = new BlockingCollection<string>(new ConcurrentQueue<string>());

        /// <summary>
        /// Should be called on a worker thread because it blocks...
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> Lines()
        {
            while (!_lines.IsCompleted)
            {
                // Blocking take
                var line = _lines.Take();
                if (line != null)
                {
                    yield return line;
                }
                else
                {
                    yield break;
                }
            }
        }

        public void Dispose()
        {
            if (_process != null)
            {
                if (!_process.HasExited)
                {
                    _process.Kill();
                }

                _process.Dispose();
            }

            _lines?.Dispose();
        }
    }
}

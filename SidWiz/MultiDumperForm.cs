using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace SidWiz
{
    public partial class MultiDumperForm : Form
    {
        private readonly string _filename;
        private readonly string _multidumperPath;
        private int _channelCount;

        public MultiDumperForm(string filename, string multidumperPath)
        {
            _filename = filename;
            _multidumperPath = multidumperPath;
            InitializeComponent();
        }

        public IEnumerable<string> Filenames { get; set; }

        private class ProcessWrapper: IDisposable
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
                _process?.Dispose();
                _lines?.Dispose();
            }
        }

        private void OkButtonClick(object sender, EventArgs e)
        {
            // We start a task to wrap the load task
            ProgressBar.Style = ProgressBarStyle.Continuous;
            OKButton.Enabled = false;

            var index = Subsongs.SelectedIndex.ToString();
            ProgressBar.Maximum = _channelCount * 100;
            Task.Factory.StartNew(() =>
            {
                using (var p = new ProcessWrapper(
                    _multidumperPath,
                    $"\"{_filename}\" {index}"))
                {
                    var progressParts = Enumerable.Repeat(0.0, _channelCount).ToList();
                    var r = new Regex(@"(?<channel>\d+)\|(?<position>\d+)\|(?<total>\d+)");
                    var stopwatch = Stopwatch.StartNew();
                    foreach (var match in p.Lines().Select(l => r.Match(l)).Where(m => m.Success))
                    {
                        var channel = Convert.ToInt32(match.Groups["channel"].Value);
                        if (channel < 0 || channel > _channelCount)
                        {
                            continue;
                        }
                        var position = Convert.ToDouble(match.Groups["position"].Value);
                        var total = Convert.ToDouble(match.Groups["total"].Value);
                        progressParts[channel] = position / total;
                        if (stopwatch.Elapsed.TotalMilliseconds > 100)
                        {
                            // Update the progress bar every 100ms
                            var progress = (int) (progressParts.Sum() * 100);
                            ProgressBar.BeginInvoke(new Action(() => ProgressBar.Value = progress));
                            stopwatch.Restart();
                        }
                    }

                    BeginInvoke(new Action(() =>
                    {
                        // Stop the animation
                        ProgressBar.Style = ProgressBarStyle.Blocks;

                        // Then check for generated files
                        var directory = Path.GetDirectoryName(_filename);
                        if (directory != null)
                        {
                            Filenames = Directory.EnumerateFiles(
                                directory,
                                Path.GetFileNameWithoutExtension(_filename) + " - *.wav")
                                .OrderBy(x => x)
                                .ToList();
                        }

                        DialogResult = DialogResult.OK;
                        Close();
                    }));
                }
            });
        }

        private void SubsongSelectionForm_Load(object sender, EventArgs e)
        {
            Subsongs.Items.Clear();
            Subsongs.Items.Add($"Checking {_filename}...");
            // Start a task to load the metadata
            Task.Factory.StartNew(() =>
            {
                using (var p = new ProcessWrapper(
                    _multidumperPath,
                    $"\"{_filename}\" --json"))
                {
                    var result = string.Join("", p.Lines());

                    // Extract metadata
                    dynamic metadata = JsonConvert.DeserializeObject(result);

                    Subsongs.BeginInvoke(new Action(() =>
                    {
                        // Back on the GUI thread...
                        Subsongs.Items.Clear();
                        if (metadata.subsongCount < 1)
                        {
                            return;
                        }

                        // This helps us reject any junk strings MultiDumper gives us for empty tags
                        string Clean(string s) => s.Any(char.IsControl) ? string.Empty : s;

                        int index = 0;
                        foreach (var song in metadata.songs)
                        {
                            Subsongs.Items.Add($"{index++}. {Clean(song.name)} - {Clean(song.author)} ({Clean(song.comment)})");
                        }

                        Subsongs.SelectedIndex = 0;
                        OKButton.Enabled = true;

                        _channelCount = metadata.channels.Count;

                        if (metadata.subsongCount == 1)
                        {
                            // If only one song, select it
                            OkButtonClick(this, new EventArgs());
                        }
                    }));
                }
            });
        }
    }
}

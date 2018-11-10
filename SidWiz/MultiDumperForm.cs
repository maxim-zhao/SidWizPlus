using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace SidWiz
{
    public partial class MultiDumperForm : Form
    {
        private readonly string _filename;
        private readonly string _multidumperPath;

        public MultiDumperForm(string filename, string multidumperPath)
        {
            _filename = filename;
            _multidumperPath = multidumperPath;
            InitializeComponent();
        }

        public IEnumerable<string> Filenames { get; set; }

        private string RunMultiDumper(string filename, string args)
        {
            using (var p = Process.Start(new ProcessStartInfo
            {
                FileName = _multidumperPath,
                Arguments = $"\"{filename}\" {args}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }))
            {
                if (p == null)
                {
                    throw new Exception("Failed to launch MultiDumper");
                }
                p.WaitForExit();
                return p.StandardOutput.ReadToEnd();
            }
        }


        private void OKButtonClick(object sender, EventArgs e)
        {
            // We start a task to wrap the load task
            // We don't bother with real progress... this makes it be "indeterminate".
            ProgressBar.Style = ProgressBarStyle.Marquee;
            OKButton.Enabled = false;

            var index = Subsongs.SelectedIndex.ToString();
            var loadTask = Task.Factory.StartNew(() => RunMultiDumper(_filename, index));
            while (!loadTask.IsCompleted)
            {
                Application.DoEvents();
                loadTask.Wait(10);
            }
            // Stop the animation
            ProgressBar.Style = ProgressBarStyle.Blocks;

            // Then check for generated files
            var directory = Path.GetDirectoryName(_filename);
            if (directory != null)
            {
                Filenames = Directory.EnumerateFiles(
                    directory,
                    Path.GetFileNameWithoutExtension(_filename) + " - *.wav").ToList();
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void SubsongSelectionForm_Load(object sender, EventArgs e)
        {
            // Start a task to load the metadata
            Task.Factory.StartNew(() =>
            {
                // Extract metadata
                dynamic metadata = JsonConvert.DeserializeObject(RunMultiDumper(_filename, "--json"));

                Subsongs.BeginInvoke(new Action(() =>
                {
                    if (metadata.subsongCount < 1)
                    {
                        return;
                    }

                    int index = 0;
                    foreach (var song in metadata.songs)
                    {
                        Subsongs.Items.Add($"{index++}. {song.name} - {song.author} ({song.comment})");
                    }

                    Subsongs.SelectedIndex = 0;
                    OKButton.Enabled = true;

                    if (metadata.subsongCount == 1)
                    {
                        OKButtonClick(this, new EventArgs());
                    }
                }));
            });
        }
    }
}

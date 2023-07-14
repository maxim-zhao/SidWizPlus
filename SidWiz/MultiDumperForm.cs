using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibSidWiz;

namespace SidWizPlusGUI
{
    public partial class MultiDumperForm : Form
    {
        private readonly string _filename;
        private readonly MultiDumperWrapper _wrapper;

        public IEnumerable<string> Filenames { get; private set; }

        public MultiDumperForm(string filename, string multiDumperPath, int samplingRate, int loopCount, int fadeMs, int gapMs)
        {
            _filename = filename;
            _wrapper = new MultiDumperWrapper(multiDumperPath, samplingRate, loopCount, fadeMs, gapMs);
            InitializeComponent();
        }

        private void OkButtonClick(object sender, EventArgs e)
        {
            if (!(Subsongs.SelectedItem is MultiDumperWrapper.Song song))
            {
                return;
            }

            if (song.GetLength() <= TimeSpan.Zero)
            {
                // Try to parse the text box
                if (!TimeSpan.TryParseExact(lengthBox.Text, "m\\:ss", null, out var length))
                {
                    return;
                }

                song.ForceLength = length;
            }

            OKButton.Enabled = false;

            // We start a task to wrap the load task
            Task.Factory.StartNew(() =>
            {
                try
                {
                    Filenames = _wrapper.Dump(song,
                        progress =>
                        {
                            ProgressBar.BeginInvoke(
                                new Action(() => ProgressBar.Value = (int) (progress * 100)));
                        }).ToList();

                    BeginInvoke(new Action(() =>
                    {
                        DialogResult = DialogResult.OK;
                        Close();
                    }));
                }
                catch (Exception)
                {
                    BeginInvoke(new Action(() =>
                    {
                        DialogResult = DialogResult.Cancel;
                        Filenames = null;
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
                try
                {
                    var songs = _wrapper.GetSongs(_filename).ToList();
                    Subsongs.BeginInvoke(new Action(() =>
                    {
                        // Back on the GUI thread...
                        Subsongs.Items.Clear();
                        Subsongs.Items.AddRange(songs.ToArray<object>());

                        Subsongs.SelectedIndex = 0;
                        OKButton.Enabled = true;

                        if (songs.Count == 1 && songs[0].GetLength() > TimeSpan.Zero)
                        {
                            // If only one song, and it has a length, choose it
                            OkButtonClick(this, EventArgs.Empty);
                        }
                    }));
                }
                catch (Exception ex)
                {
                    Subsongs.BeginInvoke(new Action(() =>
                    {
                        // Back on the GUI thread...
                        Subsongs.Items.Add($"Failed to read {_filename}: {ex.Message}");
                    }));
                }
            });
        }

        private void SubsongSelectionForm_Closing(object sender, EventArgs e)
        {
            _wrapper.Dispose();
        }

        private void Subsongs_SelectedIndexChanged(object sender, EventArgs e)
        {
            // We enable the length controls if the track is missing info
            label1.Enabled = lengthBox.Enabled = Subsongs.SelectedItem is MultiDumperWrapper.Song s && s.GetLength() <= TimeSpan.Zero;
        }
    }
}

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

        public IEnumerable<string> Filenames { get; set; }

        public MultiDumperForm(string filename, string multiDumperPath)
        {
            _filename = filename;
            _wrapper = new MultiDumperWrapper(multiDumperPath);
            InitializeComponent();
        }

        private void OkButtonClick(object sender, EventArgs e)
        {
            // We start a task to wrap the load task
            OKButton.Enabled = false;

            if (Subsongs.SelectedItem is MultiDumperWrapper.Song song)
            {
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
        }

        private void SubsongSelectionForm_Load(object sender, EventArgs e)
        {
            Subsongs.Items.Clear();
            Subsongs.Items.Add($"Checking {_filename}...");
            // Start a task to load the metadata
            Task.Factory.StartNew(() =>
            {
                var songs = _wrapper.GetSongs(_filename);
                Subsongs.BeginInvoke(new Action(() =>
                {
                    // Back on the GUI thread...
                    Subsongs.Items.Clear();
                    Subsongs.Items.AddRange(songs.ToArray<object>());

                    Subsongs.SelectedIndex = 0;
                    OKButton.Enabled = true;

                    if (Subsongs.Items.Count == 1)
                    {
                        // If only one song, select it
                        OkButtonClick(this, new EventArgs());
                    }
                }));
            });
        }

        private void SubsongSelectionForm_Closing(object sender, EventArgs e)
        {
            _wrapper.Dispose();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibSidWiz;

namespace SidWizPlusGUI
{
    public partial class SidPlayForm : Form
    {
        public class SidFile
        {
            public string Filename { get; }

            public string Released { get; }

            public string Name { get; }

            public string Author { get; }

            public int SongCount { get; }

            public string Chip { get; }

            // ReSharper disable once MemberCanBePrivate.Global
            public int Version { get; }

            public SidFile(string filename)
            {
                Filename = filename;
                using (var f = new FileStream(filename, FileMode.Open))
                using (var r = new BinaryReader(f, Encoding.ASCII))
                {
                    // C64 is big-endian...
                    f.Seek(0x04, SeekOrigin.Begin);
                    Version = r.ReadByte() << 8 | r.ReadByte();
                    f.Seek(0x0e, SeekOrigin.Begin);
                    SongCount = r.ReadByte() << 8 | r.ReadByte();
                    f.Seek(0x16, SeekOrigin.Begin);
                    Name = new string(r.ReadChars(32)).TrimEnd('\0');
                    Author = new string(r.ReadChars(32)).TrimEnd('\0');
                    Released = new string(r.ReadChars(32)).TrimEnd('\0');

                    if (Version >= 2)
                    {
                        f.Seek(0x76, SeekOrigin.Begin);
                        var flags = r.ReadByte() << 8 | r.ReadByte();
                        switch ((flags >> 4) & 0b11)
                        {
                            case 0b00:
                                Chip = "SID";
                                break;
                            case 0b01:
                                Chip = "MOS6581";
                                break;
                            case 0b10:
                                Chip = "MOS8580";
                                break;
                            case 0b11:
                                Chip = "MOS6581 & MOS8580";
                                break;
                        }
                    }
                }
            }
        }

        public class SidPlayWrapper : IDisposable
        {
            private readonly string _sidPlayPath;
            private IList<ProcessWrapper> _processWrappers;

            public SidPlayWrapper(string sidPlayPath)
            {
                _sidPlayPath = sidPlayPath;
            }

            public class Song
            {
                public SidFile File { get; set; }
                public int Index { get; set; }

                public override string ToString()
                {
                    return $"#{Index}: {File.Name} - {File.Author} - {File.Released}";
                }
            }

            public IEnumerable<Song> GetSongs(string filename)
            {
                filename = Path.GetFullPath(filename);

                if (!File.Exists(filename))
                {
                    throw new FileNotFoundException("Cannot find file", filename);
                }

                var file = new SidFile(filename);
                return Enumerable.Range(1, file.SongCount).Select(index => new Song {File = file, Index = index});
            }

            public IEnumerable<string> Dump(Song song)
            {
                var filenames = new List<string>();

                _processWrappers = Enumerable.Range(1, 4).Select(channel =>
                {
                    var muting = string.Join(" ",
                        Enumerable.Range(1, 4).Where(n => n != channel).Select(n => $"-u{n}"));
                    var filename = $"{song.File.Filename} - Song {song.Index} - {song.File.Chip} #{channel}.wav";
                    filenames.Add(filename);
                    return new ProcessWrapper(
                        _sidPlayPath,
                        $"{muting} -os -o{song.Index} -w\"{filename}\" \"{song.File.Filename}\"");
                }).ToList();
                    
                // sidplayfp doesn't emit anything to stdout... so we just block until they all exit
                foreach (var wrapper in _processWrappers)
                {
                    wrapper.WaitForExit();
                    wrapper.Dispose();
                }

                _processWrappers.Clear();

                // Older sidplayfp versions use the filename we give; newer ones always add .wav to the end.
                // We check which one appeared.
                return filenames.Select(x => File.Exists(x + ".wav")
                    ? x + ".wav"
                    : x);
            }

            public void Dispose()
            {
                foreach (var wrapper in _processWrappers)
                {
                    wrapper.Dispose();
                }
            }
        }


        private readonly string _filename;
        private readonly SidPlayWrapper _wrapper;

        public IEnumerable<string> Filenames { get; private set; }

        public SidPlayForm(string filename, string sidPlayPath)
        {
            _filename = filename;
            _wrapper = new SidPlayWrapper(sidPlayPath);
            InitializeComponent();
        }

        private void OkButtonClick(object sender, EventArgs e)
        {
            OKButton.Enabled = false;

            if (Subsongs.SelectedItem is SidPlayWrapper.Song song)
            {
                ProgressBar.Style = ProgressBarStyle.Marquee;

                // We start a task to wrap the load task
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Filenames = _wrapper.Dump(song).ToList();

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
                        OkButtonClick(this, EventArgs.Empty);
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

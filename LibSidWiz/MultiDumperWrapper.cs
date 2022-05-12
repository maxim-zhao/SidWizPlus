using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LibSidWiz
{
    public class MultiDumperWrapper: IDisposable
    {
        private readonly string _multiDumperPath;
        private readonly int _samplingRate;
        private readonly int _loopCount;
        private readonly int _fadeMs;
        private readonly int _gapMs;
        private ProcessWrapper _processWrapper;
        private readonly HashSet<string> _allowedParameters;

        public MultiDumperWrapper(string multiDumperPath, int samplingRate, int loopCount, int fadeMs, int gapMs)
        {
            _multiDumperPath = multiDumperPath;
            _samplingRate = samplingRate;
            _loopCount = loopCount;
            _fadeMs = fadeMs;
            _gapMs = gapMs;

            // We parse the usage info first to check for allowed parameters
            var helpText = GetOutputText("", true);
            _allowedParameters = new HashSet<string>(
                Regex.Matches(helpText, "--[^]=]+")
                    .Cast<Match>()
                    .Select(x => x.Value));
        }

        public class Song
        {
            // ReSharper disable UnusedAutoPropertyAccessor.Global
            public string Name { get; set; }
            public string Author { get; set; }
            public string Comment { get; set; }
            public string Copyright { get; set; }
            public string Dumper { get; set; }
            public string Game { get; set; }
            public string System { get; set; }
            public List<string> Channels { get; set; }
            public int Index { get; set; }
            public string Filename { get; set; }
            public TimeSpan IntroLength { get; set; }
            public TimeSpan LoopLength { get; set; }
            // ReSharper restore UnusedAutoPropertyAccessor.Global
            public int LoopCount { get; set; }
            public TimeSpan ForceLength { get; set; } = TimeSpan.Zero;

            public override string ToString()
            {
                var length = GetLength();

                var sb = new StringBuilder();
                sb.Append($"#{Index}: ")
                    .Append(string.IsNullOrWhiteSpace(Game) ? "Unknown game" : Game)
                    .Append(" - ")
                    .Append(string.IsNullOrWhiteSpace(Name) ? "Unknown title" : Name)
                    .Append(" - ")
                    .Append(string.IsNullOrWhiteSpace(Author) ? "Unknown author" : Author)
                    .Append(string.IsNullOrWhiteSpace(Comment) ? "" : $" ({Comment})")
                    .Append(length <= TimeSpan.Zero ? " (Unknown length)" : $" ({length})");
                return sb.ToString();
            }

            public TimeSpan GetLength()
            {
                if (ForceLength > TimeSpan.Zero)
                {
                    return ForceLength;
                }

                var length = TimeSpan.Zero;
                if (IntroLength > TimeSpan.Zero)
                {
                    length += IntroLength;
                }

                if (LoopLength > TimeSpan.Zero)
                {
                    length += TimeSpan.FromTicks(LoopLength.Ticks * LoopCount);
                }

                return length;
            }
        }

        public IEnumerable<Song> GetSongs(string filename)
        {
            filename = Path.GetFullPath(filename);

            if (!File.Exists(filename))
            {
                throw new FileNotFoundException("Cannot find VGM file", filename);
            }

            // Don't check the --json parameter as it may not have mentioned it for an old build
            var json = GetOutputText($"\"{filename}\" --json", false);

            if (string.IsNullOrEmpty(json))
            {
                throw new Exception("Failed to get song data from MultiDumper");
            }

            // Extract metadata
            // Example result:
            // {
            //  "channels":[
            //      "SEGA PSG #0","SEGA PSG #1","SEGA PSG #2","SEGA PSG #3",
            //      "YM2413 #0","YM2413 #1","YM2413 #2","YM2413 #3","YM2413 #4","YM2413 #5","YM2413 #6",
            //      "YM2413 #7","YM2413 #8","YM2413 #9","YM2413 #10","YM2413 #11","YM2413 #12","YM2413 #13"],
            //  "containerinfo":
            //  {
            //      "copyright":"1988/08/14",
            //      "dumper":"sherpa",
            //      "game":"Golvellius - Valley of Doom",
            //      "system":"Sega Master System"
            //  },
            //  "songs":[
            //      {
            //          "author":"Masatomo Miyamoto, Takeshi Santo, Shin-kun, Pazu",
            //          "comment":"",
            //          "name":"Title Screen",
            //          "length":"12345"
            //      }],
            //  "subsongCount":1
            // }
            dynamic metadata = JsonConvert.DeserializeObject(json);

            if (metadata == null)
            {
                throw new Exception("Failed to parse song metadata");
            }
            var channels = metadata.channels.ToObject<List<string>>();
            var songs = (JArray) metadata.songs;
            var i = 0;

            // This helps us reject any junk strings MultiDumper gives us for empty tags
            string Clean(string s) => string.IsNullOrEmpty(s) || s.Any(char.IsControl) ? string.Empty : s;
            return songs.Cast<dynamic>().Select(s => new Song
            {
                Filename = filename,
                Index = i++,
                Name = Clean(s.name),
                Author = Clean(s.author),
                Channels = channels,
                Comment = Clean(s.comment),
                Copyright = Clean(metadata.containerinfo.copyright),
                Dumper = Clean(metadata.containerinfo.dumper),
                Game = Clean(metadata.containerinfo.game),
                System = Clean(metadata.containerinfo.system),
                IntroLength = TimeSpan.FromMilliseconds((int)(s.intro_length ?? 0)),
                LoopLength = TimeSpan.FromMilliseconds((int)(s.loop_length ?? 0)),
                LoopCount = _loopCount
            });
        }

        private string GetOutputText(string args, bool includeStdErr)
        {
            using (var p = new ProcessWrapper(
                _multiDumperPath,
                args,
                includeStdErr))
            {
                string text = string.Join("", p.Lines());
                // Try to decode any UTF-8 in there
                try
                {
                    text = Encoding.UTF8.GetString(Encoding.Default.GetBytes(text));
                }
                catch (Exception)
                {
                    // Ignore it, use unfixed string
                }
                return text;
            }
        }

        public IEnumerable<string> Dump(Song song, Action<double> onProgress)
        {
            var args = new StringBuilder($"\"{song.Filename}\" {song.Index}");
            AddArgIfSupported(args, "sampling_rate", _samplingRate);
            AddArgIfSupported(args, "fade_length", _fadeMs);
            AddArgIfSupported(args, "loop_count", _loopCount);
            AddArgIfSupported(args, "gap_length", _gapMs);
            if (song.ForceLength > TimeSpan.Zero)
            {
                AddArgIfSupported(args, "play_length", (long)song.ForceLength.TotalMilliseconds);
            }

            _processWrapper = new ProcessWrapper(
                _multiDumperPath,
                args.ToString());
            var progressParts = Enumerable.Repeat(0.0, song.Channels.Count).ToList();
            var r = new Regex(@"(?<channel>\d+)\|(?<position>\d+)\|(?<total>\d+)");
            var stopwatch = Stopwatch.StartNew();
            foreach (var match in _processWrapper.Lines().Select(l => r.Match(l)).Where(m => m.Success))
            {
                var channel = Convert.ToInt32(match.Groups["channel"].Value);
                if (channel < 0 || channel > song.Channels.Count)
                {
                    continue;
                }

                var position = Convert.ToDouble(match.Groups["position"].Value);
                var total = Convert.ToDouble(match.Groups["total"].Value);
                progressParts[channel] = position / total;
                if (stopwatch.Elapsed.TotalMilliseconds > 100)
                {
                    // Update the progress every 100ms
                    onProgress?.Invoke(progressParts.Average());
                    stopwatch.Restart();
                }
            }
            _processWrapper.Dispose();
            _processWrapper = null;

            onProgress?.Invoke(1.0);

            var baseName = Path.Combine(
                Path.GetDirectoryName(song.Filename) ?? "",
                Path.GetFileNameWithoutExtension(song.Filename));
            return song.Channels.Select(channel => $"{baseName} - {channel}.wav");
        }

        private void AddArgIfSupported(StringBuilder args, string name, object value)
        {
            if (_allowedParameters.Contains($"--{name}"))
            {
                args.Append($" --{name}={value}");
            }
        }

        public void Dispose()
        {
            _processWrapper?.Dispose();
        }
    }
}
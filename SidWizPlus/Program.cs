using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CommandLine;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using LibSidWiz;
using LibSidWiz.Outputs;
using LibSidWiz.Triggers;
using LibVgm;
using Channel = LibSidWiz.Channel;

namespace SidWizPlus
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class Program
    {
        // ReSharper disable once ClassNeverInstantiated.Local
        private class Settings
        {
            // ReSharper disable UnusedAutoPropertyAccessor.Local

            [Option('f', "files", Separator = ',', HelpText = "Input WAV files, comma-separated. Wildcards are accepted.", Group = "Inputs")] 
            public IEnumerable<string> InputFiles { get; set; }

            [Option('v', "vgm", Required = false, HelpText = "VGM file, if specified GD3 text is drawn", Group = "Inputs")]
            public string VgmFile { get; set; }

            [Option('m', "master", Required = false, HelpText = "Master audio file, if not specified then the inputs will be mixed to a new file")]
            public string MasterAudioFile { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("nomastermix", HelpText = "Disable automatic generation of master audio file (on by default)")]
            public bool NoMasterMix { get; set;}

            // ReSharper disable once StringLiteralTypo
            [Option("nomastermixreplaygain", HelpText = "Disable automatic ReplayGain adjustment of automatically generated master audio file (on by default)")]
            public bool NoMasterMixReplayGain { get; set;}

            [Option('o', "output", Required = false, HelpText = "Output file", Group = "Outputs")]
            public string OutputFile { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option('p', "previewframeskip", Required = false, HelpText = "Enable a preview window with the specified frameskip - higher values give faster rendering by not drawing every frame to the screen.", Group = "Outputs")]
            public int PreviewFrameskip { get; set; }

            [Option('w', "width", Required = false, HelpText = "Width of image rendered", Default = 720*16/9)]
            public int Width { get; set; }

            [Option('h', "height", Required = false, HelpText = "Height of image rendered", Default = 720)]
            public int Height { get; set; }

            [Option('c', "columns", Required = false, HelpText = "Number of columns to render", Default = 1)]
            public int Columns { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("maxaspectratio", Required = false, HelpText = "Maximum aspect ratio, used to automatically determine the number of columns", Default = -1.0)]
            public double MaximumAspectRatio { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("viewms", Required = false, HelpText = "Rendered view width in ms", Default = 35)]
            public int ViewWidthMs { get; set; }

            [Option('r', "fps", Required = false, HelpText = "Frame rate", Default = 60)]
            public int FramesPerSecond { get; set; }
            
            // ReSharper disable once StringLiteralTypo
            [Option("linewidth", Required = false, HelpText = "Line width", Default = 3)]
            public float LineWidth { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("linecolor", Required = false, HelpText = "Line color, can be hex or a .net color name", Default = "white")]
            public string LineColor { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("fillcolor", Required = false, HelpText = "Fill color, can be hex or a .net color name", Default = "transparent")]
            public string FillColor { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("fillbase", Required = false, HelpText = "Fill baseline, values in range -1..+1 make sense", Default = 0.0)]
            public double FillBase { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option('a', "autoscale", Required = false, HelpText = "Automatic scaling percentage. A value of 100 will make the peak amplitude just fit in the rendered area.")]
            public float AutoScalePercentage { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("autoscaleignorepercussion", Required = false, HelpText = "Makes automatic scaling ignore YM2413 percussion channels")]
            public bool AutoScaleIgnoreYm2413Percussion { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("labelsfromvgm", Required = false, HelpText = "Attempt to label channels based on their filename")]
            public bool ChannelLabelsFromVgm { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option('t', "triggeralgorithm", Required = false, HelpText = "Trigger algorithm name", Default = nameof(PeakSpeedTrigger))]
            public string TriggerAlgorithm { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("triggerlookahead", Required = false, HelpText = "Number of frames to allow the trigger to look ahead, zero means no lookahead", Default = 0)]
            public int TriggerLookahead { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("triggerlookaheadonfailure", Required = false, HelpText = "Number of frames to allow the trigger to look ahead when failing to find a match with the default", Default = 1)]
            public int TriggerLookaheadOnFailureFrames { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("highpass", Required = false, HelpText = "Enable high-pass filtering", Default = false)]
            public bool HighPass { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("ffmpeg", Required = false, HelpText = "Path to FFMPEG. Required if rendering to a file. Will be discovered if on the path.")]
            public string FfMpegPath { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("vcodec", Required = false, HelpText = "Video codec for FFMPEG", Default = "libx264")]
            public string VideoCodec { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("acodec", Required = false, HelpText = "Audio codec for FFMPEG", Default = "aac")]
            public string AudioCodec { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("ffmpegoptions", Required = false, 
                HelpText = "Extra commandline options for FFMPEG, e.g. to set the output format. Surround value with quotes and start with a space, e.g. \" -crf 20\", to avoid conflicts with other parameters.", Default = "")]
            public string FfMpegExtraOptions { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("multidumper", Required = false, HelpText = "Path to MultiDumper, if specified with --vgm and no --files then it will be invoked for the VGM")]
            // ReSharper disable once IdentifierTypo
            public string MultidumperPath { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("multidumpersamplingrate", Required = false, HelpText = "Sampling rate for MultiDumper", Default = 44100)]
            // ReSharper disable once IdentifierTypo
            public int MultidumperSamplingRate { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("multidumperloopcount", Required = false, HelpText = "Loop count for MultiDumper", Default = 2)]
            // ReSharper disable once IdentifierTypo
            public int MultidumperLoopCount { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("multidumperfadeoutms", Required = false, HelpText = "Fade out time after looping for MultiDumper, in ms", Default = 8000)]
            // ReSharper disable once IdentifierTypo
            public int MultidumperFadeOutMs { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("multidumpergapms", Required = false, HelpText = "Gap time for non-looping tracks for MultiDumper, in ms", Default = 1000)]
            // ReSharper disable once IdentifierTypo
            public int MultidumperGapMs { get; set; }
            
            // ReSharper disable once StringLiteralTypo
            [Option("multidumperoptions", Required = false, HelpText = "Extra arguments for MultiDumper. Surround value with quotes and start with a space, e.g. \" --default_filter\", to avoid conflicts with other parameters.", Default = "")]
            // ReSharper disable once IdentifierTypo
            public string MultidumperOptions { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("silencethreshold", Required = false, HelpText = "Amplitude range treated as silent auto-importing from MultiDumper", Default = 0.01f)]
            // ReSharper disable once IdentifierTypo
            public float SilenceThreshold { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("backgroundcolor", Required = false, HelpText = "Background color, can be hex or a .net color name", Default = "black")]
            public string BackgroundColor { get; set; }

            [Option("background", Required = false, HelpText = "Background image, drawn transparently in the background")]
            public string BackgroundImageFile { get; set; }
            
            [Option("logo", Required = false, HelpText = "Logo image, drawn in the lower right")]
            public string LogoImageFile { get; set; }
            
            // ReSharper disable once StringLiteralTypo
            [Option("gridcolor", Required = false, HelpText = "Grid color, can be hex or a .net color name", Default = "white")]
            public string GridColor { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("gridwidth", Required = false, HelpText = "Grid line width", Default = 0)]
            public float GridLineWidth { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("gridborder", Required = false, HelpText = "Draw a border around the waves as well as between them", Default = true)]
            public bool GridBorder { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("zerolinecolor", Required = false, HelpText = "Zero line color", Default = "white")]
            public string ZeroLineColor { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("zerolinewith", Required = false, HelpText = "Zero line width", Default = 0)]
            public float ZeroLineWidth { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("gd3font", Required = false, HelpText = "Font for GD3 info", Default = "Tahoma")]
            public string Gd3Font { get; set; }
            [Option("gd3size", Required = false, HelpText = "Font size (in points) for GD3 info", Default = 16)]
            public float Gd3FontSize { get; set; }
            [Option("gd3color", Required = false, HelpText = "Font color for GD3 info", Default = "white")]
            public string Gd3FontColor { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("labelsfont", Required = false, HelpText = "Font for channel labels")]
            public string ChannelLabelsFont { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("labelssize", HelpText = "Font size for channel labels", Default = 8)]
            public float ChannelLabelsSize { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("labelscolor", HelpText = "Font color for channel labels", Default = "white")]
            public string ChannelLabelsColor { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("labelspadding", HelpText = "Padding for channel labels - more specific settings override this", Default = 0)]
            public int ChannelLabelsPadding { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("labelspaddingleft", HelpText = "Left padding for channel labels")]
            public int? ChannelLabelsPaddingLeft { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("labelspaddingright", HelpText = "Right padding for channel labels")]
            public int? ChannelLabelsPaddingRight { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("labelspaddingtop", HelpText = "Top padding for channel labels")]
            public int? ChannelLabelsPaddingTop { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("labelspaddingbottom", HelpText = "Bottom padding for channel labels")]
            public int? ChannelLabelsPaddingBottom { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("labelsalignment", HelpText = "Alignment for channel labels", Default = ContentAlignment.TopLeft)]
            public ContentAlignment ChannelLabelsAlignment { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("youtubesecret", HelpText = "YouTube client secret JSON file. Use this to specify a custom OAth key if the embedded one doesn't work.")]
            public string YouTubeUploadClientSecret { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("youtubetitle", HelpText = "YouTube video title. If a VGM is specified then you can reference GD3 tags like [title], [system], [game], [composer]")]
            public string YouTubeTitle { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("youtubecategory", HelpText = "YouTube video category", Default = "Gaming")]
            public string YouTubeCategory { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("youtubetags", HelpText = "YouTube video tags, comma separated")]
            public string YouTubeTags { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("youtubetagsfromgd3", HelpText = "Populate additional tags from the GD3 tag")]
            public bool YouTubeTagsFromGd3 { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("youtubeplaylist", HelpText = "YouTube playlist title. If a VGM is specified then you can reference GD3 tags like [title], [system], [game], [composer]")]
            public string YouTubePlaylist { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("youtubeplaylistdescriptionfile", HelpText = "Use the specified file for the playlist description")]
            public string YouTubePlaylistDescriptionFile { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("youtubedescriptionsextratext", HelpText = "Extra text to append to descriptions")]
            public string YouTubeDescriptionsExtra { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("youtubeonly", HelpText = "Only upload to YouTube")]
            public bool YouTubeOnly { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("youtubemerge", HelpText = "Merge the specified videos (wildcard, results sorted alphabetically) to one file and upload to YouTube", Group="Inputs")]
            public string YouTubeMerge { get; set; }

            [Option("threads", HelpText = "Number of rendering threads to use. Defaults to as many CPUs as your computer has.", Default = -1)]
            public int ThreadCount { get; set; }

            [Option("verbose", HelpText = "Enable even more logging", Default = false)]
            public bool Verbose { get; set; }
        }

        static int Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;

                // ReSharper disable once RedundantNameQualifier
                new CommandLine.Parser(settings =>
                {
                    settings.CaseSensitive = false;
                    settings.AutoHelp = true;
                    settings.AutoVersion = true;
                    settings.HelpWriter = Console.Error;
                })
                    .ParseArguments<Settings>(args)
                    .WithParsed(Run);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Fatal: {e}");
                return 1;
            }

            return 0;
        }

        private static void Run(Settings settings)
        {
            if (!settings.YouTubeOnly)
            {
                if (settings.VgmFile != null)
                {
                    LogVerbose(settings, "Running MultiDumper because VGM file was specified");
                    RunMultiDumper(settings);
                }
                else
                {
                    // We want to expand any wildcards in the input file list (and also fully qualify them)
                    LogVerbose(settings, "Expanding wildcards in inputs...");
                    var inputs = new List<string>();
                    foreach (var inputFile in settings.InputFiles)
                    {
                        if (File.Exists(inputFile))
                        {
                            inputs.Add(Path.GetFullPath(inputFile));
                            continue;
                        }

                        var pattern = Path.GetFileName(inputFile) ??
                            throw new Exception($"Failed to match {inputFile}");
                        var pathPart = inputFile.Substring(0, inputFile.Length - pattern.Length);
                        var directory = pathPart.Length > 0
                            ? Path.GetFullPath(pathPart)
                            : Directory.GetCurrentDirectory();
                        var files = Directory.EnumerateFiles(directory, pattern).ToList();
                        if (files.Count == 0)
                        {
                            throw new Exception($"Failed to match {inputFile}");
                        }

                        inputs.AddRange(files);
                    }

                    settings.InputFiles = inputs;
                    LogVerbose(settings, string.Join("\n- ", Enumerable.Repeat("Input files are:", 1).Concat(inputs)));
                }

                if (settings.InputFiles == null || !settings.InputFiles.Any())
                {
                    throw new Exception("No inputs specified");
                }

                LogVerbose(settings, "Creating channel objects...");
                var channels = settings.InputFiles
                    .AsParallel()
                    .Select(filename =>
                    {
                        var channel = new Channel(false)
                        {
                            Filename = filename,
                            LineColor = ParseColor(settings.LineColor),
                            LineWidth = settings.LineWidth,
                            FillColor = ParseColor(settings.FillColor),
                            FillBase = settings.FillBase,
                            Label = Channel.GuessNameFromMultidumperFilename(filename),
                            Algorithm = CreateTriggerAlgorithm(settings.TriggerAlgorithm),
                            TriggerLookaheadFrames = settings.TriggerLookahead,
                            TriggerLookaheadOnFailureFrames = settings.TriggerLookaheadOnFailureFrames,
                            SilenceThreshold = settings.SilenceThreshold,
                            ZeroLineWidth = settings.ZeroLineWidth,
                            ZeroLineColor = ParseColor(settings.ZeroLineColor),
                            LabelFont = settings.ChannelLabelsFont == null
                                ? null
                                : new Font(settings.ChannelLabelsFont, settings.ChannelLabelsSize),
                            LabelColor = ParseColor(settings.ChannelLabelsColor),
                            LabelAlignment = settings.ChannelLabelsAlignment,
                            LabelMargins = new Padding(
                                settings.ChannelLabelsPaddingLeft ?? settings.ChannelLabelsPadding,
                                settings.ChannelLabelsPaddingTop ?? settings.ChannelLabelsPadding,
                                settings.ChannelLabelsPaddingRight ?? settings.ChannelLabelsPadding,
                                settings.ChannelLabelsPaddingBottom ?? settings.ChannelLabelsPadding),
                            HighPassFilter = settings.HighPass
                        };
                        channel.LoadDataAsync().Wait();
                        // We can only set this when the file is loaded
                        channel.ViewWidthInMilliseconds = settings.ViewWidthMs;
                        return channel;
                    })
                    .Where(ch => ch.SampleCount > 0 && !ch.IsSilent)
                    .OrderByAlphaNumeric(ch => ch.Filename)
                    .ToList();

                if (settings.AutoScalePercentage > 0)
                {
                    LogVerbose(settings, "Auto-scaling...");

                    float max;

                    static bool IsYm2413Percussion(Channel ch) =>
                        ch.Label.StartsWith("YM2413 ") && !ch.Label.StartsWith("YM2413 Tone");

                    if (settings.AutoScaleIgnoreYm2413Percussion)
                    {
                        var channelsToUse = channels.Where(channel => !IsYm2413Percussion(channel)).ToList();
                        if (channelsToUse.Count == 0)
                        {
                            // Fall back on overall max if all channels are percussion
                            channelsToUse = channels;
                        }

                        max = channelsToUse.Max(ch => ch.Max);
                    }
                    else
                    {
                        max = channels.Max(ch => ch.Max);
                    }

                    var scale = settings.AutoScalePercentage / 100 / max;
                    LogVerbose(settings, $"Applying scale of {scale} to all channels");
                    foreach (var channel in channels)
                    {
                        channel.Scale = scale;
                    }
                }

                if (settings.ChannelLabelsFromVgm && settings.VgmFile != null)
                {
                    LogVerbose(settings, "Guessing channel labels from VGM...");
                    TryGuessLabelsFromVgm(channels, settings.VgmFile);
                }

                if (settings.OutputFile != null)
                {
                    // Emit normalized data to a WAV file for later mixing
                    if (settings.MasterAudioFile == null && !settings.NoMasterMix)
                    {
                        settings.MasterAudioFile = settings.OutputFile + ".wav";
                        LogVerbose(settings, "Generating master audio file...");
                        Mixer.MixToFile(channels, settings.MasterAudioFile, !settings.NoMasterMixReplayGain);
                    }
                }

                LogVerbose(settings, "Starting render...");
                Render(settings, channels);
                LogVerbose(settings, "Render complete");

                foreach (var channel in channels)
                {
                    channel.Dispose();
                }
            }

            if (settings.YouTubeTitle != null)
            {
                if (settings.YouTubeMerge != null)
                {
                    UploadMergedToYouTube(settings).Wait();
                }
                else
                {
                    UploadToYouTube(settings).Wait();
                }
            }
        }

        private class InstrumentState
        {
            public int Instrument { private get; set; }
            public int Ticks { get; private set; }

            private static readonly string[] Names =
            [
                "Custom Instrument",
                "Violin",
                "Guitar",
                "Piano",
                "Flute",
                "Clarinet",
                "Oboe",
                "Trumpet",
                "Organ",
                "Horn",
                "Synthesizer",
                "Harpsichord",
                "Vibraphone",
                "Synthesizer Bass",
                "Acoustic Bass",
                "Electric Guitar"
            ];

            public string Name => Names[Instrument];

            public override string ToString() => $"{Name} ({TimeSpan.FromSeconds((double)Ticks / 44100)})";

            public void AddTime(int ticks)
            {
                Ticks += ticks;
            }
        }

        private class ChannelState
        {
            private readonly List<InstrumentState> _instruments = [];
            private readonly Dictionary<int, InstrumentState> _instrumentsByChannel = [];
            private InstrumentState _currentInstrument;
            public bool KeyDown { private get; set; }

            public void SetInstrument(int instrument)
            {
                if (!_instrumentsByChannel.TryGetValue(instrument, out var state))
                {
                    state = new InstrumentState {Instrument = instrument};
                    _instruments.Add(state);
                    _instrumentsByChannel.Add(instrument, state);
                }

                _currentInstrument = state;
            }

            public void AddTime(int ticks)
            {
                if (KeyDown)
                {
                    _currentInstrument?.AddTime(ticks);
                }
            }

            public IEnumerable<InstrumentState> Instruments => _instruments;

            public override string ToString() => string.Join(", ", Instruments.Where(x => x.Ticks > 0));
        }

        private static void TryGuessLabelsFromVgm(List<Channel> channels, string vgmFile)
        {
            var file = new VgmFile(vgmFile);

            var channelStates = new Dictionary<int, ChannelState>();
            ChannelState GetChannelState(int channelIndex)
            {
                if (!channelStates.TryGetValue(channelIndex, out var channelState))
                {
                    channelState = new ChannelState();
                    channelStates.Add(channelIndex, channelState);
                }

                return channelState;
            }

            foreach (var command in file.Commands())
            {
                switch (command)
                {
                    case VgmFile.WaitCommand waitCommand:
                        // Wait
                        foreach (var channelState in channelStates.Values)
                        {
                            channelState.AddTime(waitCommand.Ticks);
                        }
                        break;
                    case VgmFile.AddressDataCommand c:
                    {
                        if (c.Address is >= 0x30 and <= 0x38)
                        {
                            // YM2413 instrument
                            GetChannelState(c.Address & 0xf).SetInstrument(c.Data >> 4);
                        }
                        else if (c.Address is >= 0x20 and <= 0x28)
                        {
                            // YM2413 key down
                            var channelState = GetChannelState(c.Address & 0xf);
                            channelState.KeyDown = (c.Data & 0b00010000) != 0;
                        }
                        break;
                    }
                }
            }

            foreach (var kvp in channelStates.OrderBy(x => x.Key))
            {
                Console.WriteLine($"YM2413 channel {kvp.Key}: {kvp.Value}");
            }

            foreach (var channel in channels.Where(c => c.Label.StartsWith("YM2413 Tone ")))
            {
                var match = Regex.Match(channel.Label, "^YM2413 Tone (?<index>[0-9])$");
                if (!match.Success)
                {
                    continue;
                }
                var index = Convert.ToInt32(match.Groups["index"].Value) - 1;
                if (channelStates.TryGetValue(index, out var channelState))
                {
                    var instruments = channelState.Instruments
                        .Where(x => x.Ticks > 0)
                        .Select(x => x.Name);
                    channel.Label += ": " + string.Join("/\x200b", instruments);

                    Console.WriteLine($"Channel {index} is {channel.Label}");
                }
            }
        }

        private static Color ParseColor(string value)
        {
            // If it looks like hex, use that.
            // We support 3, 6 or 8 hex chars.
            var match = Regex.Match(value, "^#?(?<hex>[0-9a-fA-F]{3}([0-9a-fA-F]{3})?([0-9a-fA-F]{2})?)$");
            if (match.Success)
            {
                var hex = match.Groups["hex"].Value;
                if (hex.Length == 3)
                {
                    hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
                }

                if (hex.Length == 6)
                {
                    hex = "ff" + hex;
                }
                int alpha = Convert.ToInt32(hex.Substring(0, 2), 16);
                int red = Convert.ToInt32(hex.Substring(2, 2), 16);
                int green = Convert.ToInt32(hex.Substring(4, 2), 16);
                int blue = Convert.ToInt32(hex.Substring(6, 2), 16);
                return Color.FromArgb(alpha, red, green, blue);
            }
            // Then try named colors
            var property = typeof(Color)
                .GetProperties(BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public)
                .FirstOrDefault(p =>
                    p.PropertyType == typeof(Color) &&
                    p.Name.Equals(value, StringComparison.InvariantCultureIgnoreCase));
            return property == null 
                ? throw new Exception($"Could not parse color {value}") : 
                (Color)property.GetValue(null);
        }

        private static void RunMultiDumper(Settings settings)
        {
            if (!File.Exists(settings.MultidumperPath))
            {
                settings.MultidumperPath = FindExecutable("multidumper.exe");
            }

            if (settings.MultidumperPath == null && settings.VgmFile != null)
            {
                throw new Exception("Please pass --multidumper parameter to load a VGM file");
            }

            if (settings.VgmFile != null && settings.InputFiles.Any())
            {
                throw new Exception("Can't pass both --files and --vgm");
            }

            if (settings.VgmFile == null)
            {
                throw new Exception("VGM file not specified");
            }

            // We normalize the VGM path here because we need to know its directory...
            settings.VgmFile = Path.GetFullPath(settings.VgmFile);
            // Check if we have WAVs. Note that we use "natural" sorting to make sure 10 comes after 9.
            settings.InputFiles = Directory.EnumerateFiles(
                    Path.GetDirectoryName(settings.VgmFile) ?? throw new Exception($"Can't get path from VGM \"{settings.VgmFile}\""),
                    Path.GetFileNameWithoutExtension(settings.VgmFile) + " - *.wav")
                .ToList();
            if (!settings.InputFiles.Any())
            {
                Console.WriteLine("Running MultiDumper...");
                // Let's run it
                var wrapper = new MultiDumperWrapper(
                    settings.MultidumperPath, 
                    settings.MultidumperSamplingRate, 
                    settings.MultidumperLoopCount, 
                    settings.MultidumperFadeOutMs,
                    settings.MultidumperGapMs,
                    settings.MultidumperOptions);
                var song = wrapper.GetSongs(settings.VgmFile).First();
                var filenames = wrapper.Dump(song, d => Console.Write($"\r{d:P0}"));
                settings.InputFiles = filenames.ToList();
                Console.WriteLine($" done. {settings.InputFiles.Count()} files found.");
            }
            else
            {
                Console.WriteLine($"Skipping MultiDumper as {settings.InputFiles.Count()} files were already present.");
            }
        }

        private static void LogVerbose(Settings settings, string message)
        {
            if (!settings.Verbose)
            {
                return;
            }
            Console.WriteLine(message);
        }

        private static void Render(Settings settings, List<Channel> channels)
        {
            Console.WriteLine("Generating background image...");

            var backgroundImage = new BackgroundRenderer(settings.Width, settings.Height, ParseColor(settings.BackgroundColor));
            if (settings.BackgroundImageFile != null)
            {
                using var bm = Image.FromFile(settings.BackgroundImageFile);
                backgroundImage.Add(new ImageInfo(bm, ContentAlignment.MiddleCenter, true, DockStyle.None, 0.5f));
            }

            if (!string.IsNullOrEmpty(settings.LogoImageFile))
            {
                using var bm = Image.FromFile(settings.LogoImageFile);
                backgroundImage.Add(new ImageInfo(bm, ContentAlignment.BottomRight, false, DockStyle.None, 1));
            }

            if (settings.VgmFile != null)
            {
                var gd3 = Gd3Tag.LoadFromVgm(settings.VgmFile);
                var gd3Text = gd3.ToString();
                if (gd3Text.Length > 0)
                {
                    backgroundImage.Add(new TextInfo(gd3Text, settings.Gd3Font, settings.Gd3FontSize, ContentAlignment.BottomLeft, FontStyle.Regular,
                        DockStyle.Bottom, ParseColor(settings.Gd3FontColor)));
                }
            }

            if (settings.MaximumAspectRatio > 0.0)
            {
                Console.WriteLine($"Determining column count for maximum aspect ratio {settings.MaximumAspectRatio}:");
                for (var columns = 1; columns < 100; ++columns)
                {
                    var width = backgroundImage.WaveArea.Width / columns;
                    var rows = channels.Count / columns + (channels.Count % columns == 0 ? 0 : 1);
                    var height = backgroundImage.WaveArea.Height / rows;
                    var ratio = (double) width / height;
                    Console.WriteLine($"- {columns} columns => {width} x {height} pixels => ratio {ratio}");
                    if (ratio < settings.MaximumAspectRatio)
                    {
                        settings.Columns = columns;
                        break;
                    }
                }
            }

            // If we are doing only YM2413, we consider adding a blank channel after the tone channels
            if (channels.All(x => x.Label.Contains("YM2413") && channels.Count % settings.Columns != 0))
            {
                var numTone = channels.Count(x => x.Label.Contains(" Tone "));
                // We add enough to pad out the tones, or to right-fill the percussion, whichever is fewer.
                var numToAdd = Math.Min(numTone % settings.Columns, channels.Count % settings.Columns);
                // Add them
                if (numToAdd > 0)
                {
                    var emptyChannel = new Channel(false)
                    {
                        Filename = ""
                    };
                    emptyChannel.LoadDataAsync().Wait();
                    for (var i = 0; i < numToAdd; ++i)
                    {
                        channels.InsertRange(numTone, Enumerable.Repeat(emptyChannel, numToAdd));
                    }

                }
            }

            var renderer = new WaveformRenderer
            {
                BackgroundImage = backgroundImage.Image,
                Columns = settings.Columns,
                FramesPerSecond = settings.FramesPerSecond,
                Width = settings.Width,
                Height = settings.Height,
                SamplingRate = channels.First().SampleRate,
                RenderingBounds = backgroundImage.WaveArea
            };

            if (settings.GridLineWidth > 0)
            {
                foreach (var channel in channels)
                {
                    channel.BorderColor = ParseColor(settings.GridColor);
                    channel.BorderWidth = settings.GridLineWidth;
                    channel.BorderEdges = settings.GridBorder;
                }
            }

            // Add the data to the renderer
            foreach (var channel in channels)
            {
                renderer.AddChannel(channel);
            }

            var outputs = new List<IGraphicsOutput>();
            if (settings.OutputFile != null)
            {
                Console.WriteLine("Adding FFMPEG renderer...");
                if (!File.Exists(settings.FfMpegPath))
                {
                    // Try to find it
                    settings.FfMpegPath = FindExecutable("ffmpeg.exe");
                }
                outputs.Add(new FfmpegOutput(settings.FfMpegPath, settings.OutputFile, settings.Width, settings.Height, settings.FramesPerSecond, settings.FfMpegExtraOptions, settings.MasterAudioFile, settings.VideoCodec, settings.AudioCodec, false));
            }

            if (settings.PreviewFrameskip > 0)
            {
                Console.WriteLine("Adding preview renderer...");
                outputs.Add(new PreviewOutput(settings.PreviewFrameskip, true));
            }

            try
            {
                if (settings.ThreadCount < 1)
                {
                    settings.ThreadCount = Environment.ProcessorCount;
                    LogVerbose(settings, $"Defaulted thread count to {settings.ThreadCount}");
                }
                Console.WriteLine($"Rendering on {settings.ThreadCount} threads...");
                var sw = Stopwatch.StartNew();
                renderer.Render(outputs, settings.ThreadCount, settings.Verbose);
                sw.Stop();
                int numFrames = (int) (channels.Max(x => x.Length).TotalSeconds * settings.FramesPerSecond);
                Console.WriteLine($"Rendering complete in {sw.Elapsed:g}, average {numFrames / sw.Elapsed.TotalSeconds:N} fps");
            }
            catch (Exception ex)
            {
                // Should mean it was cancelled
                Console.WriteLine($"Rendering cancelled: {ex.Message}\n{ex}");
            }
            finally
            {
                foreach (var graphicsOutput in outputs)
                {
                    graphicsOutput.Dispose();
                }
            }
        }

        private static string FindExecutable(string name)
        {
            string fullPath;
            // Look in the current directory...
            if (File.Exists(name))
            {
                fullPath = Path.GetFullPath(name);
            }
            else
            {
                fullPath = Environment.GetEnvironmentVariable("PATH")
                    ?.Split(Path.PathSeparator)
                    .Select(path => Path.Combine(path, name))
                    .FirstOrDefault(File.Exists)
                    ?? throw new Exception($"Could not find path to {name}");
            }

            Console.WriteLine($"Found {name} at {fullPath}");
            return fullPath;
        }

        private static ITriggerAlgorithm CreateTriggerAlgorithm(string name)
        {
            var type = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t =>
                    typeof(ITriggerAlgorithm).IsAssignableFrom(t) &&
                    t.Name.ToLowerInvariant().Equals(name.ToLowerInvariant())) 
                ?? throw new Exception($"Unknown trigger algorithm \"{name}\"");
            return Activator.CreateInstance(type) as ITriggerAlgorithm;
        }

        private static async Task<string> UploadToYouTube(Settings settings)
        {
            var youtubeService = await GetYouTubeService(settings);

            var video = new Video
            {
                Snippet = new VideoSnippet
                {
                    Title = settings.YouTubeTitle,
                    CategoryId = "10" // Music
                },
                Status = new VideoStatus {PrivacyStatus = "public"}
                // or "private" or "public"
            };

            var tags = new List<string>();
            if (settings.YouTubeTags != null)
            {
                tags.AddRange(settings.YouTubeTags.Split(','));
            }

            Gd3Tag gd3 = null;
            if (settings.VgmFile != null)
            {
                gd3 = Gd3Tag.LoadFromVgm(settings.VgmFile);
            }

            if (gd3 != null)
            {
                video.Snippet.Description = $"Oscilloscope View of {gd3.Title}";
                if (gd3.Game.English.Length > 0)
                {
                    video.Snippet.Description += $" from the game {gd3.Game.English}";
                }
                if (gd3.System.English.Length > 0)
                {
                    video.Snippet.Description += $" for the {gd3.System.English}";
                }
                if (gd3.Composer.English.Length > 0)
                {
                    video.Snippet.Description += $", composed by {gd3.Composer}";
                }
                video.Snippet.Description += ".";
                if (gd3.Ripper.Length > 0)
                {
                    video.Snippet.Description += $"\nRipped by {gd3.Ripper}";
                }
                if (gd3.Notes.Length > 0)
                {
                    video.Snippet.Description += "\n\nNotes:\n" + gd3.Notes;
                }

            }

            if (settings.YouTubeDescriptionsExtra != null)
            {
                video.Snippet.Description += "\n" + settings.YouTubeDescriptionsExtra;
            }

            video.Snippet.Description += "\n\nVideo created using SidWizPlus - https://github.com/maxim-zhao/SidWizPlus";

            if (settings.YouTubeTagsFromGd3 && gd3 != null)
            {
                // We have a VGM file
                tags.Add(gd3.Game.English);
                tags.Add(gd3.System.English);
                tags.AddRange(gd3.Composer.English.Split(';'));
            }

            video.Snippet.Tags = tags.Distinct().Where(t => !string.IsNullOrEmpty(t)).Select(t => t.Trim()).ToList();

            if (settings.YouTubeCategory != null)
            {
                var request = youtubeService.VideoCategories.List("snippet");
                request.RegionCode = "US";
                var response = await request.ExecuteAsync();
                video.Snippet.CategoryId = response.Items
                    .Where(c => c.Snippet.Title.ToLowerInvariant().Contains(settings.YouTubeCategory.ToLowerInvariant()))
                    .Select(c => c.Id)
                    .FirstOrDefault();
                if (video.Snippet.CategoryId == null)
                {
                    await Console.Error.WriteLineAsync($"Warning: couldn't find category matching \"{settings.YouTubeCategory}\", defaulting to \"Music\"");
                }
            }

            if (gd3 != null)
            {
                video.Snippet.Title = FormatFromGd3(video.Snippet.Title, gd3);
            }

            if (video.Snippet.Title.Length > 100)
            {
                video.Snippet.Title = video.Snippet.Title.Substring(0, 97) + "...";
            }

            // We now escape some strings as the API doesn't do it internally...
            video.Snippet.Title = RemoveAngledBrackets(video.Snippet.Title);
            video.Snippet.Description = RemoveAngledBrackets(video.Snippet.Description);
            video.Snippet.Tags = video.Snippet.Tags.Select(RemoveAngledBrackets).ToList();

            await UploadVideo(settings.OutputFile, youtubeService, video);

            if (settings.YouTubePlaylist != null && !string.IsNullOrEmpty(video.Id))
            {
                if (gd3 != null)
                {
                    settings.YouTubePlaylist = RemoveAngledBrackets(FormatFromGd3(settings.YouTubePlaylist, gd3));
                }
                
                // We need to decide if it's an existing playlist

                // We iterate over all channels...
                var playlistsRequest = youtubeService.Playlists.List("snippet");
                playlistsRequest.Mine = true;
                var playlistsResponse = await playlistsRequest.ExecuteAsync();
                var playlist = playlistsResponse.Items.FirstOrDefault(p => p.Snippet.Title == settings.YouTubePlaylist);
                if (playlist == null)
                {
                    // Create it
                    playlist = new Playlist
                    {
                        Snippet = new PlaylistSnippet
                        {
                            Title = settings.YouTubePlaylist
                        },
                        Status = new PlaylistStatus
                        {
                            PrivacyStatus = "public"
                        }
                    };
                    if (settings.YouTubePlaylistDescriptionFile != null)
                    {
                        playlist.Snippet.Description = RemoveAngledBrackets(File.ReadAllText(settings.YouTubePlaylistDescriptionFile));
                    }

                    if (settings.YouTubeDescriptionsExtra != null)
                    {
                        playlist.Snippet.Description += "\n\n" + settings.YouTubeDescriptionsExtra;
                    }

                    playlist = await youtubeService.Playlists.Insert(playlist, "snippet, status").ExecuteAsync();
                    Console.WriteLine($"Created playlist \"{settings.YouTubePlaylist}\" with ID {playlist.Id}");
                }

                // Add to it
                var newPlaylistItem = new PlaylistItem
                {
                    Snippet = new PlaylistItemSnippet
                    {
                        PlaylistId = playlist.Id,
                        ResourceId = new ResourceId {Kind = "youtube#video", VideoId = video.Id}
                    }
                };
                newPlaylistItem = await youtubeService.PlaylistItems.Insert(newPlaylistItem, "snippet").ExecuteAsync();
                Console.WriteLine($"Added video {video.Id} ({video.Snippet.Title}) to playlist {playlist.Id} ({playlist.Snippet.Title}) as item {newPlaylistItem.Id}");
            }

            return video.Id;
        }

        private static async Task UploadVideo(string filename, YouTubeService youtubeService, Video video)
        {
            using var fileStream = new FileStream(filename, FileMode.Open);
            var videosInsertRequest = youtubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");
            videosInsertRequest.ChunkSize = ResumableUpload.MinimumChunkSize;
            long totalSize = fileStream.Length;
            bool shouldRetry = true;
            var sw = Stopwatch.StartNew();
            videosInsertRequest.ProgressChanged += progress =>
            {
                switch (progress.Status)
                {
                    case UploadStatus.Uploading:
                    {
                        var elapsedSeconds = sw.Elapsed.TotalSeconds;
                        var fractionComplete = (double)progress.BytesSent / totalSize;
                        var eta = TimeSpan.FromSeconds(elapsedSeconds / fractionComplete - elapsedSeconds);
                        var sent = (double)progress.BytesSent / 1024 / 1024;
                        var kbPerSecond = progress.BytesSent / sw.Elapsed.TotalSeconds / 1024;
                        Console.Write(
                            $"\r{fractionComplete:P0} {sent:f}MB sent, average {kbPerSecond:f}KB/s, ETA {eta:g}");
                        break;
                    }
                    case UploadStatus.Failed:
                        Console.Error.WriteLine($"Upload failed: {progress.Exception}");
                        // Google API says we can retry if we get a non-API error, or one of these four 5xx error codes
                        shouldRetry = progress.Exception is not GoogleApiException errorCode
                                      || new[]
                                      {
                                          HttpStatusCode.InternalServerError, HttpStatusCode.BadGateway,
                                          HttpStatusCode.ServiceUnavailable, HttpStatusCode.GatewayTimeout
                                      }.Contains(errorCode.HttpStatusCode);
                        if (shouldRetry)
                        {
                            Console.WriteLine("Retrying...");
                        }

                        break;
                    case UploadStatus.Completed:
                        Console.WriteLine($"Progress: {progress.Status}");
                        shouldRetry = false;
                        break;
                    default:
                        Console.WriteLine($"Progress: {progress.Status}");
                        break;
                }
            };
            videosInsertRequest.ResponseReceived += video1 =>
            {
                video.Id = video1.Id;
                Console.WriteLine($"\nUpload completed: video ID is {video1.Id}");
            };

            try
            {
                await videosInsertRequest.UploadAsync();
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Upload failed: {ex}");
            }

            while (shouldRetry)
            {
                try
                {
                    await videosInsertRequest.ResumeAsync();
                }
                catch (Exception ex)
                {
                    await Console.Error.WriteLineAsync($"Upload failed: {ex}");
                }
            }
        }

        private static async Task<YouTubeService> GetYouTubeService(Settings settings)
        {
            if (settings.YouTubeUploadClientSecret == null)
            {
                throw new Exception("No YouTube client secret provided");
            }

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    (await GoogleClientSecrets.FromFileAsync(settings.YouTubeUploadClientSecret)).Secrets,
                    // This OAuth 2.0 access scope allows an application to upload files to the
                    // authenticated user's YouTube channel, but doesn't allow other types of access.
                    [YouTubeService.Scope.YoutubeUpload, YouTubeService.Scope.YoutubeForceSsl],
                    "SidWizPlus",
                    CancellationToken.None
                );

                var youtubeService = new YouTubeService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = Assembly.GetExecutingAssembly().GetName().Name,
                    GZipEnabled = true
                });
                return youtubeService;
        }

        private static async Task UploadMergedToYouTube(Settings settings)
        {
            if (!File.Exists(settings.FfMpegPath))
            {
                settings.FfMpegPath = FindExecutable("ffmpeg.exe");
            }

            // First we look for the videos and collect some metadata
            var outputPath = Path.GetFullPath(settings.OutputFile);
            var directoryName = Path.GetDirectoryName(settings.YouTubeMerge);
            if (string.IsNullOrEmpty(directoryName))
            {
                directoryName = ".";
            }
            var files = Directory.EnumerateFiles(
                    directoryName,
                    Path.GetFileName(settings.YouTubeMerge))
                .AsParallel()
                .Select(Path.GetFullPath)
                .Where(path => path != outputPath)
                .Select(path => new
                {
                    Path = path,
                    Length = GetVideoDuration(path, settings),
                    Gd3 = GetGd3(path)
                })
                .OrderBy(x => x.Path)
                .ToList();

            foreach (var file in files)
            {
                Console.WriteLine($"{file.Path} is {file.Length}, tag is {file.Gd3?.ToString() ?? "<unknown>"}");
            }

            var mergedGd3 = MergeGd3Tags(files.Select(x => x.Gd3).Where(x => x != null).ToList());

            // Next we start to build the description with "chapter markers"
            var description = new StringBuilder()
                .AppendLine(
                    $"Oscilloscope View of music from the game {mergedGd3.Game} for the {mergedGd3.System}.");
            if (mergedGd3.Composer.English.Length > 0)
            {
                description.AppendLine($"Composed by {mergedGd3.Composer}.");
            }

            description.AppendLine($"Ripped by {mergedGd3.Ripper}");

            description.AppendLine("\nPlaylist:");
            var position = TimeSpan.Zero;
            foreach (var file in files)
            {
                description.AppendLine($"{position:hh':'mm':'ss} {file.Gd3?.Title.ToString() ?? Path.GetFileNameWithoutExtension(file.Path)}");
                position += file.Length;
            }

            if (settings.YouTubeDescriptionsExtra != null)
            {
                description.AppendLine($"\n{settings.YouTubeDescriptionsExtra}");
            }

            description.AppendLine("\nVideo created using SidWizPlus - https://github.com/maxim-zhao/SidWizPlus");

            Console.WriteLine("Video description:");
            Console.WriteLine(description);

            // Now we merge the files...
            // We need to write them to a list file for FFMPEG
            var listFile = Path.GetTempFileName();
            File.WriteAllLines(listFile, files.Select(f => $"file '{f.Path.Replace("'", "'\\''")}'"));
            using (var wrapper = new ProcessWrapper(
                settings.FfMpegPath,
                $"-hide_banner -y -f concat -safe 0 -i \"{listFile}\" -c copy {settings.FfMpegExtraOptions} \"{settings.OutputFile}\"",
                false,
                false,
                true))
            {
                wrapper.WaitForExit();
            }
            File.Delete(listFile);

            // Now we start the YouTube work...

            var youtubeService = await GetYouTubeService(settings);

            var video = new Video
            {
                Snippet = new VideoSnippet
                {
                    Title = FormatFromGd3(settings.YouTubeTitle, mergedGd3).TrimEnd(' ', '-'),
                    CategoryId = "10", // Music
                    Description = description.ToString()
                },
                Status = new VideoStatus {PrivacyStatus = "public"}
            };

            if (settings.YouTubeCategory != null)
            {
                bool retry = false;
                int tryCount = 0;
                do
                {
                    try
                    {
                        var request = youtubeService.VideoCategories.List("snippet");
                        request.RegionCode = "US";
                        ++tryCount;
                        var response = await request.ExecuteAsync();
                        video.Snippet.CategoryId = response.Items
                            .Where(c => c.Snippet.Title.ToLowerInvariant()
                                .Contains(settings.YouTubeCategory.ToLowerInvariant()))
                            .Select(c => c.Id)
                            .FirstOrDefault();
                        if (video.Snippet.CategoryId == null)
                        {
                            await Console.Error.WriteLineAsync(
                                $"Warning: couldn't find category matching \"{settings.YouTubeCategory}\", defaulting to \"Music\"");
                        }
                    }
                    catch (AggregateException ex)
                    {
                        retry = ex.InnerExceptions.OfType<TokenResponseException>().Any() && tryCount < 10;
                        await Console.Error.WriteLineAsync($"Exception talking to YouTube; retry = {retry}, tryCount = {tryCount}");
                    }
                } while (retry);
            }

            if (video.Snippet.Title.Length > 100)
            {
                video.Snippet.Title = video.Snippet.Title.Substring(0, 97) + "...";
            }

            if (video.Snippet.Description.Length > 4500)
            {
                video.Snippet.Description = video.Snippet.Description.Substring(0, 4450) + "...\nDescription truncated to fit in YouTube limits";
            }

            // We now escape some strings as the API doesn't do it internally...
            video.Snippet.Title = RemoveAngledBrackets(video.Snippet.Title);
            video.Snippet.Description = RemoveAngledBrackets(video.Snippet.Description);

            await UploadVideo(settings.OutputFile, youtubeService, video);
        }

        private static Gd3Tag MergeGd3Tags(IList<Gd3Tag> tags)
        {
            return new Gd3Tag
            {
                System = new Gd3Tag.MultiLanguageTag
                {
                    English = MergeTags(tags, t => t.System.English),
                    Japanese = MergeTags(tags, t => t.System.Japanese)
                },
                Game = new Gd3Tag.MultiLanguageTag
                {
                    English = MergeTags(tags, t => t.Game.English),
                    Japanese = MergeTags(tags, t => t.Game.Japanese)
                },
                Title = new Gd3Tag.MultiLanguageTag
                {
                    English = MergeTags(tags, t => t.Title.English),
                    Japanese = MergeTags(tags, t => t.Title.Japanese)
                },
                Composer = new Gd3Tag.MultiLanguageTag
                {
                    English = MergeTags(tags, t => t.Composer.English),
                    Japanese = MergeTags(tags, t => t.Composer.Japanese)
                },
                Ripper = MergeTags(tags, t => t.Ripper),
                Date = MergeTags(tags, t => t.Date)
            };
        }

        private static string MergeTags(IEnumerable<Gd3Tag> tags, Func<Gd3Tag, string> getter)
        {
            return string.Join("; ",
                tags.Select(getter)
                    .SelectMany(s => s.Split(';', ','))
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .Distinct()
                    .OrderBy(s => s));
        }

        private static Gd3Tag GetGd3(string path)
        {
            // We guess a file path for the VGM
            var vgmPath = Path.ChangeExtension(path, "vgm");
            if (!File.Exists(vgmPath))
            {
                return null;
            }

            return new VgmFile(vgmPath).Gd3Tag;
        }

        private static TimeSpan GetVideoDuration(string path, Settings settings)
        {
            // This is a bit of a hack...
            var re = new Regex(" +Duration: (?<duration>[0-9:.]+)");
            using var wrapper = new ProcessWrapper(
                settings.FfMpegPath,
                $"-i \"{path}\" -hide_banner",
                true);
            wrapper.WaitForExit();
            var lines = wrapper.Lines().ToList();
            var line = lines.FirstOrDefault(s => re.IsMatch(s)) 
                ?? throw new Exception($"Failed to find duration for {path}. FFMPEG output:\n{string.Join("\n", lines)}");
            return TimeSpan.Parse(re.Match(line).Groups["duration"].Value);
        }

        private static string RemoveAngledBrackets(string s)
        {
            return s.Replace("<", "").Replace(">", "");
        }

        private static string FormatFromGd3(string pattern, Gd3Tag gd3)
        {
            return pattern
                .Replace("[title]", gd3.Title.English)
                .Replace("[system]", gd3.System.English)
                .Replace("[game]", gd3.Game.English)
                .Replace("[composer]", gd3.Composer.English);
        }
    }
}

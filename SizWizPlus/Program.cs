using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CommandLine;
using CommandLine.Text;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using LibSidWiz;
using LibSidWiz.Outputs;
using LibSidWiz.Triggers;
using Channel = LibSidWiz.Channel;

namespace SidWizPlus
{
    class Program
    {
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        private class Settings
        {
            [OptionList('f', "files", Separator = ',', HelpText = "Input WAV files, comma-separated")] 
            public List<string> InputFiles { get; set; }

            [Option('v', "vgm", Required = false, HelpText = "VGM file, if specified GD3 text is drawn")]
            public string VgmFile { get; set; }

            [Option('m', "master", Required = false, HelpText = "Master audio file, if not specified then the inputs will be mixed to a new file")]
            public string MasterAudioFile { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("nomastermix", HelpText = "Disable automatic generation of master audio file (on by default)")]
            public bool NoMasterMix { get; set;}

            // ReSharper disable once StringLiteralTypo
            [Option("nomastermixreplaygain", HelpText = "Disable automatic ReplayGain adjustment of automatically generated master audio file (on by default)")]
            public bool NoMasterMixReplayGain { get; set;}

            [Option('o', "output", Required = false, HelpText = "Output file")]
            public string OutputFile { get; set; }

            [Option('w', "width", Required = false, HelpText = "Width of image rendered", DefaultValue = 1024)]
            public int Width { get; set; }

            [Option('h', "height", Required = false, HelpText = "Height of image rendered", DefaultValue = 720)]
            public int Height { get; set; }

            [Option('c', "columns", Required = false, HelpText = "Number of columns to render", DefaultValue = 1)]
            public int Columns { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("viewms", Required = false, HelpText = "Rendered view width in ms", DefaultValue = 35)]
            public int ViewWidthMs { get; set; }

            [Option('r', "fps", Required = false, HelpText = "Frame rate", DefaultValue = 60)]
            public int FramesPerSecond { get; set; }
            
            // ReSharper disable once StringLiteralTypo
            [Option("linewidth", Required = false, HelpText = "Line width", DefaultValue = 3)]
            public float LineWidth { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("linecolor", Required = false, HelpText = "Line color, can be hex or a .net color name", DefaultValue = "white")]
            public string LineColor { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("fillcolor", Required = false, HelpText = "Line color, can be hex or a .net color name", DefaultValue = "transparent")]
            public string FillColor { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("highpassfilter", Required = false, HelpText = "Enable high pass filtering with the given value as the cutoff frequency. A value of 10 works well to remove DC offsets.")]
            public float HighPassFilterFrequency { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option('a', "autoscale", Required = false, HelpText = "Automatic scaling percentage. A value of 100 will make the peak amplitude just fit in the rendered area.")]
            public float AutoScalePercentage { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option('t', "triggeralgorithm", Required = false, HelpText = "Trigger algorithm name", DefaultValue = nameof(PeakSpeedTrigger))]
            public string TriggerAlgorithm { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("triggerlookahead", Required = false, HelpText = "Number of frames to allow the trigger to look ahead, zero means no lookahead", DefaultValue = 0)]
            public int TriggerLookahead { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option('p', "previewframeskip", Required = false, HelpText = "Enable a preview window with the specified frameskip - higher values give faster rendering by not drawing every frame to the screen.")]
            public int PreviewFrameskip { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("ffmpeg", Required = false, HelpText = "Path to FFMPEG. If not given, no output is produced.")]
            public string FfMpegPath { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("ffmpegoptions", Required = false, HelpText = "Extra commandline options for FFMPEG, e.g. to set the output format", DefaultValue = "")]
            public string FfMpegExtraOptions { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("multidumper", Required = false, HelpText = "Path to MultiDumper, if specified with --vgm and no --files then it will be invoked for the VGM")]
            // ReSharper disable once IdentifierTypo
            public string MultidumperPath { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("backgroundcolor", Required = false, HelpText = "Background color, can be hex or a .net color name", DefaultValue = "black")]
            public string BackgroundColor { get; set; }

            [Option("background", Required = false, HelpText = "Background image, drawn transparently in the background")]
            public string BackgroundImageFile { get; set; }
            
            [Option("logo", Required = false, HelpText = "Logo image, drawn in the lower right")]
            public string LogoImageFile { get; set; }
            
            // ReSharper disable once StringLiteralTypo
            [Option("gridcolor", Required = false, HelpText = "Grid color, can be hex or a .net color name", DefaultValue = "white")]
            public string GridColor { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("gridwidth", Required = false, HelpText = "Grid line width", DefaultValue = 0)]
            public float GridLineWidth { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("gridborder", HelpText = "Draw a border around the waves as well as between them")]
            public bool GridBorder { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("zerolinecolor", HelpText = "Zero line color", DefaultValue = "white")]
            public string ZeroLineColor { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("zerolinewith", HelpText = "Zero line width", DefaultValue = 0)]
            public float ZeroLineWidth { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("gd3font", HelpText = "Font for GD3 info", DefaultValue = "Tahoma")]
            public string Gd3Font { get; set; }
            [Option("gd3size", HelpText = "Font size (in points) for GD3 info", DefaultValue = 16)]
            public float Gd3FontSize { get; set; }
            [Option("gd3color", HelpText = "Font color for GD3 info", DefaultValue = "white")]
            public string Gd3FontColor { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("labelsfont", HelpText = "Font for channel labels")]
            public string ChannelLabelsFont { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("labelssize", HelpText = "Font size for channel labels", DefaultValue = 8)]
            public float ChannelLabelsSize { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("labelscolor", HelpText = "Font color for channel labels", DefaultValue = "white")]
            public string ChannelLabelsColor { get; set; }

            // ReSharper disable once StringLiteralTypo
            [Option("youtubesecret", HelpText = "YouTube client secret JSON file")]
            public string YouTubeUploadClientSecret { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("youtubetitle", HelpText = "YouTube video title. If a VGM is specified then you can reference GD3 tags like [title], [system], [game], [composer]")]
            public string YouTubeTitle { get; set; }
            // ReSharper disable once StringLiteralTypo
            [Option("youtubecategory", HelpText = "YouTube video category", DefaultValue = "Gaming")]
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

            [HelpOption]
            public string GetUsage()
            {
                var help = new HelpText {
                    Heading = new HeadingInfo("SidWizPlus", "0.5"),
                    Copyright = new CopyrightInfo("Maxim", 2018),
                    AdditionalNewLineAfterOption = false,
                    AddDashesToOption = true,
                    MaximumDisplayWidth = Console.WindowWidth
                };
                help.AddPreOptionsLine("Licensed under MIT License");
                help.AddOptions(this);
                return help;
            }
        }

        static void Main(string[] args)
        {
            try
            {
                var settings = new Settings();
                // ReSharper disable once RedundantNameQualifier
                using (var parser = new CommandLine.Parser(x =>
                {
                    x.CaseSensitive = false;
                    x.IgnoreUnknownArguments = true;
                }))
                {
                    if (!parser.ParseArguments(args, settings))
                    {
                        Console.Error.WriteLine(settings.GetUsage());
                        return;
                    }
                }

                if (!settings.YouTubeOnly)
                {
                    if (settings.InputFiles == null)
                    {
                        RunMultiDumper(ref settings);
                    }
                    else
                    {
                        // We want to expand any wildcards in the input file list (and also fully qualify them)
                        settings.InputFiles = settings.InputFiles
                            .SelectMany(s => Directory
                                .EnumerateFiles(Directory.GetCurrentDirectory(), s)
                                .OrderByAlphaNumeric(x => x))
                            .ToList();
                    }

                    if (settings.InputFiles == null || !settings.InputFiles.Any())
                    {
                        Console.Error.WriteLine(settings.GetUsage());
                        throw new Exception("No inputs specified");
                    }

                    var channels = settings.InputFiles
                        .AsParallel()
                        .Select(filename =>
                    {
                        var channel = new Channel
                        {
                            Filename = filename,
                            HighPassFilterFrequency = settings.HighPassFilterFrequency,
                            LineColor = ParseColor(settings.LineColor),
                            LineWidth = settings.LineWidth,
                            FillColor = ParseColor(settings.FillColor),
                            Name = Channel.GuessNameFromMultidumperFilename(filename),
                            Algorithm = CreateTriggerAlgorithm(settings.TriggerAlgorithm),
                            TriggerLookaheadFrames = settings.TriggerLookahead,
                            ZeroLineWidth = settings.ZeroLineWidth,
                            ZeroLineColor = ParseColor(settings.ZeroLineColor),
                        };
                        channel.LoadDataAsync().Wait();
                        channel.ViewWidthInMilliseconds = settings.ViewWidthMs;
                        return channel;
                    }).Where(ch => ch.SampleCount > 0).ToList();
                    if (settings.AutoScalePercentage > 0)
                    {
                        var scale = settings.AutoScalePercentage / 100 / channels.Max(ch => ch.Max);
                        foreach (var channel in channels)
                        {
                            channel.Scale = scale;
                        }
                    }
                        
                    if (settings.OutputFile != null)
                    {
                        // Emit normalized data to a WAV file for later mixing
                        if (settings.MasterAudioFile == null && !settings.NoMasterMix)
                        {
                            settings.MasterAudioFile = settings.OutputFile + ".wav";
                            Mixer.MixToFile(channels, settings.MasterAudioFile, !settings.NoMasterMixReplayGain);
                        }
                    }

                    Render(settings, channels);
                }

                if (settings.YouTubeUploadClientSecret != null)
                {
                    var task = UploadToYouTube(settings);
                    task.Wait();
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Fatal: {e}");
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
            if (property == null)
            {
                throw new Exception($"Could not parse color {value}");
            }

            return (Color)property.GetValue(null);
        }

        private static void RunMultiDumper(ref Settings settings)
        {
            if (settings.MultidumperPath == null || settings.VgmFile == null || settings.InputFiles != null)
            {
                return;
            }
            // We normalize the VGM path here because we need to know its directory...
            settings.VgmFile = Path.GetFullPath(settings.VgmFile);
            // Check if we have WAVs. Note that we use "natural" sorting to make sure 10 comes after 9.
            settings.InputFiles = Directory.EnumerateFiles(
                    Path.GetDirectoryName(settings.VgmFile) ?? throw new Exception($"Can't get path from VGM \"{settings.VgmFile}\""),
                    Path.GetFileNameWithoutExtension(settings.VgmFile) + " - *.wav")
                .OrderByAlphaNumeric(s => s)
                .ToList();
            if (!settings.InputFiles.Any())
            {
                Console.Write("Running MultiDumper...");
                // Let's run it
                using (var p = Process.Start(new ProcessStartInfo
                {
                    FileName = settings.MultidumperPath,
                    Arguments = $"\"{settings.VgmFile}\" 0",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                }))
                {
                    // We don't actually consume its stdout, we just want to have it not shown as it makes it much slower...
                    if (p != null)
                    {
                        p.BeginOutputReadLine();
                        p.WaitForExit();
                    }
                }
                // And try again
                settings.InputFiles = Directory.EnumerateFiles(
                        Path.GetDirectoryName(settings.VgmFile) ?? throw new Exception($"Can't get path from VGM \"{settings.VgmFile}\""),
                        Path.GetFileNameWithoutExtension(settings.VgmFile) + " - *.wav")
                    .OrderByAlphaNumeric(s => s)
                    .ToList();
                Console.WriteLine($" done. {settings.InputFiles.Count} files found.");
            }
            else
            {
                Console.WriteLine($"Skipping MultiDumper as {settings.InputFiles.Count} files were already present.");
            }
        }

        private static void Render(Settings settings, IReadOnlyCollection<Channel> channels)
        {
            Console.WriteLine("Generating background image...");

            var backgroundImage = new BackgroundRenderer(settings.Width, settings.Height, ParseColor(settings.BackgroundColor));
            if (settings.BackgroundImageFile != null)
            {
                using (var bm = Image.FromFile(settings.BackgroundImageFile))
                {
                    backgroundImage.Add(new ImageInfo(bm, ContentAlignment.MiddleCenter, true, DockStyle.None, 0.5f));
                }
            }

            if (settings.LogoImageFile != null)
            {
                using (var bm = Image.FromFile(settings.LogoImageFile))
                {
                    backgroundImage.Add(new ImageInfo(bm, ContentAlignment.BottomRight, false, DockStyle.None, 1));
                }
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
                renderer.Grid = new WaveformRenderer.GridConfig
                {
                    Color = ParseColor(settings.GridColor),
                    Width = settings.GridLineWidth,
                    DrawBorder = settings.GridBorder
                };
            }

            // Add the data to the renderer
            foreach (var channel in channels)
            {
                renderer.AddChannel(channel);
            }

            if (settings.ChannelLabelsFont != null)
            {
                renderer.ChannelLabels = new WaveformRenderer.LabelConfig
                {
                    Color = ParseColor(settings.ChannelLabelsColor),
                    FontName = settings.ChannelLabelsFont,
                    Size = settings.ChannelLabelsSize
                };
            }

            var outputs = new List<IGraphicsOutput>();
            if (settings.FfMpegPath != null)
            {
                Console.WriteLine("Adding FFMPEG renderer...");
                outputs.Add(new FfmpegOutput(settings.FfMpegPath, settings.OutputFile, settings.Width, settings.Height, settings.FramesPerSecond, settings.FfMpegExtraOptions, settings.MasterAudioFile));
            }

            if (settings.PreviewFrameskip > 0)
            {
                Console.WriteLine("Adding preview renderer...");
                outputs.Add(new PreviewOutput(settings.PreviewFrameskip));
            }

            try
            {
                Console.WriteLine("Rendering...");
                var sw = Stopwatch.StartNew();
                renderer.Render(outputs);
                sw.Stop();
                int numFrames = (int) (channels.Max(x => x.Length).TotalSeconds * settings.FramesPerSecond);
                Console.WriteLine($"Rendering complete in {sw.Elapsed:g}, average {numFrames / sw.Elapsed.TotalSeconds:N} fps");
            }
            catch (Exception ex)
            {
                // Should mean it was cancelled
                Console.WriteLine($"Rendering cancelled: {ex.Message}");
            }
            finally
            {
                foreach (var graphicsOutput in outputs)
                {
                    graphicsOutput.Dispose();
                }
            }
        }

        private static ITriggerAlgorithm CreateTriggerAlgorithm(string name)
        {
            var type = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t =>
                    typeof(ITriggerAlgorithm).IsAssignableFrom(t) &&
                    t.Name.ToLowerInvariant().Equals(name.ToLowerInvariant()));
            if (type == null)
            {
                throw new Exception($"Unknown trigger algorithm \"{name}\"");
            }
            return Activator.CreateInstance(type) as ITriggerAlgorithm;
        }

        private static async Task<string> UploadToYouTube(Settings settings)
        {
            UserCredential credential;
            using (var stream = new FileStream(settings.YouTubeUploadClientSecret, FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    // This OAuth 2.0 access scope allows an application to upload files to the
                    // authenticated user's YouTube channel, but doesn't allow other types of access.
                    new[] { YouTubeService.Scope.YoutubeUpload, YouTubeService.Scope.YoutubeForceSsl },
                    "SidWizPlus",
                    CancellationToken.None
                );
            }

            var youtubeService = new YouTubeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = Assembly.GetExecutingAssembly().GetName().Name,
                GZipEnabled = true
            });

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
                tags.Add(gd3.Game.English);
                tags.Add(gd3.System.English);
                tags.AddRange(gd3.Composer.English.Split(';'));
            }

            video.Snippet.Tags = tags.Where(t => !string.IsNullOrEmpty(t)).Select(t => t.Trim()).ToList();

            if (settings.YouTubeCategory != null)
            {
                var request = youtubeService.VideoCategories.List("snippet");
                request.RegionCode = "US";
                var response = request.Execute();
                video.Snippet.CategoryId = response.Items
                    .Where(c => c.Snippet.Title.ToLowerInvariant().Contains(settings.YouTubeCategory.ToLowerInvariant()))
                    .Select(c => c.Id)
                    .FirstOrDefault();
                if (video.Snippet.CategoryId == null)
                {
                    Console.Error.WriteLine($"Warning: couldn't find category matching \"{settings.YouTubeCategory}\", defaulting to \"Music\"");
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

            using (var fileStream = new FileStream(settings.OutputFile, FileMode.Open))
            {
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
                            var fractionComplete = (double) progress.BytesSent / totalSize;
                            var eta = TimeSpan.FromSeconds(elapsedSeconds / fractionComplete - elapsedSeconds);
                            var sent = (double) progress.BytesSent / 1024 / 1024;
                            var kbPerSecond = progress.BytesSent / sw.Elapsed.TotalSeconds / 1024;
                            Console.Write($"\r{sent:f}MB sent ({fractionComplete:P}, average {kbPerSecond:f}KB/s, ETA {eta:g})");
                            break;
                        }
                        case UploadStatus.Failed:
                            Console.Error.WriteLine($"Upload failed: {progress.Exception}");
                            // Google API says we can retry if we get a non-API error, or one of these four 5xx error codes
                            shouldRetry = !(progress.Exception is GoogleApiException errorCode) 
                                || new[] {
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
                    videosInsertRequest.Upload();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Upload failed: {ex}");
                }

                while (shouldRetry)
                {
                    try
                    {
                        videosInsertRequest.Resume();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Upload failed: {ex}");
                    }
                }
            }

            if (settings.YouTubePlaylist != null && !string.IsNullOrEmpty(video.Id))
            {
                if (gd3 != null)
                {
                    settings.YouTubePlaylist = FormatFromGd3(settings.YouTubePlaylist, gd3);
                }
                
                // We need to decide if it's an existing playlist

                // We iterate over all channels...
                var playlistsRequest = youtubeService.Playlists.List("snippet");
                playlistsRequest.Mine = true;
                var playlistsResponse = playlistsRequest.Execute();
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
                        playlist.Snippet.Description = File.ReadAllText(settings.YouTubePlaylistDescriptionFile);
                    }

                    if (settings.YouTubeDescriptionsExtra != null)
                    {
                        playlist.Snippet.Description += "\n\n" + settings.YouTubeDescriptionsExtra;
                    }

                    playlist = youtubeService.Playlists.Insert(playlist, "snippet, status").Execute();
                    Console.WriteLine($"Created playlist \"{settings.YouTubePlaylist} with ID {playlist.Id}\"");
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CommandLine;
using CommandLine.Text;
using LibSidWiz;
using LibSidWiz.Outputs;
using LibSidWiz.Triggers;

namespace SidWizPlus
{
    class Program
    {
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        private class Settings
        {
            [OptionList('f', "file", ' ', HelpText = "Input WAV files")] 
            public List<string> InputFiles { get; set; }

            [Option('v', "vgm", Required = false, HelpText = "VGM file, if specified GD3 text is drawn")]
            public string VgmFile { get; set; }

            [Option('m', "master", Required = false, HelpText = "Master audio file, if not specified then the inputs will be mixed to a new file")]
            public string MasterAudioFile { get; set; }

            [Option('o', "output", Required = false, HelpText = "Output file")]
            public string OutputFile { get; set; }

            [Option('w', "width", Required = false, HelpText = "Width of image rendered", DefaultValue = 1024)]
            public int Width { get; set; }

            [Option('h', "height", Required = false, HelpText = "Height of image rendered", DefaultValue = 720)]
            public int Height { get; set; }

            [Option('c', "columns", Required = false, HelpText = "Number of columns to render", DefaultValue = 1)]
            public int Columns { get; set; }

            [Option("viewms", Required = false, HelpText = "Rendered view width in ms", DefaultValue = 35)]
            public int ViewWidthMs { get; set; }

            [Option('r', "fps", Required = false, HelpText = "Frame rate", DefaultValue = 60)]
            public int FramesPerSecond { get; set; }

            [Option('l', "linewidth", Required = false, HelpText = "Line width", DefaultValue = 3)]
            public float LineWidth { get; set; }

            [Option("highpassfilter", Required = false, HelpText = "Enable high pass filtering with the given value as the cutoff frequency. A value of 10 works well to remove DC offsets.")]
            public float HighPassFilterFrequency { get; set; }

            [Option('s', "scale", Required = false, HelpText = "Vertical scale factor. This is applied as a multiplier after auto scaling.")]
            public float VerticalScaleMultiplier { get; set; }

            [Option('a', "autoscale", Required = false, HelpText = "Automatic scaling percentage. A value of 100 will make the peak amplitude just fit in the rendered area.")]
            public float AutoScalePercentage { get; set; }

            [Option('t', "triggeralgorithm", Required = false, HelpText = "Trigger algorithm name", DefaultValue = nameof(PeakSpeedTrigger))]
            public string TriggerAlgorithm { get; set; }

            [Option('p', "previewframeskip", Required = false, HelpText = "Enable a preview window with the specified frameskip - higher values give faster rendering by not drawing every frame to the screen.")]
            public int PreviewFrameskip { get; set; }

            [Option("ffmpeg", Required = false, HelpText = "Path to FFMPEG. If not given, no output is produced.")]
            public string FfMpegPath { get; set; }
            [Option("ffmpegoptions", Required = false, HelpText = "Extra commandline options for FFMPEG, e.g. to set the output format", DefaultValue = "")]
            public string FfMpegExtraOptions { get; set; }

            [Option("multidumper", Required = false, HelpText = "Path to MultiDumper, if specified with --vgm and no --files then it will be invoked for the VGM")]
            public string MultidumperPath { get; set; }

            [Option("background", Required = false, HelpText = "Background image, drawn transparently in the background")]
            public string BackgroundImageFile { get; set; }
            
            [Option("logo", Required = false, HelpText = "Logo image, drawn in the lower right")]
            public string LogoImageFile { get; set; }
            
            [Option("gridcolor", Required = false, HelpText = "Grid color, can be hex or a .net color name", DefaultValue = "white")]
            public string GridColor { get; set; }
            [Option("gridwidth", Required = false, HelpText = "Grid line width", DefaultValue = 0)]
            public float GridLineWidth { get; set; }
            [Option("gridborder", HelpText = "Draw a border around the waves as well as between them")]
            public bool GridBorder { get; set; }

            [Option("zerolinecolor", HelpText = "Zero line color", DefaultValue = "white")]
            public string ZeroLineColor { get; set; }
            [Option("zerolinewith", HelpText = "Zero line width", DefaultValue = 0)]
            public float ZeroLineWidth { get; set; }

            [Option("gd3font", HelpText = "Font for GD3 info", DefaultValue = "Tahoma")]
            public string Gd3Font { get; set; }
            [Option("gd3fontsize", HelpText = "Font size (in points) for GD3 info", DefaultValue = 16)]
            public float Gd3FontSize { get; set; }
            [Option("gd3fontcolor", HelpText = "Font colour for GD3 info", DefaultValue = "white")]
            public string Gd3FontColor { get; set; }

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

                using (var loader = new AudioLoader
                {
                    AutoScalePercentage = settings.AutoScalePercentage,
                    HighPassFilterFrequency = settings.HighPassFilterFrequency,
                    VerticalScaleMultiplier = settings.VerticalScaleMultiplier,
                })
                {
                    loader.LoadAudio(settings.InputFiles);
                    Render(settings, loader);
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
                    Path.GetDirectoryName(settings.VgmFile),
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
                    // We don;t actually consume its stdout, we just want to have it not shown as it makes it much slower...
                    p.BeginOutputReadLine();
                    p.WaitForExit();
                }
                // And try again
                settings.InputFiles = Directory.EnumerateFiles(
                        Path.GetDirectoryName(settings.VgmFile),
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

        private static void Render(Settings settings, AudioLoader loader)
        {
            if (settings.OutputFile != null)
            {
                // Emit normalized data to a WAV file for later mixing
                if (settings.MasterAudioFile == null)
                {
                    settings.MasterAudioFile = settings.OutputFile + ".wav";
                    loader.MixToMaster(settings.MasterAudioFile);
                }
            }

            Console.WriteLine("Generating background image...");

            var backgroundImage = new BackgroundRenderer(settings.Width, settings.Height, Color.Black);
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
                SamplingRate = loader.SampleRate,
                RenderedLineWidthInSamples = settings.ViewWidthMs * loader.SampleRate / 1000,
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

            if (settings.ZeroLineWidth > 0)
            {
                renderer.ZeroLine = new WaveformRenderer.ZeroLineConfig
                {
                    Color = ParseColor(settings.ZeroLineColor),
                    Width = settings.ZeroLineWidth
                };
            }

            foreach (var channel in loader.Data)
            {
                renderer.AddChannel(new Channel(channel, Color.White, settings.LineWidth, "Hello world", CreateTriggerAlgorithm(settings.TriggerAlgorithm)));
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
                int numFrames = (int) (loader.Length.TotalSeconds * settings.FramesPerSecond);
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
    }
}

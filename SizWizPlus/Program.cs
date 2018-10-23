using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibSidWiz;
using LibSidWiz.Outputs;
using LibSidWiz.Triggers;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NReplayGain;

namespace SidWizPlus
{
    class Program
    {
        struct Settings
        {
            public Size Size;
            public int Columns;
            public TimeSpan ViewWidth;
            public int FramesPerSecond;
            public float LineWidth;

            public struct FfMpegSettings
            {
                public string Path;
                public string ExtraOptions;
            }

            public FfMpegSettings FfMpeg;

            public List<string> InputFiles;
            public string OutputFile;

            public string BackgroundImageFile;
            public string LogoImageFile;
            
            public string VgmFile;
            public string MultidumperPath;
            public int PreviewFrameskip;
            public float HighPassFilterFrequency;
            public float VerticalScaleMultiplier;
            public float AutoScale;
            public Type TriggerAlgorithm;
            public string MasterAudioFile;

            public struct GridSettings
            {
                public Color Color;
                public float LineWidth;
                public bool DrawBorder;
            }

            public GridSettings Grid;
            public struct ZeroLineSettings
            {
                public Color Color;
                public float LineWidth;
            }

            public ZeroLineSettings ZeroLine;
            public int SampleRate;
        }

        static void Main(string[] args)
        {
            try
            {
                // TODO add a better arg parsing lib here, giving things like help

                var settings = ParseArgs(args);

                RunMultiDumper(ref settings);

                var channelData = LoadAudio(ref settings);

                Render(settings, channelData);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Fatal: {e}");
            }
        }

        private static Settings ParseArgs(IReadOnlyList<string> args)
        {
            var settings = new Settings
            {
                // We default some settings...
                Size = new Size(1024, 720),
                AutoScale = 100,
                Columns = 1,
                FramesPerSecond = 60,
                LineWidth = 3,
                ViewWidth = TimeSpan.FromMilliseconds(35),
                TriggerAlgorithm = typeof(PeakSpeedTrigger)
            };

            // We have a dumb parser which just processes "--key value" pairs.
            for (int i = 0; i < args.Count - 1; i += 2)
            {
                var arg = args[i].ToLowerInvariant();
                var value = args[i + 1];
                switch (arg)
                {
                    case "--ffmpeg":
                        settings.FfMpeg.Path = value;
                        break;
                    case "--ffmpegoptions":
                        settings.FfMpeg.ExtraOptions = value;
                        break;
                    case "--columns":
                        settings.Columns = int.Parse(value);
                        break;
                    case "--viewms":
                        settings.ViewWidth = TimeSpan.FromMilliseconds(double.Parse(value));
                        break;
                    case "--fps":
                        settings.FramesPerSecond = int.Parse(value);
                        break;
                    case "--width":
                        settings.Size.Width = int.Parse(value);
                        break;
                    case "--height":
                        settings.Size.Height = int.Parse(value);
                        break;
                    case "--file":
                        // We support wildcards...
                        settings.InputFiles = Directory.EnumerateFiles(Directory.GetCurrentDirectory(), value).OrderBy(x => x)
                            .ToList();
                        break;
                    case "--output":
                        settings.OutputFile = value;
                        break;
                    case "--background":
                        settings.BackgroundImageFile = value;
                        break;
                    case "--logo":
                        settings.LogoImageFile = value;
                        break;
                    case "--vgm":
                        settings.VgmFile = Path.GetFullPath(value);
                        break;
                    case "--multidumper":
                        settings.MultidumperPath = value;
                        break;
                    case "--previewframeskip":
                        settings.PreviewFrameskip = int.Parse(value);
                        break;
                    case "--highpassfilter":
                        settings.HighPassFilterFrequency = float.Parse(value);
                        break;
                    case "--scale":
                        settings.VerticalScaleMultiplier = float.Parse(value);
                        break;
                    case "--triggeralgorithm":
                        settings.TriggerAlgorithm = AppDomain.CurrentDomain
                            .GetAssemblies()
                            .SelectMany(a => a.GetTypes())
                            .FirstOrDefault(t =>
                                typeof(ITriggerAlgorithm).IsAssignableFrom(t) &&
                                t.Name.ToLowerInvariant().Equals(value.ToLowerInvariant()));
                        break;
                    case "--autoscale":
                        settings.AutoScale = float.Parse(value) / 100;
                        break;
                    case "--masteraudio":
                        settings.MasterAudioFile = value;
                        break;
                    case "--gridcolor":
                        settings.Grid.Color = ParseColor(value);
                        break;
                    case "--gridwidth":
                        settings.Grid.LineWidth = float.Parse(value);
                        break;
                    case "--gridborder":
                        settings.Grid.DrawBorder = value == "1" || value.ToLowerInvariant().StartsWith("t");
                        break;
                    case "--zerolinecolor":
                        settings.ZeroLine.Color = ParseColor(value);
                        break;
                    case "--zerolinewidth":
                        settings.ZeroLine.LineWidth = float.Parse(value);
                        break;
                    case "--linewidth":
                        settings.LineWidth = float.Parse(value);
                        break;
                }
            }

            return settings;
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
            if (settings.MultidumperPath != null && settings.VgmFile != null && settings.InputFiles == null)
            {
                // Check if we have WAVs
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
        }

        private class ChannelData
        {
            public float[] Data { get; set; }
            public WaveFileReader WavReader { get; set; }
            public float Max { get; set; }
        }

        private static List<ChannelData> LoadAudio(ref Settings settings)
        {
            // TODO need to move this into the lib
            // TODO file load feedback for GUI?
            Console.WriteLine("Loading audio files...");
            using (var reader = new WaveFileReader(settings.InputFiles.First()))
            {
                settings.SampleRate = reader.WaveFormat.SampleRate;
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            int stepsPerFile = 3 + (settings.HighPassFilterFrequency > 0 ? 1 : 0);
            int totalProgress = settings.InputFiles.Count * stepsPerFile;
            int progress = 0;

            // We have to copy the reference to make it "safe" for threads
            var settings1 = settings;
            var loadTask = Task.Run(() =>
            {
                // Do a parallel read of all files
                var channels = settings1.InputFiles.AsParallel().Select((wavFilename, channelIndex) =>
                {
                    Console.WriteLine($"- Reading {wavFilename}");
                    var reader = new WaveFileReader(wavFilename);
                    var buffer = new float[reader.SampleCount];

                    // We read the file and convert to mono
                    reader.ToSampleProvider().ToMono().Read(buffer, 0, (int) reader.SampleCount);
                    Interlocked.Increment(ref progress);

                    // We don't care about ones where the samples are all equal
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (buffer.Length == 0 || buffer.All(s => s == buffer[0]))
                    {
                        Console.WriteLine($"- Skipping {wavFilename} because it is silent");
                        // So we skip steps here
                        reader.Dispose();
                        Interlocked.Add(ref progress, stepsPerFile - 1);
                        return null;
                    }

                    if (settings1.HighPassFilterFrequency > 0)
                    {
                        Console.WriteLine($"- High-pass filtering {wavFilename}");
                        // Apply the high pass filter
                        var filter = BiQuadFilter.HighPassFilter(reader.WaveFormat.SampleRate, settings1.HighPassFilterFrequency, 1);
                        for (int i = 0; i < buffer.Length; ++i)
                        {
                            buffer[i] = filter.Transform(buffer[i]);
                        }

                        Interlocked.Increment(ref progress);
                    }

                    float max = float.MinValue;
                    foreach (var sample in buffer)
                    {
                        max = Math.Max(max, Math.Abs(sample));
                    }

                    return new ChannelData{Data = buffer, WavReader = reader, Max = max};
                }).Where(ch => ch != null).ToList();

                if (settings1.AutoScale > 0 || settings1.VerticalScaleMultiplier > 1)
                {
                    // Calculate the multiplier
                    float multiplier = 1.0f;
                    if (settings1.AutoScale > 0)
                    {
                        multiplier = settings1.AutoScale / channels.Max(channel => channel.Max);
                    }

                    if (settings1.VerticalScaleMultiplier > 1)
                    {
                        multiplier *= settings1.VerticalScaleMultiplier;
                    }

                    // ...and we apply it
                    Console.WriteLine($"- Applying scaling (x{multiplier:N})...");
                    channels.AsParallel().Select(channel => channel.Data).ForAll(samples =>
                    {
                        for (int i = 0; i < samples.Length; ++i)
                        {
                            samples[i] *= multiplier;
                        }

                        Interlocked.Increment(ref progress);
                    });
                }

                return channels.ToList();
            });

            loadTask.Wait();

            return loadTask.Result;
        }

        private static void Render(Settings settings, IReadOnlyCollection<ChannelData> channelData)
        {
            var outputFile = Path.GetFullPath(settings.OutputFile);

            // Emit normalized data to a WAV file for later mixing
            if (settings.MasterAudioFile == null)
            {
                Console.WriteLine("Mixing per-channel data...");
                // Mix the audio. We should probably not be re-reading it here... should do this in one pass.
                foreach (var reader in channelData.Select(channel => channel.WavReader))
                {
                    reader.Position = 0;
                }
                var mixer = new MixingSampleProvider(channelData.Select(channel => channel.WavReader.ToSampleProvider()));
                var length = (int) channelData.Max(channel => channel.WavReader.SampleCount);
                var mixedData = new float[length * mixer.WaveFormat.Channels];
                mixer.Read(mixedData, 0, mixedData.Length);
                // Then we want to deinterleave it
                var leftChannel = new float[length];
                var rightChannel = new float[length];
                for (int i = 0; i < length; ++i)
                {
                    leftChannel[i] = mixedData[i * 2];
                    rightChannel[i] = mixedData[i * 2 + 1];
                }
                // Then Replay Gain it
                // The +3 is to make it at "YouTube loudness", which is a lot louder than ReplayGain defaults to.
                Console.WriteLine("Computing ReplayGain...");
                var replayGain = new TrackGain(settings.SampleRate);
                replayGain.AnalyzeSamples(leftChannel, rightChannel);
                var gain = replayGain.GetGain() + 3;
                float multiplier = (float)Math.Pow(10, gain / 20);
                // And apply it
                Console.WriteLine($"Applying ReplayGain ({gain:N} dB)...");
                for (int i = 0; i < mixedData.Length; ++i)
                {
                    mixedData[i] *= multiplier;
                }
                // Generate a temp filename
                settings.MasterAudioFile = outputFile + ".wav";
                Console.WriteLine($"Saving to {settings.MasterAudioFile}");
                WaveFileWriter.CreateWaveFile(
                    settings.MasterAudioFile, 
                    new FloatArraySampleProvider(mixedData, settings.SampleRate).ToWaveProvider());
            }

            Console.WriteLine("Generating background image...");

            var backgroundImage = new BackgroundRenderer(settings.Size, Color.Black);
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
                    backgroundImage.Add(new TextInfo(gd3Text, "Tahoma", 16, ContentAlignment.BottomLeft, FontStyle.Regular,
                        DockStyle.Bottom, Color.White));
                }
            }

            var renderer = new WaveformRenderer
            {
                BackgroundImage = backgroundImage.Image,
                Columns = settings.Columns,
                FramesPerSecond = settings.FramesPerSecond,
                Width = settings.Size.Width,
                Height = settings.Size.Height,
                SamplingRate = settings.SampleRate,
                RenderedLineWidthInSamples = (int) (settings.ViewWidth.TotalSeconds * settings.SampleRate),
                RenderingBounds = backgroundImage.WaveArea
            };

            if (settings.Grid.Color != Color.Empty && settings.Grid.LineWidth > 0)
            {
                renderer.Grid = new WaveformRenderer.GridConfig
                {
                    Color = settings.Grid.Color,
                    Width = settings.Grid.LineWidth,
                    DrawBorder = settings.Grid.DrawBorder
                };
            }

            if (settings.ZeroLine.Color != Color.Empty && settings.ZeroLine.LineWidth > 0)
            {
                renderer.ZeroLine = new WaveformRenderer.ZeroLineConfig
                {
                    Color = settings.ZeroLine.Color,
                    Width = settings.ZeroLine.LineWidth
                };
            }

            foreach (var channel in channelData)
            {
                renderer.AddChannel(new Channel(channel.Data, Color.White, settings.LineWidth, "Hello world", Activator.CreateInstance(settings.TriggerAlgorithm) as ITriggerAlgorithm));
            }

            var outputs = new List<IGraphicsOutput>();
            if (settings.FfMpeg.Path != null)
            {
                Console.WriteLine("Adding FFMPEG renderer...");
                outputs.Add(new FfmpegOutput(settings.FfMpeg.Path, outputFile, settings.Size.Width, settings.Size.Height, settings.FramesPerSecond, settings.FfMpeg.ExtraOptions, settings.MasterAudioFile));
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
                int numFrames = channelData.Max(x => x.Data.Length) * settings.FramesPerSecond / settings.SampleRate;
                Console.WriteLine($"Rendering complete in {sw.Elapsed}, average {numFrames / sw.Elapsed.TotalSeconds:N} fps");
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
    }
}

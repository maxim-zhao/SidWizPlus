using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NReplayGain;

namespace LibSidWiz
{
    public class AudioLoader
    {
        public int SampleRate { get; private set; }
        public float HighPassFilterFrequency { get; set; }
        public float VerticalScaleMultiplier { get; set; }
        public float AutoScalePercentage { get; set; }

        // TODO we don't really want to expose this?
        public class ChannelData
        {
            public float[] Data { get; set; }
            public WaveFileReader WavReader { get; set; }
            public float Max { get; set; }
        }

        public List<ChannelData> Data { get; private set; }
        public TimeSpan Length { get; private set; }

        public void LoadAudio(IList<string> filenames)
        {
            // TODO file load feedback for GUI?
            Console.WriteLine("Loading audio files...");
            using (var reader = new WaveFileReader(filenames.First()))
            {
                SampleRate = reader.WaveFormat.SampleRate;
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            // int stepsPerFile = 3 + (settings.HighPassFilterFrequency > 0 ? 1 : 0);
            // int totalProgress = settings.InputFiles.Count * stepsPerFile;
            // int progress = 0;

            // We have to copy the reference to make it "safe" for threads
            var loadTask = Task.Run(() =>
            {
                // Do a parallel read of all files
                var channels = filenames.AsParallel().Select((wavFilename, channelIndex) =>
                {
                    var filename = Path.GetFileName(wavFilename);
                    Console.WriteLine($"- Reading {filename}");
                    var reader = new WaveFileReader(wavFilename);
                    var buffer = new float[reader.SampleCount];

                    // We read the file and convert to mono
                    reader.ToSampleProvider().ToMono().Read(buffer, 0, (int) reader.SampleCount);
                    // Interlocked.Increment(ref progress);

                    // We don't care about ones where the samples are all equal
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (buffer.Length == 0 || buffer.All(s => s == buffer[0]))
                    {
                        Console.WriteLine($"- Skipping {filename} because it is silent");
                        // So we skip steps here
                        reader.Dispose();
                        // Interlocked.Add(ref progress, stepsPerFile - 1);
                        return null;
                    }

                    if (HighPassFilterFrequency > 0)
                    {
                        Console.WriteLine($"- High-pass filtering {filename}");
                        // Apply the high pass filter
                        var filter = BiQuadFilter.HighPassFilter(reader.WaveFormat.SampleRate, HighPassFilterFrequency, 1);
                        for (int i = 0; i < buffer.Length; ++i)
                        {
                            buffer[i] = filter.Transform(buffer[i]);
                        }

                        // Interlocked.Increment(ref progress);
                    }

                    float max = float.MinValue;
                    foreach (var sample in buffer)
                    {
                        max = Math.Max(max, Math.Abs(sample));
                    }

                    return new ChannelData{Data = buffer, WavReader = reader, Max = max};
                }).Where(ch => ch != null).ToList();

                if (AutoScalePercentage > 0 || VerticalScaleMultiplier > 1)
                {
                    // Calculate the multiplier
                    float multiplier = 1.0f;
                    if (AutoScalePercentage > 0)
                    {
                        multiplier = AutoScalePercentage / 100 / channels.Max(channel => channel.Max);
                    }

                    if (VerticalScaleMultiplier > 1)
                    {
                        multiplier *= VerticalScaleMultiplier;
                    }

                    // ...and we apply it
                    Console.WriteLine($"- Applying scaling (x{multiplier:N})...");
                    channels.AsParallel().Select(channel => channel.Data).ForAll(samples =>
                    {
                        for (int i = 0; i < samples.Length; ++i)
                        {
                            samples[i] *= multiplier;
                        }

                        // Interlocked.Increment(ref progress);
                    });
                }

                return channels.ToList();
            });

            loadTask.Wait();

            Data = loadTask.Result;
            Length = TimeSpan.FromSeconds((double)Data.Max(x => x.Data.Length) / SampleRate);
        }

        public void MixToMaster(string filename)
        {
                Console.WriteLine("Mixing per-channel data...");
                // Mix the audio. We should probably not be re-reading it here... should do this in one pass.
                foreach (var reader in Data.Select(channel => channel.WavReader))
                {
                    reader.Position = 0;
                }
                var mixer = new MixingSampleProvider(Data.Select(channel => channel.WavReader.ToSampleProvider()));
                var length = (int) Data.Max(channel => channel.WavReader.SampleCount);
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
                var replayGain = new TrackGain(SampleRate);
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
                Console.WriteLine($"Saving to {filename}");
                WaveFileWriter.CreateWaveFile(
                    filename, 
                    new FloatArraySampleProvider(mixedData, SampleRate).ToWaveProvider());
        }
    }
}

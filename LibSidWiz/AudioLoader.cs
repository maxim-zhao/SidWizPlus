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
    public class AudioLoader: IDisposable
    {
        public float HighPassFilterFrequency { get; set; }
        public float AutoScalePercentage { get; set; }
        public TimeSpan Length { get; private set; }
        public int SampleRate { get; private set; }

        public class Channel
        {
            public IList<float> Samples { get; set; }
            public string Filename { get; set; }
        }
        public IEnumerable<Channel> Data => _data.Select(channel => new Channel{Samples = channel.Data, Filename = channel.Filename});

        private class ChannelData
        {
            public float[] Data { get; set; }
            public WaveFileReader WavReader { get; set; }
            public float Max { get; set; }
            public string Filename { get; set; }
        }

        private List<ChannelData> _data;

        public void LoadAudio(IList<string> filenames)
        {
            // TODO file load feedback for GUI?
            Console.WriteLine("Loading audio files...");
            using (var reader = new WaveFileReader(filenames.First()))
            {
                SampleRate = reader.WaveFormat.SampleRate;
            }

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

                    // We don't care about ones where the samples are all equal
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (buffer.Length == 0 || buffer.All(s => s == buffer[0]))
                    {
                        Console.WriteLine($"- Skipping {filename} because it is silent");
                        // So we skip steps here
                        reader.Dispose();
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
                    }

                    float max = buffer.Select(Math.Abs).Max();

                    return new ChannelData{Data = buffer, WavReader = reader, Max = max, Filename = filename};
                }).Where(ch => ch != null).ToList();

                if (AutoScalePercentage > 0)
                {
                    // Calculate the multiplier
                    float multiplier = 1.0f;
                    if (AutoScalePercentage > 0)
                    {
                        multiplier = AutoScalePercentage / 100 / channels.Max(channel => channel.Max);
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

            _data = loadTask.Result;
            Length = TimeSpan.FromSeconds((double)_data.Max(x => x.Data.Length) / SampleRate);
        }

        public void MixToFile(string filename, bool applyReplayGain)
        {
            Console.WriteLine("Mixing per-channel data...");
            Console.WriteLine("Computing ReplayGain...");
            // Mix the audio. We should probably not be re-reading it here... we could do this in the same pass as loading.
            foreach (var reader in _data.Select(channel => channel.WavReader))
            {
                reader.Position = 0;
            }

            if (applyReplayGain)
            {
                // We read it in a second at a time, to calculate Replay Gains
                var mixer = new MixingSampleProvider(_data.Select(channel => channel.WavReader.ToSampleProvider().ToStereo()));
                var buffer = new float[mixer.WaveFormat.SampleRate * 2];
                var replayGain = new TrackGain(SampleRate);
                for (;;)
                {
                    int numRead = mixer.Read(buffer, 0, buffer.Length);
                    if (numRead == 0)
                    {
                        break;
                    }

                    // And analyze
                    replayGain.AnalyzeSamples(buffer, numRead);
                }

                // The +3 is to make it at "YouTube loudness", which is a lot louder than ReplayGain defaults to.
                var gain = replayGain.GetGain() + 3;

                Console.WriteLine($"Applying ReplayGain ({gain:N} dB) and saving to {filename}");

                // Reset the readers again
                foreach (var reader in _data.Select(channel => channel.WavReader))
                {
                    reader.Position = 0;
                }

                mixer = new MixingSampleProvider(_data.Select(channel => channel.WavReader.ToSampleProvider().ToStereo()));
                var amplifier = new VolumeSampleProvider(mixer) {Volume = (float) Math.Pow(10, gain / 20)};
                WaveFileWriter.CreateWaveFile(filename, amplifier.ToWaveProvider());
            }
            else
            {
                var mixer = new MixingSampleProvider(_data.Select(channel => channel.WavReader.ToSampleProvider().ToStereo()));
                WaveFileWriter.CreateWaveFile(filename, mixer.ToWaveProvider());
            }
        }

        public void Dispose()
        {
            foreach (var channelData in _data)
            {
                channelData.WavReader.Dispose();
                channelData.WavReader = null;
                channelData.Data = null;
            }
        }
    }
}

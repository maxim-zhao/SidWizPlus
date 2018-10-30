using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using LibSidWiz.Triggers;
using NAudio.Dsp;
using NAudio.Wave;

namespace LibSidWiz
{
    /// <summary>
    /// Wraps a single "voice", and also deals with loading the data into memory
    /// </summary>
    public class Channel
    {
        private IList<float> _samples;

        public void LoadData()
        {
            Console.WriteLine($"- Reading {Filename}");
            float[] buffer;
            using (var reader = new WaveFileReader(Filename))
            {
                SampleRate = reader.WaveFormat.SampleRate;
                Length = TimeSpan.FromSeconds((double) reader.SampleCount / reader.WaveFormat.SampleRate);

                // We read the file and convert to mono
                buffer = new float[reader.SampleCount];
                reader.ToSampleProvider().ToMono().Read(buffer, 0, (int) reader.SampleCount);
            }

            // We don't care about ones where the samples are all equal
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (buffer.Length == 0 || buffer.All(s => s == buffer[0]))
            {
                Console.WriteLine($"- {Filename} is silent");
                // So we skip steps here
                _samples = null;
            }

            if (HighPassFilterFrequency > 0)
            {
                Console.WriteLine($"- High-pass filtering {Filename}");
                // Apply the high pass filter
                var filter = BiQuadFilter.HighPassFilter(SampleRate, HighPassFilterFrequency, 1);
                for (int i = 0; i < buffer.Length; ++i)
                {
                    buffer[i] = filter.Transform(buffer[i]);
                }
            }

            Max = buffer.Select(Math.Abs).Max();
            Console.WriteLine($"- Peak sample amplitude for {Filename} is {Max}");

            _samples = buffer;
        }

        public ITriggerAlgorithm Algorithm { get; set; }
        public int TriggerLookaheadFrames { get; set; }

        public Color Color { get; set; } = Color.White;
        public string Name { get; set; } = "";
        public float LineWidth { get; set; } = 3;

        public float HighPassFilterFrequency { get; set; } = -1;

        public float Max { get; private set; }
        public float Scale { get; set; } = 1.0f;
        public int SampleCount => _samples?.Count ?? 0;
        public TimeSpan Length { get; private set; }
        public string Filename { get; set; }
        public int SampleRate { get; private set; }

        public float GetSample(int sampleIndex)
        {
            return sampleIndex < 0 || sampleIndex >= _samples.Count ? 0 : _samples[sampleIndex] * Scale;
        }

        public int GetTriggerPoint(int frameIndexSamples, int frameSamples)
        {
            return Algorithm.GetTriggerPoint(this, frameIndexSamples, frameIndexSamples + frameSamples * (TriggerLookaheadFrames + 1));
        }

        public static string GuessNameFromMultidumperFilename(string filename)
        {
            var namePart = Path.GetFileNameWithoutExtension(filename);
            try
            {
                if (namePart == null)
                {
                    return filename;
                }

                var index = namePart.IndexOf(" - YM2413 #", StringComparison.Ordinal);
                if (index > -1)
                {
                    index = int.Parse(namePart.Substring(index + 11));
                    if (index < 9)
                    {
                        return $"YM2413 tone {index + 1}";
                    }

                    switch (index)
                    {
                        case 9: return "YM2413 Bass Drum";
                        case 10: return "YM2413 Snare Drum";
                        case 11: return "YM2413 Tom-Tom";
                        case 12: return "YM2413 Cymbal";
                        case 13: return "YM2413 Hi-Hat";
                    }
                }

                index = namePart.IndexOf(" - SEGA PSG #", StringComparison.Ordinal);
                if (index > -1)
                {
                    index = Int32.Parse(namePart.Substring(index + 13));
                    if (index < 3)
                    {
                        return $"Sega PSG Square {index + 1}";
                    }

                    return "Sega PSG Noise";
                }

                // Guess it's the bit after the last " - "
                index = namePart.LastIndexOf(" - ", StringComparison.Ordinal);
                if (index > -1)
                {
                    return namePart.Substring(index + 3);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error guessing channel name for {filename}: {ex}");
            }

            // Default to just the filename
            return namePart;
        }
    }
}
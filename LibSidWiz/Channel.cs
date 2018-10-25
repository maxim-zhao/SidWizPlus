using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using LibSidWiz.Triggers;

namespace LibSidWiz
{
    /// <summary>
    /// Wraps a single "voice"
    /// </summary>
    public class Channel
    {
        private readonly ITriggerAlgorithm _algorithm;
        private readonly int _triggerLookahead;

        public Channel(IList<float> samples, Color color, float lineWidth, string name, ITriggerAlgorithm algorithm, int triggerLookahead)
        {
            Samples = samples;
            Color = color;
            Name = name;
            _algorithm = algorithm;
            _triggerLookahead = triggerLookahead;
            LineWidth = lineWidth;
        }

        public IList<float> Samples { get; }
        public Color Color { get; }
        public string Name { get; }
        public float LineWidth { get; }

        public float GetSample(int sampleIndex)
        {
            return sampleIndex < 0 || sampleIndex >= Samples.Count ? 0 : Samples[sampleIndex];
        }

        public int GetTriggerPoint(int frameIndexSamples, int frameSamples)
        {
            return _algorithm.GetTriggerPoint(this, frameIndexSamples, frameIndexSamples + frameSamples * (_triggerLookahead + 1));
        }

        [SuppressMessage("ReSharper", "StringIndexOfIsCultureSpecific.1")]
        public static string GuessNameFromMultidumperFilename(string filename)
        {
            var namePart = Path.GetFileNameWithoutExtension(filename);
            try
            {
                if (namePart == null)
                {
                    return filename;
                }

                var index = namePart.IndexOf(" - YM2413 #");
                if (index > -1)
                {
                    index = Int32.Parse(namePart.Substring(index + 11));
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

                index = namePart.IndexOf(" - SEGA PSG #");
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
                index = namePart.LastIndexOf(" - ");
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
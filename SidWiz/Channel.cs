using System.Collections.Generic;
using System.Drawing;
using SidWiz.Triggers;

namespace SidWiz
{
    /// <summary>
    /// Wraps a single "voice"
    /// </summary>
    internal class Channel
    {
        private readonly ITriggerAlgorithm _algorithm;

        public Channel(IList<float> samples, Color color, float lineWidth, string name, ITriggerAlgorithm algorithm)
        {
            Samples = samples;
            Color = color;
            Name = name;
            _algorithm = algorithm;
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
            return _algorithm.GetTriggerPoint(this, frameIndexSamples, frameSamples);
        }
    }
}
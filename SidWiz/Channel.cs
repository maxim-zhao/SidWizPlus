using System.Collections.Generic;
using System.Drawing;

namespace SidWiz
{
    /// <summary>
    /// Wraps a single "voice"
    /// </summary>
    internal class Channel
    {
        public Channel(IList<float> samples, Color color, float lineWidth, string name)
        {
            Samples = samples;
            Color = color;
            Name = name;
            LineWidth = lineWidth;
        }

        public IList<float> Samples { get; }
        public Color Color { get; }
        public string Name { get; }
        public float LineWidth { get; }
    }
}
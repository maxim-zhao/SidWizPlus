using System.Drawing;
using System.Windows.Forms;

namespace SidWizPlus
{
    internal class TextInfo
    {
        public string Text { get; }
        public string FontName { get; }
        public float FontSize { get; }
        public FontStyle FontStyle { get; }
        public ContentAlignment Alignment { get; }
        public DockStyle ConstrainWaves { get; }
        public Color Color { get; }

        public TextInfo(string text, string fontName, float fontSize, ContentAlignment alignment, FontStyle fontStyle, DockStyle constrainWaves, Color color)
        {
            Text = text;
            Alignment = alignment;
            ConstrainWaves = constrainWaves;
            Color = color;
            FontName = fontName;
            FontSize = fontSize;
            FontStyle = fontStyle;
        }
    }
}
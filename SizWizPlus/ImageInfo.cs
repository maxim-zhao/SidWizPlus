using System.Drawing;
using System.Windows.Forms;

namespace SidWizPlus
{
    internal class ImageInfo
    {
        public Image Image { get; }
        public ContentAlignment Alignment { get; }
        public bool StretchToFit { get; }
        public DockStyle ConstrainWaves { get; }
        public float Alpha { get; }

        public ImageInfo(Image image, ContentAlignment alignment, bool stretchToFit, DockStyle constrainWaves, float alpha)
        {
            Image = image;
            Alignment = alignment;
            StretchToFit = stretchToFit;
            ConstrainWaves = constrainWaves;
            Alpha = alpha;
        }
    }
}
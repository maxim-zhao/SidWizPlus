using System.Drawing;

namespace SidWiz
{
    internal interface IGraphicsOutput
    {
        void Write(byte[] data, Image image, double fractionComplete);
    }
}

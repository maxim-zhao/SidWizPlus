using System;
using System.Drawing;

namespace LibSidWiz.Outputs
{
    public interface IGraphicsOutput: IDisposable
    {
        void Write(Image image, byte[] data, double fractionComplete, TimeSpan length);
    }
}

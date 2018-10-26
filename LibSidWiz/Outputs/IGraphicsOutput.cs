using System;
using System.Drawing;

namespace LibSidWiz.Outputs
{
    public interface IGraphicsOutput: IDisposable
    {
        void Write(byte[] data, Image image, double fractionComplete);
    }
}

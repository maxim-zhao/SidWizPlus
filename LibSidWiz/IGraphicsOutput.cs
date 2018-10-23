using System;
using System.Drawing;

namespace LibSidWiz
{
    public interface IGraphicsOutput: IDisposable
    {
        void Write(byte[] data, Image image, double fractionComplete);
    }
}

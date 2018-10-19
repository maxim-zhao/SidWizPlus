using System;
using System.Drawing;

namespace SidWiz
{
    internal interface IGraphicsOutput: IDisposable
    {
        void Write(byte[] data, Image image, double fractionComplete);
    }
}

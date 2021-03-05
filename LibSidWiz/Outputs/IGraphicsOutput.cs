using System;
using System.Drawing;
using SkiaSharp;

namespace LibSidWiz.Outputs
{
    public interface IGraphicsOutput: IDisposable
    {
        void Write(byte[] data, Image image, double fractionComplete);
        void Write(byte[] data, SKImage image, double fractionComplete);
    }
}

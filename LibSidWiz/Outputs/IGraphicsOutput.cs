using System;
using SkiaSharp;

namespace LibSidWiz.Outputs
{
    public interface IGraphicsOutput: IDisposable
    {
        void Write(SKImage image, byte[] data, double fractionComplete, TimeSpan length);
    }
}

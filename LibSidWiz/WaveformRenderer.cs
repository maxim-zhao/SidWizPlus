using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using LibSidWiz.Outputs;

namespace LibSidWiz
{
    /// <summary>
    /// Class responsible for rendering
    /// </summary>
    public class WaveformRenderer
    {
        private readonly List<Channel> _channels = new List<Channel>();

        public int Width { get; set; }
        public int Height { get; set; }
        public int Columns { get; set; }
        public int SamplingRate { get; set; }
        public int FramesPerSecond { get; set; }
        public int RenderedLineWidthInSamples { get; set; }
        public Color BackgroundColor { get; set; } = Color.Black;
        public Image BackgroundImage { get; set; }
        public Rectangle RenderingBounds { get; set; }
        public GridConfig Grid { get; set; }
        public ZeroLineConfig ZeroLine { get; set; }
        public LabelConfig ChannelLabels { get; set; }

        public class LabelConfig
        {
            public Color Color { get; set; }
            public string FontName { get; set; }
            public float Size { get; set; }
        }

        public class GridConfig
        {
            public Color Color { get; set; }
            public float Width { get; set; }
            public bool DrawBorder { get; set; }
        }

        public void AddChannel(Channel channel)
        {
            _channels.Add(channel);
        }

        public void Render(IList<IGraphicsOutput> outputs)
        {
            int sampleLength = _channels.Max(c => c.SampleCount);

            var renderingBounds = RenderingBounds;
            if (renderingBounds.Width == 0 || renderingBounds.Height == 0)
            {
                // Default to no bounds
                renderingBounds = new Rectangle(0, 0, Width, Height);
            }

            int viewWidth = renderingBounds.Width / Columns;
            int viewHeight = renderingBounds.Height / (int)Math.Ceiling((double)_channels.Count / Columns);

            // We generate our "base image"
            var template = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(template))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                if (BackgroundImage != null)
                {
                    // Fill with the background image
                    g.DrawImage(BackgroundImage, 0, 0, Width, Height);
                }
                else
                {
                    // Fill background
                    using (var brush = new SolidBrush(BackgroundColor))
                    {
                        g.FillRectangle(brush, 0, 0, Width, Height);
                    }
                }

                if (Grid != null)
                {
                    using (var pen = new Pen(Grid.Color, Grid.Width))
                    {
                        // Verticals
                        for (int c = 1; c < Columns; ++c)
                        {
                            g.DrawLine(
                                pen, 
                                renderingBounds.Left + viewWidth * c, renderingBounds.Top, 
                                renderingBounds.Left + viewWidth * c, renderingBounds.Bottom);
                        }
                        // Horizontals
                        for (int r = 1; r < (float)_channels.Count / Columns; ++r)
                        {
                            g.DrawLine(
                                pen, 
                                renderingBounds.Left, renderingBounds.Top + viewHeight * r,
                                renderingBounds.Right, renderingBounds.Top + viewHeight * r);
                        }

                        if (Grid.DrawBorder)
                        {
                            g.DrawRectangle(pen, renderingBounds);
                        }
                    }
                }

                if (ZeroLine != null)
                {
                    using (var pen = new Pen(ZeroLine.Color, ZeroLine.Width))
                    {
                        for (int channelIndex = 0; channelIndex < _channels.Count; ++channelIndex)
                        {
                            // Compute the initial x, y to render the line from.
                            var yBase = renderingBounds.Top + channelIndex / Columns * viewHeight + viewHeight / 2;
                            var xBase = renderingBounds.Left + (channelIndex % Columns) * renderingBounds.Width / Columns ;

                            // Draw the zero line
                            g.DrawLine(pen, xBase, yBase, xBase + viewWidth, yBase);
                        }
                    }
                }

                if (ChannelLabels != null)
                {
                    using (var font = new Font(ChannelLabels.FontName, ChannelLabels.Size))
                    using (var brush = new SolidBrush(ChannelLabels.Color))
                    {
                        for (int channelIndex = 0; channelIndex < _channels.Count; ++channelIndex)
                        {
                            var y = renderingBounds.Top + channelIndex / Columns * viewHeight;
                            var x = renderingBounds.Left + (channelIndex % Columns) * renderingBounds.Width / Columns;
                            g.DrawString(_channels[channelIndex].Name, font, brush, x, y);
                        }
                    }
                }
            }

            // This is the raw data buffer we use to store the generated image.
            // We need it in this form so we can pass it to FFMPEG.
            var rawData = new byte[Width * Height * 3];
            // We also need to "pin" it so the bitmap can be based on it.
            var pinnedArray = GCHandle.Alloc(rawData, GCHandleType.Pinned);
            using (var bm = new Bitmap(Width, Height, Width * 3, PixelFormat.Format24bppRgb, pinnedArray.AddrOfPinnedObject()))
            using (var g = Graphics.FromImage(bm))
            {
                // Enable anti-aliased lines
                g.SmoothingMode = SmoothingMode.HighQuality;

                var pens = _channels.Select(c => new Pen(c.Color, c.LineWidth) {MiterLimit = c.LineWidth, LineJoin = LineJoin.Bevel}).ToList();

                int numFrames = sampleLength * FramesPerSecond / SamplingRate;
                var points = new PointF[viewWidth];

                for (int frameIndex = 0; frameIndex < numFrames; ++frameIndex)
                {
                    // Compute the start of the sample window
                    int frameIndexSamples = frameIndex * SamplingRate / FramesPerSecond;

                    // Copy from the template
                    g.DrawImageUnscaled(template, 0, 0);

                    var frameSamples = SamplingRate / FramesPerSecond;

                    // For each channel...
                    for (int channelIndex = 0; channelIndex < _channels.Count; ++channelIndex)
                    {
                        var channel = _channels[channelIndex];

                        // Compute the "trigger point".. This will be the centre of our rendering.
                        var triggerPoint = channel.GetTriggerPoint(frameIndexSamples, frameSamples);

                        // Compute the initial x, y to render the line from.
                        var yBase = renderingBounds.Top + channelIndex / Columns * viewHeight + viewHeight / 2;
                        var xBase = renderingBounds.Left + (channelIndex % Columns) * renderingBounds.Width / Columns ;

                        // And the initial sample index
                        var leftmostSampleIndex = triggerPoint - RenderedLineWidthInSamples / 2;

                        // Then, for each pixel, compute the Y coordinate for the waveform
                        for (int x = 0; x < viewWidth; x++)
                        {
                            var sampleIndex = leftmostSampleIndex + x * RenderedLineWidthInSamples / viewWidth;
                            var sampleValue = channel.GetSample(sampleIndex);

                            // Compute the Y coordinate
                            var y = yBase - sampleValue * viewHeight / 2;

                            points[x].X = x + xBase;
                            points[x].Y = y;
                        }

                        // Then draw them all in one go...
                        g.DrawLines(pens[channelIndex], points);
                        /*
                        // This can do "filled paths" - maybe?
                        var path = new GraphicsPath();
                        path.AddLine(points[0].X, yBase, points[0].X, points[0].Y);
                        path.AddLines(points);
                        path.AddLine(points[points.Length-1].X, points[points.Length-1].Y, points[points.Length-1].X, yBase);
                        g.DrawPath(pens[channelIndex], path);
                        */
                    }

                    // Emit
                    double fractionComplete = (double) (frameIndex + 1)/ numFrames;
                    foreach (var output in outputs)
                    {
                        output.Write(rawData, bm, fractionComplete);
                    }
                }

            }

            pinnedArray.Free();
        }


        public class ZeroLineConfig
        {
            public Color Color { get; set; }
            public float Width { get; set; }
        }
    }
}

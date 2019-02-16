﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
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
        public Color BackgroundColor { get; set; } = Color.Black;
        public Image BackgroundImage { get; set; }
        public Rectangle RenderingBounds { get; set; }

        public void AddChannel(Channel channel)
        {
            _channels.Add(channel);
        }

        public void Render(IList<IGraphicsOutput> outputs)
        {
            // This is the raw data buffer we use to store the generated image.
            // We need it in this form so we can pass it to FFMPEG.
            var rawData = new byte[Width * Height * 3];
            // We also need to "pin" it so the bitmap can be based on it.
            GCHandle pinnedArray = GCHandle.Alloc(rawData, GCHandleType.Pinned);
            try
            {
                using (var bm = new Bitmap(Width, Height, Width * 3, PixelFormat.Format24bppRgb, pinnedArray.AddrOfPinnedObject()))
                {
                    int numFrames = (int)((long)_channels.Max(c => c.SampleCount) * FramesPerSecond / SamplingRate);

                    int frameIndex = 0;
                    Render(bm, () =>
                        {
                            double fractionComplete = (double) ++frameIndex / numFrames;
                            foreach (var output in outputs)
                            {
                                // ReSharper disable once AccessToDisposedClosure
                                output.Write(rawData, bm, fractionComplete);
                            }
                        },
                        0, numFrames);
                }
            }
            finally
            {
                pinnedArray.Free();
            }
        }

        private void Render(Image destination, Action onFrame, int startFrame, int endFrame)
        {
            // Default rendering bounds if not set
            var renderingBounds = RenderingBounds;
            if (renderingBounds.Width == 0 || renderingBounds.Height == 0)
            {
                renderingBounds = new Rectangle(0, 0, Width, Height);
            }

            // Compute channel bounds
            var numRows = _channels.Count / Columns + (_channels.Count % Columns == 0 ? 0 : 1);
            for (int i = 0; i < _channels.Count; ++i)
            {
                var channel = _channels[i];
                var column = i % Columns;
                var row = i / Columns;
                channel.X = column * renderingBounds.Width / Columns;
                channel.Y = row * renderingBounds.Height / numRows;
                // Compute sizes as difference to next one to avoid off by 1 errors
                channel.Width = (column + 1) * renderingBounds.Width / Columns - channel.X;
                channel.Height = (row + 1) * renderingBounds.Height / numRows - channel.Y;
            }

            // We generate our "base image"
            using (var template = GenerateTemplate())
            {
                using (var g = Graphics.FromImage(destination))
                {
                    // Enable anti-aliased lines
                    g.SmoothingMode = SmoothingMode.HighQuality;

                    // Prepare the pens and brushes we will use
                    var pens = _channels.Select(c => c.LineColor == Color.Transparent || c.LineWidth <= 0
                        ? null
                        : new Pen(c.LineColor, c.LineWidth)
                        {
                            MiterLimit = c.LineWidth,
                            LineJoin = LineJoin.Bevel
                        }).ToList();
                    var brushes = _channels.Select(c => c.FillColor == Color.Transparent 
                        ? null 
                        : new SolidBrush(c.FillColor)).ToList();

                    // Prepare buffers to hold the line coordinates
                    var buffers = _channels.Select(channel => new PointF[channel.ViewWidthInSamples]).ToList();
                    var path = new GraphicsPath();

                    var frameSamples = SamplingRate / FramesPerSecond;

                    // Initialise the "previous trigger points"
                    var triggerPoints = new int[_channels.Count];
                    for (int channelIndex = 0; channelIndex < _channels.Count; ++channelIndex)
                    {
                        triggerPoints[channelIndex] = (int)((long)startFrame * SamplingRate / FramesPerSecond) - frameSamples;
                    }

                    for (int frameIndex = startFrame; frameIndex < endFrame; ++frameIndex)
                    {
                        // Compute the start of the sample window
                        int frameIndexSamples = (int)((long)frameIndex * SamplingRate / FramesPerSecond);

                        // Copy from the template
                        g.DrawImageUnscaled(template, 0, 0);

                        // For each channel...
                        for (int channelIndex = 0; channelIndex < _channels.Count; ++channelIndex)
                        {
                            var channel = _channels[channelIndex];
                            if (channel.IsEmpty)
                            {
                                continue;
                            }

                            // Compute the initial x, y to render the line from.
                            var yBase = channel.Y + channel.Height / 2;
                            var xBase = channel.X;

                            if (!string.IsNullOrEmpty(channel.ErrorMessage))
                            {
                                g.DrawString(channel.ErrorMessage, SystemFonts.DefaultFont, Brushes.Red, new RectangleF(
                                    channel.X,
                                    channel.Y,
                                    channel.Width,
                                    channel.Height));
                            }
                            else if (channel.Loading)
                            {
                                g.DrawString("Loading data...", SystemFonts.DefaultFont, Brushes.Green, xBase, yBase);
                            }
                            else if (channel.IsSilent)
                            {
                                g.DrawString("This channel is silent", SystemFonts.DefaultFont, Brushes.Yellow, xBase, yBase);
                            }
                            else
                            {
                                // Compute the "trigger point". This will be the centre of our rendering.
                                var triggerPoint = channel.GetTriggerPoint(frameIndexSamples, frameSamples, triggerPoints[channelIndex]);
                                triggerPoints[channelIndex] = triggerPoint;

                                RenderWave(g, channel, triggerPoint, pens[channelIndex], brushes[channelIndex], buffers[channelIndex], path);
                            }
                        }

                        // Emit
                        onFrame();
                    }

                    foreach (var pen in pens)
                    {
                        pen?.Dispose();
                    }
                    foreach (var brush in brushes)
                    {
                        brush?.Dispose();
                    }
                }
            }
        }

        private Bitmap GenerateTemplate()
        {
            var template = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);
            
            // Draw it
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
                        g.FillRectangle(brush, -1, -1, Width + 1, Height + 1);
                    }
                }

                foreach (var channel in _channels)
                {
                    if (channel.ZeroLineColor != Color.Transparent && channel.ZeroLineWidth > 0)
                    {
                        using (var pen = new Pen(channel.ZeroLineColor, channel.ZeroLineWidth))
                        {
                            // Draw the zero line
                            g.DrawLine(pen, channel.X, channel.Y + channel.Height / 2, channel.X + channel.Width, channel.Y + channel.Height / 2);
                        }
                    }

                    if (channel.LabelFont != null && channel.LabelColor != Color.Transparent)
                    {
                        g.TextRenderingHint = TextRenderingHint.AntiAlias;
                        using (var brush = new SolidBrush(channel.LabelColor))
                        {
                            g.DrawString(channel.Name, channel.LabelFont, brush, channel.X, channel.Y);
                        }
                    }

                    if (channel.BorderWidth > 0 && channel.BorderColor != Color.Transparent)
                    {
                        using (var pen = new Pen(channel.BorderColor, channel.BorderWidth))
                        {
                            g.DrawRectangle(pen, channel.X, channel.Y, channel.Width, channel.Height);
                        }
                    }
                }
            }

            return template;
        }

        private void RenderWave(Graphics g, Channel channel, int triggerPoint, Pen pen, Brush brush, PointF[] points, GraphicsPath path)
        {
            // And the initial sample index
            var leftmostSampleIndex = triggerPoint - channel.ViewWidthInSamples / 2;

            for (int i = 0; i < channel.ViewWidthInSamples; ++i)
            {
                var sampleValue = channel.GetSample(leftmostSampleIndex + i);
                points[i].X = channel.X + (float)channel.Width * i / channel.ViewWidthInSamples;
                points[i].Y = channel.Y + (sampleValue + 1) * channel.Height * 0.5f;
            }

            // Then draw them all in one go...
            if (pen != null)
            {
                g.DrawLines(pen, points);
            }

            if (brush != null)
            {
                path.Reset();
                path.AddLine(points[0].X, channel.Y + channel.Height / 2, points[0].X, points[0].Y);
                path.AddLines(points);
                path.AddLine(points[points.Length - 1].X, points[points.Length - 1].Y, points[points.Length - 1].X, channel.Y + channel.Height / 2);
                g.FillPath(brush, path);
            }
        }

        /// <summary>
        /// Version for rendering a single frame for previewing
        /// </summary>
        public Bitmap RenderFrame(float position = 0)
        {
            var frameIndex = _channels.Count > 0
                ? (int) (position * _channels.Max(c => c.SampleCount) * FramesPerSecond / SamplingRate)
                : 0;
            var bitmap = new Bitmap(Width, Height);
            Render(bitmap, () => { }, frameIndex, frameIndex + 1);
            return bitmap;
        }
    }
}

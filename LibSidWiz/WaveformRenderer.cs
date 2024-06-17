using System;
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
        private readonly List<Channel> _channels = [];

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
            // We need it in this form, so we can pass it to FFMPEG.
            var rawData = new byte[Width * Height * 4];
            // We also need to "pin" it so the bitmap can be based on it.
            GCHandle pinnedArray = GCHandle.Alloc(rawData, GCHandleType.Pinned);
            try
            {
                using var bm = new Bitmap(Width, Height, Width * 4, PixelFormat.Format32bppPArgb, pinnedArray.AddrOfPinnedObject());
                int numFrames = (int)(_channels.Max(c => c.SampleCount) * FramesPerSecond / SamplingRate);
                var length = TimeSpan.FromSeconds((double)numFrames / FramesPerSecond);

                int frameIndex = 0;
                Render(bm, rawData, () =>
                    {
                        double fractionComplete = (double) ++frameIndex / numFrames;
                        foreach (var output in outputs)
                        {
                            // ReSharper disable once AccessToDisposedClosure
                            // bm is disposed after Render() returns, but it never invokes this 
                            output.Write(bm, rawData, fractionComplete, length);
                        }
                    },
                    0, numFrames);
            }
            finally
            {
                pinnedArray.Free();
            }
        }

        /// <summary>
        /// Renders a range of frames into the given destination image, calling back the handler for each one
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="onFrame"></param>
        /// <param name="startFrame"></param>
        /// <param name="endFrame"></param>
        /// <param name="imageBuffer"></param>
        private void Render(Image destination, byte[] imageBuffer, Action onFrame, int startFrame, int endFrame)
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
                int ChannelX(int column1) => column1 * renderingBounds.Width / Columns + renderingBounds.Left;
                int ChannelY(int row1) => row1 * renderingBounds.Height / numRows + renderingBounds.Top;

                var channel = _channels[i];
                var column = i % Columns;
                var row = i / Columns;
                // Compute sizes as difference to next one to avoid off-by-one errors
                var x = ChannelX(column);
                var y = ChannelY(row);
                channel.Bounds = new Rectangle(x, y, ChannelX(column + 1) - x, ChannelY(row + 1) - y);
            }

            // We generate our "base image"
            var templateData = new byte[Width * Height * 4];
            GCHandle pinnedArray = GCHandle.Alloc(templateData, GCHandleType.Pinned);
            try
            {
                using var templateImage = new Bitmap(Width, Height, Width * 4, PixelFormat.Format32bppPArgb,
                    pinnedArray.AddrOfPinnedObject());
                GenerateTemplate(templateImage);
                using var g = Graphics.FromImage(destination);
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
                    triggerPoints[channelIndex] =
                        (int) ((long) startFrame * SamplingRate / FramesPerSecond) - frameSamples;
                }

                // Formatting for error/progress messages
                var stringFormat = new StringFormat
                    {LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center};

                for (int frameIndex = startFrame; frameIndex < endFrame; ++frameIndex)
                {
                    // Compute the start of the sample window
                    int frameIndexSamples = (int) ((long) frameIndex * SamplingRate / FramesPerSecond);

                    // Copy from the template
                    if (imageBuffer == null)
                    {
                        g.DrawImageUnscaled(templateImage, 0, 0);
                    }
                    else
                    {
                        Buffer.BlockCopy(templateData, 0, imageBuffer, 0, templateData.Length);
                    }

                    // For each channel...
                    for (int channelIndex = 0; channelIndex < _channels.Count; ++channelIndex)
                    {
                        var channel = _channels[channelIndex];
                        if (channel.IsEmpty)
                        {
                            continue;
                        }

                        if (!string.IsNullOrEmpty(channel.ErrorMessage))
                        {
                            g.DrawString(channel.ErrorMessage, SystemFonts.DefaultFont, Brushes.Red,
                                channel.Bounds,
                                stringFormat);
                        }
                        else if (channel.Loading)
                        {
                            g.DrawString("Loading data...", SystemFonts.DefaultFont, Brushes.Green,
                                channel.Bounds,
                                stringFormat);
                        }
                        else if (channel.IsSilent && !channel.RenderIfSilent)
                        {
                            g.DrawString("This channel is silent", SystemFonts.DefaultFont, Brushes.Yellow,
                                channel.Bounds, stringFormat);
                        }
                        else
                        {
                            // Compute the "trigger point". This will be the centre of our rendering.
                            var triggerPoint = channel.GetTriggerPoint(frameIndexSamples, frameSamples,
                                triggerPoints[channelIndex]);
                            triggerPoints[channelIndex] = triggerPoint;

                            RenderWave(g, channel, triggerPoint, pens[channelIndex], brushes[channelIndex],
                                buffers[channelIndex], path, channel.FillBase);
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
            finally
            {
                pinnedArray.Free();
            }
        }

        private void GenerateTemplate(Image template)
        {
            // Draw it
            using var g = Graphics.FromImage(template);
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            if (BackgroundImage != null)
            {
                // Fill with the background image
                using var attribute = new ImageAttributes();
                attribute.SetWrapMode(WrapMode.TileFlipXY);
                g.DrawImage(
                    BackgroundImage,
                    new Rectangle(0, 0, Width, Height),
                    0,
                    0,
                    BackgroundImage.Width,
                    BackgroundImage.Height,
                    GraphicsUnit.Pixel,
                    attribute);
            }
            else
            {
                // Fill background
                using var brush = new SolidBrush(BackgroundColor);
                g.FillRectangle(brush, -1, -1, Width + 1, Height + 1);
            }

            foreach (var channel in _channels)
            {
                if (channel.BackgroundColor != Color.Transparent)
                {
                    using var b = new SolidBrush(channel.BackgroundColor);
                    g.FillRectangle(b, channel.Bounds);
                }

                if (channel.ZeroLineColor != Color.Transparent && channel.ZeroLineWidth > 0)
                {
                    using var pen = new Pen(channel.ZeroLineColor, channel.ZeroLineWidth);
                    // Draw the zero line
                    g.DrawLine(
                        pen,
                        channel.Bounds.Left,
                        channel.Bounds.Top + channel.Bounds.Height / 2,
                        channel.Bounds.Right,
                        channel.Bounds.Top + channel.Bounds.Height / 2);
                }

                if (channel.BorderWidth > 0 && channel.BorderColor != Color.Transparent)
                {
                    using var pen = new Pen(channel.BorderColor, channel.BorderWidth);
                    if (channel.BorderEdges)
                    {
                        // We want all edges to show equally.
                        // To achieve this, we need to artificially pull the edges in 1px on the right and bottom.
                        g.DrawRectangle(
                            pen,
                            channel.Bounds.Left,
                            channel.Bounds.Top,
                            channel.Bounds.Width - (channel.Bounds.Right == RenderingBounds.Right ? 1 : 0),
                            channel.Bounds.Height -
                            (channel.Bounds.Bottom == RenderingBounds.Bottom ? 1 : 0));
                    }
                    else
                    {
                        // We want to draw all lines which are not on the rendering bounds
                        if (channel.Bounds.Left != RenderingBounds.Left)
                        {
                            g.DrawLine(pen, channel.Bounds.Left, channel.Bounds.Top, channel.Bounds.Left,
                                channel.Bounds.Bottom);
                        }

                        if (channel.Bounds.Top != RenderingBounds.Top)
                        {
                            g.DrawLine(pen, channel.Bounds.Left, channel.Bounds.Top, channel.Bounds.Right,
                                channel.Bounds.Top);
                        }

                        if (channel.Bounds.Right != RenderingBounds.Right)
                        {
                            g.DrawLine(pen, channel.Bounds.Right, channel.Bounds.Top, channel.Bounds.Right,
                                channel.Bounds.Bottom);
                        }

                        if (channel.Bounds.Bottom != RenderingBounds.Bottom)
                        {
                            g.DrawLine(pen, channel.Bounds.Left, channel.Bounds.Bottom,
                                channel.Bounds.Right, channel.Bounds.Bottom);
                        }
                    }
                }

                if (channel.LabelFont != null && channel.LabelColor != Color.Transparent)
                {
                    g.TextRenderingHint = TextRenderingHint.AntiAlias;
                    using var brush = new SolidBrush(channel.LabelColor);
                    var stringFormat = new StringFormat();
                    var layoutRectangle = new RectangleF(
                        channel.Bounds.Left + channel.LabelMargins.Left,
                        channel.Bounds.Top + channel.LabelMargins.Top,
                        channel.Bounds.Width - channel.LabelMargins.Left - channel.LabelMargins.Right,
                        channel.Bounds.Height - channel.LabelMargins.Top - channel.LabelMargins.Bottom);
                    switch (channel.LabelAlignment)
                    {
                        case ContentAlignment.TopLeft:
                            stringFormat.Alignment = StringAlignment.Near;
                            stringFormat.LineAlignment = StringAlignment.Near;
                            break;
                        case ContentAlignment.TopCenter:
                            stringFormat.Alignment = StringAlignment.Center;
                            stringFormat.LineAlignment = StringAlignment.Near;
                            break;
                        case ContentAlignment.TopRight:
                            stringFormat.Alignment = StringAlignment.Far;
                            stringFormat.LineAlignment = StringAlignment.Near;
                            break;
                        case ContentAlignment.MiddleLeft:
                            stringFormat.Alignment = StringAlignment.Near;
                            stringFormat.LineAlignment = StringAlignment.Center;
                            break;
                        case ContentAlignment.MiddleCenter:
                            stringFormat.Alignment = StringAlignment.Center;
                            stringFormat.LineAlignment = StringAlignment.Center;
                            break;
                        case ContentAlignment.MiddleRight:
                            stringFormat.Alignment = StringAlignment.Far;
                            stringFormat.LineAlignment = StringAlignment.Center;
                            break;
                        case ContentAlignment.BottomLeft:
                            stringFormat.Alignment = StringAlignment.Near;
                            stringFormat.LineAlignment = StringAlignment.Far;
                            break;
                        case ContentAlignment.BottomCenter:
                            stringFormat.Alignment = StringAlignment.Center;
                            stringFormat.LineAlignment = StringAlignment.Far;
                            break;
                        case ContentAlignment.BottomRight:
                            stringFormat.Alignment = StringAlignment.Far;
                            stringFormat.LineAlignment = StringAlignment.Far;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    g.DrawString(channel.Label, channel.LabelFont, brush, layoutRectangle, stringFormat);
                }
            }
        }

        private void RenderWave(Graphics g, Channel channel, int triggerPoint, Pen pen, Brush brush, PointF[] points, GraphicsPath path, double fillBase)
        {
            // And the initial sample index
            var leftmostSampleIndex = triggerPoint - channel.ViewWidthInSamples / 2;

            float xOffset = channel.Bounds.Left;
            float xScale = (float) channel.Bounds.Width / channel.ViewWidthInSamples;
            float yOffset = channel.Bounds.Top + channel.Bounds.Height * 0.5f;
            float yScale = -channel.Bounds.Height * 0.5f;
            for (int i = 0; i < channel.ViewWidthInSamples; ++i)
            {
                var sampleValue = channel.GetSample(leftmostSampleIndex + i, false);
                points[i].X = xOffset + i * xScale;
                points[i].Y = yOffset + sampleValue * yScale;
            }
            if (channel.Clip)
            {
                for (int i = 0; i < channel.ViewWidthInSamples; ++i)
                {
                    points[i].Y = Math.Min(Math.Max(points[i].Y, channel.Bounds.Top), channel.Bounds.Bottom);
                }
            }

            // Enable anti-aliased lines
            g.SmoothingMode = channel.SmoothLines ? SmoothingMode.HighQuality : SmoothingMode.None;

            // Then draw them all in one go...
            if (pen != null)
            {
                // TODO: this is expensive
                g.DrawLines(pen, points);
            }

            if (brush != null)
            {
                // We need to add points to complete the path
                // We compute the Y position of this line. -0.5 scales -1..1 to bottom..top.
                var baseY = (float)(yOffset + channel.Bounds.Height * -0.5 * fillBase);
                path.Reset();
                path.AddLine(points[0].X, baseY, points[0].X, points[0].Y);
                path.AddLines(points);
                path.AddLine(points[points.Length - 1].X, points[points.Length - 1].Y, points[points.Length - 1].X, baseY);
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
            var bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppPArgb);
            Render(bitmap, null, () => { }, frameIndex, frameIndex + 1);
            return bitmap;
        }
    }
}

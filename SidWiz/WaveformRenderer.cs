using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;

namespace SidWiz
{
    /// <summary>
    /// Class responsible for rendering
    /// </summary>
    internal class WaveformRenderer
    {
        private readonly List<Channel> _channels = new List<Channel>();
//        private readonly List<ImageInfo> _images = new List<ImageInfo>();
//        private readonly IList<TextInfo> _text = new List<TextInfo>();

        public int Width { get; set; }
        public int Height { get; set; }
        public int Columns { get; set; }
        public int SamplingRate { get; set; }
        public int FramesPerSecond { get; set; }
        public int RenderedLineWidthInSamples { get; set; }
        public Color BackgroundColor { get; set; } = Color.Black;
        public Image BackgroundImage { get; set; }
        public Rectangle RenderingBounds { get; set; }

        private class Channel
        {
            public Channel(IList<float> samples, Color color, float lineWidth, string name)
            {
                Samples = samples;
                Color = color;
                Name = name;
                LineWidth = lineWidth;
            }

            public IList<float> Samples { get; }
            public Color Color { get; }
            public string Name { get; }
            public float LineWidth { get; }
        }

        public void AddChannel(IList<float> samples, Color color, float lineWidth, string name)
        {
            _channels.Add(new Channel(samples, color, lineWidth, name));
        }
        /*
        public void AddImage(Image image, ContentAlignment alignment, bool stretchToFit, bool constrainWaves)
        {
            _images.Add(new ImageInfo(image, alignment, stretchToFit, constrainWaves));
        }

        internal class ImageInfo
        {
            public Image Image { get; }
            public ContentAlignment Alignment { get; }
            public bool StretchToFit { get; }
            public bool ConstrainWaves { get; }

            public ImageInfo(Image image, ContentAlignment alignment, bool stretchToFit, bool constrainWaves)
            {
                Image = image;
                Alignment = alignment;
                StretchToFit = stretchToFit;
                ConstrainWaves = constrainWaves;
            }
        }

        public void AddText(string text, Font font, ContentAlignment alignment, bool constrainWaves, Color color)
        {
            _text.Add(new TextInfo(text, font, alignment, constrainWaves, color));
        }

        internal class TextInfo
        {
            private readonly bool _constrainWaves;
            public string Text { get; }
            public Font Font { get; }
            public ContentAlignment Alignment { get; }
            public Color Color { get; }

            public TextInfo(string text, Font font, ContentAlignment alignment, bool constrainWaves, Color color)
            {
                _constrainWaves = constrainWaves;
                Text = text;
                Font = font;
                Alignment = alignment;
                Color = color;
            }
        }
        */
        public void Render(IList<IGraphicsOutput> outputs)
        {
            int sampleLength = _channels.Max(c => c.Samples.Count);

            // We generate our "base image"
            var template = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);
            Rectangle drawingRectangle = new Rectangle(0, 0, Width, Height);
            using (var g = Graphics.FromImage(template))
            {
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

                /*
                // Draw images
                foreach (var imageInfo in _images)
                {
                    int imageWidth = imageInfo.Image.Width;
                    int imageHeight = imageInfo.Image.Height;
                    // Compute size if stretching
                    if (imageInfo.StretchToFit)
                    {
                        double aspectRatio = Width / (double) Height;
                        double imageAspectRatio = imageWidth / (double) imageHeight;
                        if (imageAspectRatio > aspectRatio)
                        {
                            // Image has a wider aspect ratio
                            imageWidth = Width;
                            imageHeight = (int) Math.Round(Width * imageAspectRatio);
                        }
                        else
                        {
                            imageHeight = Height;
                            imageWidth = (int) Math.Round(Height / imageAspectRatio);
                        }
                    }

                    // Compute where to draw it
                    var r = AlignedRect(imageInfo.Alignment, Width, Height, imageWidth, imageHeight);
                    g.DrawImage(imageInfo.Image, r);
                    // Apply constriction?
                    if (imageInfo.ConstrainWaves)
                    {
                        // TODO
                    }
                }

                // Draw text
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                foreach (var textInfo in _text)
                {
                    // Measure it
                    var size = g.MeasureString(textInfo.Text, textInfo.Font, new SizeF(Width, Height));
                    // Draw it
                    using (var b = new SolidBrush(textInfo.Color))
                    {
                        g.DrawString(textInfo.Text, textInfo.Font, b, AlignedRect(textInfo.Alignment, Width, Height, size.Width, size.Height));
                    }

                    // TODO constriction
                }
                */
            }

            var renderingBounds = RenderingBounds;
            if (renderingBounds.Width == 0 || renderingBounds.Height == 0)
            {
                // Default to no bounds
                renderingBounds = new Rectangle(0, 0, Width, Height);
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
                int viewWidth = renderingBounds.Width / Columns;
                int viewHeight = renderingBounds.Height / (int)Math.Ceiling((double)_channels.Count / Columns);
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
                        var triggerPoint = GetTriggerPoint(channel, frameIndexSamples, frameSamples);

                        // Compute the initial x, y to render the line from.
                        var yBase = renderingBounds.Top + channelIndex / Columns * viewHeight + viewHeight / 2;
                        var xBase = renderingBounds.Left + (channelIndex % Columns) * renderingBounds.Width / Columns ;

                        // And the initial sample index
                        var leftmostSampleIndex = frameIndexSamples + triggerPoint - RenderedLineWidthInSamples / 2;

                        // Then, for each pixel, compute the Y coordinate for the waveform
                        for (int x = 0; x < viewWidth; x++)
                        {
                            var sampleIndex = leftmostSampleIndex + x * RenderedLineWidthInSamples / viewWidth;
                            var sampleValue = GetSample(channel, sampleIndex);

                            // Compute the Y coordinate
                            var y = yBase - sampleValue * viewHeight;

                            points[x].X = x + xBase;
                            points[x].Y = y;
                        }

                        // Then draw them all in one go...
                        g.DrawLines(pens[channelIndex], points);
                    }

                    // Emit
                    double fractionComplete = (double) (frameIndex + 1)/ numFrames;
                    foreach (var output in outputs)
                    {
                        output.Write(rawData, bm, fractionComplete);
                    }
                    // TODO
                    // ffWriter.Write(rawData);
                }

            }

            //
            pinnedArray.Free();
        }

        private int GetTriggerPoint(Channel channel, int frameIndexSamples, int frameSamples)
        {
            // Find the centre point as the first point after the minimum where we see positive -> negative -> positive transition.
            // We disallow looking too far ahead.
            // TODO breaks on some waveforms - maybe better if we had a high pass filter?
            int frameTriggerOffset = 0;
            while (GetSample(channel, frameIndexSamples + frameTriggerOffset) > 0 && frameTriggerOffset < frameSamples) frameTriggerOffset++;
            while (GetSample(channel, frameIndexSamples + frameTriggerOffset) <= 0 && frameTriggerOffset < frameSamples) frameTriggerOffset++;
            if (frameTriggerOffset == frameSamples)
            {
                // Failed to find anything, just stick to the middle
                frameTriggerOffset = frameSamples / 2;
            }

            return frameTriggerOffset;
        }

        private float GetSample(Channel channel, int sampleIndex)
        {
            return sampleIndex < 0 || sampleIndex >= channel.Samples.Count ? 0 : channel.Samples[sampleIndex];
        }

/*
        private RectangleF AlignedRect(ContentAlignment alignment, float width, float height, float imageWidth,
            float imageHeight)
        {
            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                    return new RectangleF(0, 0, imageWidth, imageHeight);
                case ContentAlignment.TopCenter:
                    return new RectangleF((width - imageWidth) / 2, 0, imageWidth, imageHeight);
                case ContentAlignment.TopRight:
                    return new RectangleF(width - imageWidth, 0, imageWidth, imageHeight);
                case ContentAlignment.MiddleLeft:
                    return new RectangleF(width - imageWidth, (width - imageWidth) / 2, imageWidth, imageHeight);
                case ContentAlignment.MiddleCenter:
                    return new RectangleF((width - imageWidth) / 2, (width - imageWidth) / 2, imageWidth, imageHeight);
                case ContentAlignment.MiddleRight:
                    return new RectangleF(width - imageWidth, (width - imageWidth) / 2, imageWidth, imageHeight);
                case ContentAlignment.BottomLeft:
                    return new RectangleF(width - imageWidth, width - imageWidth, imageWidth, imageHeight);
                case ContentAlignment.BottomCenter:
                    return new RectangleF((width - imageWidth) / 2, width - imageWidth, imageWidth, imageHeight);
                case ContentAlignment.BottomRight:
                    return new RectangleF(width - imageWidth, width - imageWidth, imageWidth, imageHeight);
                default:
                    throw new Exception("Unhandled enum value " + alignment);
            }
        }
*/
    }
}

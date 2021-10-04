using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Windows.Forms;

namespace SidWizPlus
{
    class BackgroundRenderer: IDisposable
    {
        public Image Image { get; }
        public Rectangle WaveArea { get; private set; }

        private readonly Graphics _graphics;
        private readonly double _aspectRatio;
        private readonly int _width;
        private readonly int _height;

        public BackgroundRenderer(int width, int height, Color backgroundColor)
        {
            _width = width;
            _height = height;
            _aspectRatio = _width / (double) _height;

            Image = new Bitmap(_width, _height, PixelFormat.Format32bppArgb);
            WaveArea = new Rectangle(0, 0, _width, _height);
            _graphics = Graphics.FromImage(Image);
            // Fill it in
            using (var brush = new SolidBrush(backgroundColor))
            {
                _graphics.FillRectangle(brush, WaveArea);
            }
            _graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
        }

        public void Dispose()
        {
            _graphics?.Dispose();
            Image?.Dispose();
        }


        public void Add(ImageInfo imageInfo)
        {
            int imageWidth = imageInfo.Image.Width;
            int imageHeight = imageInfo.Image.Height;
            // Compute size if stretching
            if (imageInfo.StretchToFit)
            {
                double imageAspectRatio = imageWidth / (double) imageHeight;
                if (imageAspectRatio > _aspectRatio)
                {
                    // Image has a wider aspect ratio
                    imageWidth = _width;
                    imageHeight = (int) Math.Round(_width / imageAspectRatio);
                }
                else
                {
                    imageHeight = _height;
                    imageWidth = (int) Math.Round(_height * imageAspectRatio);
                }
            }

            // Compute where to draw it
            var rect = AlignedRect(imageInfo.Alignment, _width, _height, imageWidth, imageHeight);
            var ia = new ImageAttributes();
            if (imageInfo.StretchToFit)
            {
                // Make sure stretched images don't have fuzzy edges
                ia.SetWrapMode(WrapMode.TileFlipXY);
            }
            // Apply any alpha
            if (imageInfo.Alpha < 1.0)
            {
                ia.SetColorMatrix(new ColorMatrix {Matrix33 = imageInfo.Alpha});
            }
            _graphics.DrawImage(imageInfo.Image, rect, 0, 0, imageInfo.Image.Width, imageInfo.Image.Height, GraphicsUnit.Pixel, ia);
            // Apply constriction
            Constrain(rect, imageInfo.ConstrainWaves);
        }

        public void Add(TextInfo textInfo)
        {
            using var font = new Font(textInfo.FontName, textInfo.FontSize, textInfo.FontStyle);
            // Measure it
            var size = _graphics.MeasureString(textInfo.Text, font, new SizeF(_width, _height));
            // Draw it
            var rect = AlignedRect(textInfo.Alignment, _width, _height, (int) Math.Ceiling(size.Width),
                (int) Math.Ceiling(size.Height));
            using (var brush = new SolidBrush(textInfo.Color))
            {
                _graphics.DrawString(textInfo.Text, font, brush, rect, new StringFormat
                {
                    Alignment = textInfo.Alignment switch
                    {
                        ContentAlignment.BottomCenter or ContentAlignment.MiddleCenter or ContentAlignment.TopCenter => StringAlignment.Center,
                        ContentAlignment.BottomLeft or ContentAlignment.MiddleLeft or ContentAlignment.TopLeft => StringAlignment.Near,
                        _ => StringAlignment.Far,
                    }
                });
            }

            // Apply constriction
            Constrain(rect, textInfo.ConstrainWaves);
        }

        private Rectangle AlignedRect(ContentAlignment alignment, int width, int height, int imageWidth, int imageHeight)
        {
            return alignment switch
            {
                ContentAlignment.TopLeft => new Rectangle(0, 0, imageWidth, imageHeight),
                ContentAlignment.TopCenter => new Rectangle((width - imageWidth) / 2, 0, imageWidth, imageHeight),
                ContentAlignment.TopRight => new Rectangle(width - imageWidth, 0, imageWidth, imageHeight),
                ContentAlignment.MiddleLeft => new Rectangle(0, (height - imageHeight) / 2, imageWidth, imageHeight),
                ContentAlignment.MiddleCenter => new Rectangle((width - imageWidth) / 2, (height - imageHeight) / 2, imageWidth, imageHeight),
                ContentAlignment.MiddleRight => new Rectangle(width - imageWidth, (height - imageHeight) / 2, imageWidth, imageHeight),
                ContentAlignment.BottomLeft => new Rectangle(0, height - imageHeight, imageWidth, imageHeight),
                ContentAlignment.BottomCenter => new Rectangle((width - imageWidth) / 2, height - imageHeight, imageWidth, imageHeight),
                ContentAlignment.BottomRight => new Rectangle(width - imageWidth, height - imageHeight, imageWidth, imageHeight),
                _ => throw new Exception("Unhandled enum value " + alignment),
            };
        }

        private void Constrain(Rectangle source, DockStyle dockStyle)
        {
            var result = WaveArea;
            switch (dockStyle)
            {
                case DockStyle.Top:
                    result.Y += source.Height;
                    result.Height -= source.Height;
                    break;
                case DockStyle.Bottom:
                    result.Height -= source.Height;
                    break;
                case DockStyle.Left:
                    result.X += source.Width;
                    result.Width -= source.Width;
                    break;
                case DockStyle.Right:
                    result.Width -= source.Width;
                    break;
            }

            WaveArea = result;
        }

    }
}

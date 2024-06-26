using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace SidWizPlusGUI
{
    public static class HighDpiHelper
    {
        public static void AdjustControlImagesDpiScale(Control container)
        {
            var dpiScale = GetDpiScale(container).Value;
            if (CloseToOne(dpiScale))
            {
                return;
            }

            AdjustControlImagesDpiScale(container.Controls, dpiScale);
        }

        private static void AdjustControlImagesDpiScale(IEnumerable controls, float dpiScale)
        {
            foreach (Control control in controls)
            {
                switch (control)
                {
                    case ButtonBase button when button.Image != null:
                        button.Image = ScaleImage(button.Image, dpiScale);
                        break;
                    case SplitContainer splitContainer:
                        splitContainer.SplitterDistance = (int)(splitContainer.SplitterDistance * dpiScale);
                        break;
                    case TabControl tabControl when tabControl.ImageList != null:
                    {
                        var imageList = new ImageList
                        {
                            ImageSize = ScaleSize(tabControl.ImageList.ImageSize, dpiScale),
                            ColorDepth = ColorDepth.Depth32Bit
                        };

                        for (int i = 0 ; i < tabControl.ImageList.Images.Count; ++i)
                        {
                            imageList.Images.Add(
                                tabControl.ImageList.Images.Keys[i], 
                                ScaleImage(tabControl.ImageList.Images[i], dpiScale));
                        }

                        tabControl.ImageList = imageList;
                        break;
                    }
                    case ToolStrip toolStrip:
                        ScaleToolStrip(dpiScale, toolStrip);

                        toolStrip.AutoSize = true;
                        break;
                }

                if (control.ContextMenuStrip != null)
                {
                    ScaleToolStrip(dpiScale, control.ContextMenuStrip);
                }

                // Then recurse
                AdjustControlImagesDpiScale(control.Controls, dpiScale);
            }
        }

        private static void ScaleToolStrip(float dpiScale, ToolStrip toolStrip)
        {
            toolStrip.ImageScalingSize = ScaleSize(toolStrip.ImageScalingSize, dpiScale);
            foreach (var item in toolStrip.Items.Cast<ToolStripItem>().Where(i => i.Image != null))
            {
                item.Image = ScaleImage(item.Image, dpiScale);
            }
        }

        private static bool CloseToOne(float dpiScale)
        {
            return Math.Abs(dpiScale - 1) < 0.001;
        }

        private static Lazy<float> GetDpiScale(Control control)
        {
            return new Lazy<float>(() =>
            {
                using var graphics = control.CreateGraphics();
                return graphics.DpiX / 96.0f;
            });
        }

        private static Image ScaleImage(Image image, float dpiScale)
        {
            var newSize = ScaleSize(image.Size, dpiScale);
            var newBitmap = new Bitmap(newSize.Width, newSize.Height);

            using (var g = Graphics.FromImage(newBitmap))
            {
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(image, new Rectangle(new Point(), newSize));
            }

            image.Dispose();

            return newBitmap;
        }

        private static Size ScaleSize(Size size, float scale)
        {
            return new Size((int) (size.Width * scale), (int) (size.Height * scale));
        }
    }
}

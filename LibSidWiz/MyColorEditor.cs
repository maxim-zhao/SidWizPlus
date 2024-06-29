using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace LibSidWiz;

/// <summary>
/// This allows us to implement a custom colour picker
/// </summary>
public class MyColorEditor : UITypeEditor
{
    public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
    {
        return UITypeEditorEditStyle.Modal;
    }

    public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
    {
        if (value is not Color c || provider == null)
        {
            return value;
        }

        var svc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));

        if (svc == null)
        {
            return c;
        }

        using var form = new Cyotek.Windows.Forms.ColorPickerDialog();
        form.Color = c;
        form.ShowAlphaChannel = true;
        return svc.ShowDialog(form) == DialogResult.OK ? form.Color : c;
    }

    public override bool GetPaintValueSupported(ITypeDescriptorContext context)
    {
        return true;
    }

    public override void PaintValue(PaintValueEventArgs e)
    {
        // This is just for the little rectangle on the left
        // TODO: indicate transparency?
        var color = (Color)e.Value;
        if (color.A < 255)
        {
            // Draw a checkerboard of 4x4 using the system colours
            e.Graphics.FillRectangle(SystemBrushes.Window, e.Bounds);
            for (var x = 0; x < e.Bounds.Width; x += 4)
            for (var y = 0; y < e.Bounds.Height; y += 4)
                if (((x/4 ^ y/4) & 1) == 0)
                {
                    e.Graphics.FillRectangle(SystemBrushes.WindowText, x, y, 4, 4);
                }
        }
        using (var brush = new SolidBrush(color))
        {
            e.Graphics.FillRectangle(brush, e.Bounds);
        }

        e.Graphics.DrawRectangle(SystemPens.WindowText, e.Bounds);
    }
}
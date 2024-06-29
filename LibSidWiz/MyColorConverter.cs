using System.ComponentModel;
using System.Drawing;

namespace LibSidWiz;

/// <summary>
/// This overrides ColorConverter which interferes with the colour editor.
/// </summary>
public class MyColorConverter : ColorConverter
{
    public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
    {
        return false;
    }
}
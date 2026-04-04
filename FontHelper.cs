using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows; // For FontStyle, FontStyles, FontWeight, FontWeights
using System.Drawing; // For FontStyle enum

namespace TemplateEdit;
public static class FontHelper
{
    public static System.Drawing.FontStyle ConvertWpfToGdiFontStyle(System.Windows.FontStyle wpfFontStyle, FontWeight wpfFontWeight)
    {
        System.Drawing.FontStyle gdiFontStyle = System.Drawing.FontStyle.Regular;

        // Map Italic and Oblique to Italic
        if (wpfFontStyle == FontStyles.Italic || wpfFontStyle == FontStyles.Oblique)
        {
            gdiFontStyle |= System.Drawing.FontStyle.Italic;
        }

        // Map font weights greater than or equal to Bold to Bold
        // GDI+ FontStyle has a binary concept of bold (either on or off).
        // A common approach is to consider any WPF weight from "Bold" upwards as bold.
        if (wpfFontWeight >= FontWeights.Bold)
        {
            gdiFontStyle |= System.Drawing.FontStyle.Bold;
        }

        return gdiFontStyle;
    }
}

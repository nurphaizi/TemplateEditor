using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TemplateEdit;
public static class TsplElementConverter
{
    public static ( byte[] pngBytes,int width,int height) LineToTsplBitmap(Line line, double dpi = 96)
    {
        double Width = Math.Abs(line.X2 - line.X1) + line.StrokeThickness;
        double Height = Math.Abs(line.Y2 - line.Y1) + line.StrokeThickness;
        line.Measure(new Size(Width, Height));
        line.Arrange(new Rect(new Size(Width, Height)));
        int width = (int)Math.Ceiling(Width*dpi/96.0);
        int height = (int)Math.Ceiling(Height * dpi / 96.0);
        RenderTargetBitmap rtb = new RenderTargetBitmap(width, height, dpi, dpi, PixelFormats.Pbgra32);
        rtb.Render(line);
        rtb.Freeze();
        BitmapExtensions.SaveRenderTargetBitmap(rtb, @"C:\WB_SQL\line.png");
        PngBitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));

        using (MemoryStream ms = new MemoryStream())
        {
            encoder.Save(ms);
            return (ms.ToArray(), width, height);
        }
    }

public static  Image ConvertLineToImage(Line line)
{
    // Measure and arrange the line
    line.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
    line.Arrange(new Rect(line.DesiredSize));

    int width = (int)Math.Ceiling(line.RenderSize.Width+line.StrokeThickness);
    int height = (int)Math.Ceiling(line.RenderSize.Height + line.StrokeThickness);

    if (width == 0) width = 1;
    if (height == 0) height = 1;

    // Render the line to bitmap
    RenderTargetBitmap rtb = new RenderTargetBitmap(
        width,
        height,
        96,
        96,
        PixelFormats.Pbgra32);

    rtb.Render(line);

    // Create Image control
    Image img = new Image();
    img.Source = rtb;
    img.Width = width;
    img.Height = height;

    return img;
}
}

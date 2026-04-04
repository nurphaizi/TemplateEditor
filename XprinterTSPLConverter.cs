using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ExCSS;
using SharpVectors.Dom;
using SixLabors.ImageSharp;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Brushes = System.Drawing.Brushes;

namespace TemplateEdit;
public static class XprinterTSPLConverter
{
    // Creates the full byte array with TSPL commands to print the image.
    public static byte[] CreateBitmapPrintCommand(string imagePath)
    {
        using (Bitmap originalBitmap = new Bitmap(imagePath))
        {
            // Define a maximum width for the printer (e.g., 600 pixels for 203 DPI, 76mm width)
            int maxWidth = 470;

            // Resize if the image is wider than the printer's capabilities
            if (originalBitmap.Width > maxWidth)
            {
                int newHeight = (int)((float)originalBitmap.Height * ((float)maxWidth / originalBitmap.Width));
                using (Bitmap resizedBitmap = new Bitmap(originalBitmap, maxWidth, newHeight))
                {
                    return GeneratePrinterCommands(resizedBitmap);
                }
            }
            else
            {
                return GeneratePrinterCommands(originalBitmap);
            }
        }
    }

    // Generates the TSPL commands with the monochrome bitmap data.
    private static byte[] GeneratePrinterCommands(Bitmap bitmapToPrint)
    {
        int imageWidthPixels = bitmapToPrint.Width;
        int imageHeightPixels = bitmapToPrint.Height;
        // Calculate width in bytes (8 pixels per byte) for TSPL command
        int imageWidthBytes = (imageWidthPixels + 7) / 8;

        // Convert the bitmap to the required 1-bit monochrome format
        byte[] bitmapData = BitmapExtensions.ConvertToMonochromeBytes(bitmapToPrint, imageWidthPixels, imageHeightPixels);
        // TSPL commands for printing a bitmap
        // SIZE: Sets label size (adjust as needed)
        byte[] sizeCommand = Encoding.ASCII.GetBytes("SIZE 59 mm, 40 mm\r\n");
        // CLS: Clears the printer's memory buffer
        byte[] clsCommand = Encoding.ASCII.GetBytes("CLS\r\n");
        // BITMAP: X_START, Y_START, WIDTH_IN_BYTES, HEIGHT_IN_PIXELS, MODE(1=OVERWRITE), DATA
        byte[] bitmapCommandHeader = Encoding.ASCII.GetBytes($"BITMAP 10,{imageHeightPixels / 2},{imageWidthBytes},{imageHeightPixels},1,");
        // PRINT: Prints the job
        byte[] printCommand = Encoding.ASCII.GetBytes("\r\nPRINT 1,1\r\n");

        using (MemoryStream stream = new MemoryStream())
        {
            stream.Write(sizeCommand, 0, sizeCommand.Length);
            stream.Write(clsCommand, 0, clsCommand.Length);
            stream.Write(bitmapCommandHeader, 0, bitmapCommandHeader.Length);
            stream.Write(bitmapData, 0, bitmapData.Length); // Append the raw bitmap pixel data
            stream.Write(printCommand, 0, printCommand.Length);
            stream.Position = 0;
            var bytes = stream.ToArray();
            return bytes;
        }
    }
    public static byte[] GeneratePrinterCommands(string text, string font, float fontSize, System.Windows.FontStyle fontStyle, System.Windows.FontWeight fontWeight
            , int x, int y, int imageWidthPixels, int imageHeightPixels, double angle)
    {
        // Calculate width in bytes (8 pixels per byte) for TSPL command
        int imageWidthBytes = (imageWidthPixels + 7) / 8;

        // Convert the bitmap to the required 1-bit monochrome format
        byte[] bitmapData = BitmapExtensions.ConvertToMonochromeBytes(text, imageWidthPixels, imageHeightPixels
            , new System.Drawing.FontFamily(font), fontSize, FontHelper.ConvertWpfToGdiFontStyle(fontStyle, fontWeight), angle);
        byte[] clsCommand = Encoding.ASCII.GetBytes("\r\n");
        // TSPL commands for printing a bitmap
        // BITMAP: X_START, Y_START, WIDTH_IN_BYTES, HEIGHT_IN_PIXELS, MODE(1=OVERWRITE), DATA
        byte[] bitmapCommandHeader = Encoding.ASCII.GetBytes($"BITMAP {x},{y},{imageWidthBytes},{imageHeightPixels},1,");

        using (MemoryStream stream = new MemoryStream())
        {
            stream.Write(clsCommand, 0, clsCommand.Length);
            stream.Write(bitmapCommandHeader, 0, bitmapCommandHeader.Length);
            stream.Write(bitmapData, 0, bitmapData.Length); // Append the raw bitmap pixel data
            stream.Position = 0;
            var bytes = stream.ToArray();
            return bytes;
        }
    }
    public static (int,byte[]) RenderTextBoxesOnBitmap(List<System.Windows.Controls.TextBox> list, double width, double height, double fX,double fY,float DPIX, float DPIY)
    {
        width = (int)width*fX;
        height = (int)height*fY;
        using (Bitmap monochromeBitmap = new Bitmap((int)width, (int)height, System.Drawing.Imaging.PixelFormat.Format32bppRgb))
        {
            monochromeBitmap.SetResolution(DPIX, DPIY);
            using (Graphics graphics = Graphics.FromImage(monochromeBitmap))
            {
                graphics.PageUnit = GraphicsUnit.Pixel;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
                // Fill the background with white (important for 1bpp)
                graphics.FillRectangle(Brushes.White, 0, 0, (int)width, (int)height);
                var center = new System.Drawing.PointF((float)(width / 2f), (float)(height / 2f));
                StringFormat format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    FormatFlags = StringFormatFlags.LineLimit
                };

                foreach (var textBox in list)
                {
                    System.Drawing.Font font = new System.Drawing.Font(textBox.FontFamily.Source, (float)textBox.FontSize, FontHelper.ConvertWpfToGdiFontStyle(textBox.FontStyle, textBox.FontWeight));
                    var size = graphics.MeasureString(textBox.Text, font);
                    if (size.Width> textBox.Width*fX || size.Height > textBox.Height*fY)
                    {
                        // Scale down font size to fit within TextBox dimensions
                        float scaleX = (float)(textBox.Width*fX / size.Width);
                        float scaleY = (float)(textBox.Height*fY / size.Height);
                        float scale = Math.Min(scaleX, scaleY);
                        float newFontSize = (float)textBox.FontSize * scale;
                        font.Dispose();
                        font = new System.Drawing.Font(textBox.FontFamily.Source, newFontSize, FontHelper.ConvertWpfToGdiFontStyle(textBox.FontStyle, textBox.FontWeight));
                        size = graphics.MeasureString(textBox.Text, font);
                    }

                    var x = Canvas.GetLeft(textBox)*fX;
                    var y = Canvas.GetTop(textBox)*fY;
                    var origin = new System.Drawing.PointF((float)x,(float) y);
                    var angle = (textBox.RenderTransform as RotateTransform).Angle;
                    if (angle != 0)
                    {
                        graphics.TranslateTransform(origin.X + size.Width / 2, origin.Y + size.Height / 2);
                        graphics.RotateTransform((float)angle);
                        graphics.TranslateTransform(-(origin.X + size.Width / 2), -(origin.Y + size.Height / 2));
                    }
                    graphics.DrawString(textBox.Text.Trim(), font, Brushes.Black, origin);
                    graphics.ResetTransform();
                }
            }
            var bmp = BitmapHelper.ToBitmapSource(monochromeBitmap);
            var bmpSource = DitherConverter.ConvertTo1bpp(bmp);
            var bitmap1bpp = BitmapHelper.BitmapSourceToBitmap(bmpSource);
            bitmap1bpp.Save(@"f:\output.bmp", System.Drawing.Imaging.ImageFormat.Bmp);
            // Now, lock the bits of the monochrome bitmap to get the raw pixel data.
            BitmapData bmpData = new();
            byte[] data;
            int stride;
            int dataSize;
            try
            {
                bmpData = monochromeBitmap.LockBits(
                    new System.Drawing.Rectangle(0, 0, (int)width, (int)height),
                    ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format1bppIndexed); // Specify the target format

                // Calculate the size of the data array. Each row's stride might be padded.
                stride = bmpData.Stride;
                dataSize = stride *(int) height;
                data = new byte[dataSize];

                // Copy the pixel data from the bitmap to our byte array.
                Marshal.Copy(bmpData.Scan0, data, 0, dataSize);
            }
            finally
            {
                monochromeBitmap.UnlockBits(bmpData);
            }
            var bmp1bpp = BitmapExtensions.CreateBitmapFromData(data,(int)width,(int)height, System.Drawing.Imaging.PixelFormat.Format1bppIndexed, stride);
            int widthBytes;
            byte[] packedData = BitmapExtensions.GetPackedDataFrom1bppBitmap(bmp1bpp,out widthBytes);
            return (widthBytes,packedData);

        }
    }
}



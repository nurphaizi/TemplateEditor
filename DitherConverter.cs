using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TemplateEdit;

public static class DitherConverter
{
    /// <summary>
    /// Converts a BitmapSource to a 1bpp indexed bitmap using the Floyd-Steinberg dithering algorithm.
    /// </summary>
    /// <param name="source">The source image to convert.</param>
    /// <returns>A high-quality 1bpp dithered BitmapSource.</returns>
    /// 
    public static BitmapSource ConvertTo1bpp(BitmapSource source)
    {
        // 1. Convert the source image to a standardized grayscale format (Gray8)
        // This simplifies the dithering logic by ensuring we're always working with 8-bit gray values.
        var grayscaleSource = new FormatConvertedBitmap(source, PixelFormats.Gray8, null, 0);

        int width = grayscaleSource.PixelWidth;
        int height = grayscaleSource.PixelHeight;
        int stride = (width + 7) / 8; // Stride for 1bpp is bits, so we calculate bytes.

        // 2. Prepare pixel data arrays
        // We need a floating-point array to accurately accumulate the error diffusion.
        float[] pixelDataFloat = new float[width * height];
        byte[] sourcePixels = new byte[width * height];

        // Copy the 8-bit grayscale pixel data into our float array.
        grayscaleSource.CopyPixels(sourcePixels, width, 0);
        for (int i = 0; i < sourcePixels.Length; i++)
        {
            pixelDataFloat[i] = sourcePixels[i];
        }

        // 3. Perform Floyd-Steinberg Dithering
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int currentIndex = y * width + x;
                float oldPixel = pixelDataFloat[currentIndex];

                // Snap the pixel to the nearest color (0 for black, 255 for white)
                float newPixel = oldPixel > 128 ? 255 : 0;
                pixelDataFloat[currentIndex] = newPixel;

                // Calculate the quantization error
                float quantError = oldPixel - newPixel;

                // Distribute the error to neighboring pixels
                // Right
                if (x + 1 < width)
                {
                    pixelDataFloat[currentIndex + 1] += quantError * 7 / 16;
                }
                // Down-Left
                if (x - 1 >= 0 && y + 1 < height)
                {
                    pixelDataFloat[currentIndex + width - 1] += quantError * 3 / 16;
                }
                // Down
                if (y + 1 < height)
                {
                    pixelDataFloat[currentIndex + width] += quantError * 5 / 16;
                }
                // Down-Right
                if (x + 1 < width && y + 1 < height)
                {
                    pixelDataFloat[currentIndex + width + 1] += quantError * 1 / 16;
                }
            }
        }

        // 4. Create the final 1bpp Bitmap
        byte[] finalPixels = new byte[stride * height];

        // Pack the dithered float data into the 1bpp byte array.
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                // If the pixel is white (255), set the corresponding bit.
                if (pixelDataFloat[index] == 255)
                {
                    int byteIndex = y * stride + (x / 8);
                    byte bitMask = (byte)(128 >> (x % 8));
                    finalPixels[byteIndex] |= bitMask;
                }
            }
        }

        // Create the BitmapSource from the 1bpp pixel data.
        return BitmapSource.Create(width, height, 96, 96, PixelFormats.BlackWhite, null, finalPixels, stride);
    }
}
public static class BitmapHelper
{
    /// <summary>
    /// Converts a System.Drawing.Bitmap to a System.Windows.Media.Imaging.BitmapSource.
    /// </summary>
    /// <param name="bitmap">The System.Drawing.Bitmap to convert.</param>
    /// <returns>A WPF-compatible BitmapSource.</returns>
    public static System.Windows.Media.Imaging.BitmapSource ToBitmapSource(this Bitmap bitmap)
    {
        // Get the Hbitmap handle for the GDI+ bitmap.
        IntPtr hBitmap = bitmap.GetHbitmap();

        try
        {
            // Create a BitmapSource from the HBitmap.
            // The Imaging.CreateBitmapSourceFromHBitmap method is the key to this conversion.
            return Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }
        finally
        {
            // IMPORTANT: Release the HBitmap to avoid memory leaks.
            DeleteObject(hBitmap);
        }
    }

    // P/Invoke to release the GDI+ bitmap handle.
    [DllImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject([In] IntPtr hObject);

    public static System.Drawing.Bitmap BitmapSourceToBitmap(BitmapSource source)
    {
        using (var stream = new MemoryStream())
        {
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));
            encoder.Save(stream);

            stream.Seek(0, SeekOrigin.Begin);

            // The Bitmap constructor takes ownership of the stream in some cases,
            // so to be safe, we can create it this way.
            return new System.Drawing.Bitmap(stream);
        }
    }
}

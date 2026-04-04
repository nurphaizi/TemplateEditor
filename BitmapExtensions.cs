using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Xceed.Wpf.Toolkit.Core.Converters;
using Brushes = System.Drawing.Brushes;

namespace TemplateEdit;

public static class BitmapExtensions
{
    public static byte[] RenderTargetBitmapToPngBytes(RenderTargetBitmap rtb)
    {
        PngBitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));

        using (MemoryStream ms = new MemoryStream())
        {
            encoder.Save(ms);
            return ms.ToArray();
        }
    }
    public static void Thicken(byte[] src, byte[] dst, int width, int height)
    {
        int widthBytes = (width + 7) / 8;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * widthBytes + (x >> 3);
                byte mask = (byte)(0x80 >> (x & 7));

                if ((src[index] & mask) != 0)
                {
                    SetPixel(dst, widthBytes, x, y);
                    SetPixel(dst, widthBytes, x + 1, y);
                    SetPixel(dst, widthBytes, x - 1, y);
                    SetPixel(dst, widthBytes, x, y + 1);
                    SetPixel(dst, widthBytes, x, y - 1);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetPixel(byte[] buffer, int widthBytes, int x, int y)
    {
        int index = y * widthBytes + (x >> 3);
        if(index < 0 || index >= buffer.Length)
            return; // Out of bounds check
        buffer[index] |= (byte)(0x80 >> (x & 7));
    }
    public static void MergeBitmap(
     byte[] label,
     int labelWidthBytes,
     byte[] image,
     int imageWidthBytes,
     int imageHeight,
     int posX,
     int posY)
    {
        for (int y = 0; y < imageHeight; y++)
        {
            for (int xb = 0; xb < imageWidthBytes; xb++)
            {
                int dst = (posY + y) * labelWidthBytes + (posX >> 3) + xb;
                int src = y * imageWidthBytes + xb;

                label[dst] |= image[src]; // OR merge
            }
        }
    }
    public static void DrawDLine(byte[] buffer, int widthBytes,
              int x0, int y0, int x1, int y1,
              int thickness)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);

        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;

        int err = dx - dy;

        while (true)
        {
            // draw thickness around the line
            for (int t = -thickness / 2; t <= thickness / 2; t++)
            {
                if (dx > dy)
                    SetPixel(buffer, widthBytes, x0, y0 + t); // mostly horizontal
                else
                    SetPixel(buffer, widthBytes, x0 + t, y0); // mostly vertical
            }

            if (x0 == x1 && y0 == y1)
                break;

            int e2 = err << 1;

            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }
    public static void DrawDLine(byte[] buffer, int widthBytes,
              int x0, int y0, int x1, int y1)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);

        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;

        int err = dx - dy;

        while (true)
        {
            int index = y0 * widthBytes + (x0 >> 3);
            buffer[index] |= (byte)(0x80 >> (x0 & 7));

            if (x0 == x1 && y0 == y1)
                break;

            int e2 = err <<1;

            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }

    }
    public static void DrawLine(byte[] buffer, int widthBytes, int x1, int y1, int x2, int y2)
    {
        var k = (y2 - y1) / (double)(x2 - x1);

        for (int x = x1; x <= x2; x++)
        {
            var y = y1 + (int)(k * (x - x1));
            int index = (y - y1) * widthBytes + ((x - x1) >> 3);
            buffer[index] |= (byte)(0x80 >> (x & 7));
        }
    }

    public static void DrawThickLine(byte[] buf, int widthBytes,
                   int x0, int y0, int x1, int y1, int t)
    {
        for (int i = 0; i < t; i++)
            DrawLine(buf, widthBytes, x0, y0 + i, x1, y1 + i);
    }
    public static void DrawHLine(byte[] buffer, int widthBytes, int x1, int x2, int y)
    {
            for (int x = x1; x <= x2; x++)
            {
                int index = y * widthBytes + (x >> 3);
                buffer[index] |= (byte)(0x80 >> (x & 7));
            }
    }
    public static void DrawVLine(byte[] buffer, int widthBytes, int x, int y1, int y2)
    {
        byte mask = (byte)(0x80 >> (x & 7));

        for (int y = y1; y <= y2; y++)
        {
            int index = y * widthBytes + (x >> 3);
            buffer[index] |= mask;
        }
    }
    public static Bitmap To1bppIndexed(this Bitmap sourceBitmap, byte threshold = 128)
    {
        if (sourceBitmap == null)
            throw new ArgumentNullException(nameof(sourceBitmap));

        int width = sourceBitmap.Width;
        int height = sourceBitmap.Height;
        // 1. Create the new 1bpp indexed Bitmap
        Bitmap targetBitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format1bppIndexed);

        // 2. Set a simple Black and White palette (0=Black, 1=White)
        ColorPalette palette = targetBitmap.Palette;
        palette.Entries[0] = System.Drawing.Color.FromArgb(0, 0, 0);   // Black
        palette.Entries[1] = System.Drawing.Color.FromArgb(255, 255, 255); // White
        targetBitmap.Palette = palette;

        // Lock the bits of both bitmaps
        Rectangle rect = new Rectangle(0, 0, width, height);

        // Source: Assuming Format32bppRgb (or 32bppArgb) for simplicity, which is 4 bytes per pixel (B, G, R, A)
        // Format32bppRgb is often stored internally as Format32bppArgb anyway, so we lock to Format32bppArgb
        BitmapData sourceData = sourceBitmap.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        // Target: 1bpp Indexed
        BitmapData targetData = targetBitmap.LockBits(rect, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format1bppIndexed);

        try
        {
            int sourceStride = sourceData.Stride;
            int targetStride = targetData.Stride;

            // Pointers to the start of the pixel data
            IntPtr sourceScan0 = sourceData.Scan0;
            IntPtr targetScan0 = targetData.Scan0;

            // Pointers to work with
            unsafe
            {
                byte* sourcePtr = (byte*)sourceScan0.ToPointer();
                byte* targetPtr = (byte*)targetScan0.ToPointer();

                for (int y = 0; y < height; y++)
                {
                    // Calculate start of the current row
                    byte* sourceLine = sourcePtr + y * sourceStride;
                    byte* targetLine = targetPtr + y * targetStride;

                    for (int x = 0; x < width; x++)
                    {
                        // Get the BGR components from the 32bpp source pixel
                        int pixelIndex = x * 4; // 4 bytes per pixel (B, G, R, A)
                        byte blue = sourceLine[pixelIndex];
                        byte green = sourceLine[pixelIndex + 1];
                        byte red = sourceLine[pixelIndex + 2];

                        // Calculate *perceived* luminance (brightness) for the pixel
                        // Formula: 0.299*R + 0.587*G + 0.114*B (standard for color-to-grayscale)
                        byte brightness = (byte)((red * 0.299 + green * 0.587 + blue * 0.114));

                        // 4. Apply a threshold for Black (0) or White (1)
                        // If brightness is above the threshold, the pixel is White (1), otherwise Black (0)
                        bool isWhite = brightness > threshold;

                        // 5. Write the single bit (0 or 1) to the target 1bpp memory
                        int byteIndex = x / 8; // Current byte index in the row
                        int bitPosition = 7 - (x % 8); // Position within the byte (MSB first)

                        if (isWhite)
                        {
                            // Set the bit to 1 (White)
                            targetLine[byteIndex] |= (byte)(1 << bitPosition);
                        }
                        else
                        {
                            // Set the bit to 0 (Black) - technically already 0, but this is the formal way to clear it
                            targetLine[byteIndex] &= (byte)~(1 << bitPosition);
                        }
                    }
                }
            }
        }
        finally
        {
            // Unlock the bits
            sourceBitmap.UnlockBits(sourceData);
            targetBitmap.UnlockBits(targetData);
        }

        return targetBitmap;
    }
    public static void ConvertImage(string inputPath, string outputPath)
    {
        try
        {
            // Load the image (it will be loaded into an appropriate format, often Format32bppArgb)
            using (Bitmap originalBitmap = new Bitmap(inputPath))
            {
                // The extension method handles the conversion logic
                using (Bitmap monochromeBitmap = originalBitmap.To1bppIndexed(threshold: 150)) // Use a threshold of 150
                {
                    // Save the 1bpp indexed image. BMP or TIFF is recommended for preserving 1bpp exactly.
                    monochromeBitmap.Save(outputPath, ImageFormat.Bmp);

                    System.Windows.MessageBox.Show($"Conversion successful. Saved to {outputPath}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"An error occurred: {ex.Message}");
        }
    }
    public static byte[] GetPackedDataFrom1bppBitmap(Bitmap bitmap, out int widthBytes)
    {
        if (bitmap.PixelFormat != System.Drawing.Imaging.PixelFormat.Format1bppIndexed)
            throw new ArgumentException("Bitmap must be in Format1bppIndexed");

        widthBytes = (bitmap.Width + 7) / 8;
        int height = bitmap.Height;

        BitmapData bmpData = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format1bppIndexed);

        int stride = bmpData.Stride;
        byte[] packedData = new byte[widthBytes * height];

        unsafe
        {
            byte* scan0 = (byte*)bmpData.Scan0.ToPointer();

            for (int y = 0; y < height; y++)
            {
                byte* row = scan0 + y * stride;
                int dstOffset = y * widthBytes;

                for (int x = 0; x < widthBytes; x++)
                {
                    packedData[dstOffset + x] = row[x];
                }
            }
        }

        bitmap.UnlockBits(bmpData);
        return packedData;
    }
    public static Bitmap CreateBitmapFromData(byte[] pixelData, int width, int height, System.Drawing.Imaging.PixelFormat pixelFormat, int stride)
    {
        GCHandle handle = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
        IntPtr ptr = handle.AddrOfPinnedObject();

        Bitmap bitmap = new Bitmap(width, height, stride, pixelFormat, ptr);

        // Optional: clone to detach from pinned memory
        Bitmap clone = bitmap.Clone(new Rectangle(0, 0, width, height), pixelFormat);

        bitmap.Dispose();
        handle.Free();

        return clone;
    }
    public static byte[] ConvertToMonochromeBytes(Bitmap sourceBitmap, int width, int height)
    {
        // Create a temporary bitmap with the desired 1bpp indexed format.
        // We first draw the source image onto this, then extract the raw bits.

        using (Bitmap monochromeBitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppRgb))
        {
            // IMPORTANT: Use the Graphics object to draw onto a NON-INDEXED format first,
            // then convert. Or, draw directly to the 1bpp format from a higher format.
            // The following approach directly draws to the 1bpp format using dithering.

            // Create a Graphics object to draw on the monochrome bitmap.
            using (Graphics graphics = Graphics.FromImage(monochromeBitmap))
            {
                // Set interpolation mode for better quality when resizing/drawing
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
                // Fill the background with white (important for 1bpp)
                graphics.FillRectangle(Brushes.White, 0, 0, width, height);
                graphics.DrawImage(sourceBitmap, new Rectangle(0, 0, width, height));
            }

            // Now, lock the bits of the monochrome bitmap to get the raw pixel data.
            BitmapData bmpData = new();
            byte[] data;
            int stride;
            int dataSize;
            try
            {
                bmpData = monochromeBitmap.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format1bppIndexed); // Specify the target format

                // Calculate the size of the data array. Each row's stride might be padded.
                stride = bmpData.Stride;
                dataSize = stride * height;
                data = new byte[dataSize];

                // Copy the pixel data from the bitmap to our byte array.
                Marshal.Copy(bmpData.Scan0, data, 0, dataSize);
            }
            finally
            {
                monochromeBitmap.UnlockBits(bmpData);
            }
            var bmp1bpp = BitmapExtensions.CreateBitmapFromData(data, width, height, System.Drawing.Imaging.PixelFormat.Format1bppIndexed, stride);
            int widthBytes;
            byte[] packedData = BitmapExtensions.GetPackedDataFrom1bppBitmap(bmp1bpp, out widthBytes);
            return packedData;
        }
    }

    public static byte[] ConvertToMonochromeBytes(string text, int width, int height, System.Drawing.FontFamily font, float fontSize, System.Drawing.FontStyle fontStyle, double angle)
    {
        // Create a temporary bitmap with the desired 1bpp indexed format.
        // We first draw the source image onto this, then extract the raw bits.

        using (Bitmap monochromeBitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppRgb))
        {
            // IMPORTANT: Use the Graphics object to draw onto a NON-INDEXED format first,
            // then convert. Or, draw directly to the 1bpp format from a higher format.
            // The following approach directly draws to the 1bpp format using dithering.

            // Create a Graphics object to draw on the monochrome bitmap.
            using (Graphics graphics = Graphics.FromImage(monochromeBitmap))
            {
                // Set interpolation mode for better quality when resizing/drawing
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
                // Fill the background with white (important for 1bpp)
                graphics.FillRectangle(Brushes.White, 0, 0, width, height);
                var size = graphics.MeasureString(text, System.Drawing.SystemFonts.DefaultFont);
                using System.Drawing.Font f = new System.Drawing.Font(font.Name, fontSize, fontStyle);
                graphics.DrawString(text, f, Brushes.Black, new PointF((width - size.Width) / 2, (height - size.Height) / 2));
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
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format1bppIndexed); // Specify the target format

                // Calculate the size of the data array. Each row's stride might be padded.
                stride = bmpData.Stride;
                dataSize = stride * height;
                data = new byte[dataSize];

                // Copy the pixel data from the bitmap to our byte array.
                Marshal.Copy(bmpData.Scan0, data, 0, dataSize);
            }
            finally
            {
                monochromeBitmap.UnlockBits(bmpData);
            }
            var bmp1bpp = BitmapExtensions.CreateBitmapFromData(data, width, height, System.Drawing.Imaging.PixelFormat.Format1bppIndexed, stride);
            int widthBytes;
            byte[] packedData = BitmapExtensions.GetPackedDataFrom1bppBitmap(bmp1bpp, out widthBytes);
            return packedData;
        }
    }
    public static System.Drawing.FontStyle ConvertFontStyle(System.Windows.FontStyle wpfStyle)
    {
        if (wpfStyle == FontStyles.Italic || wpfStyle == FontStyles.Oblique)
            return System.Drawing.FontStyle.Italic;
        else
            return System.Drawing.FontStyle.Regular;
    }
    public static Bitmap ResampleBitmap(Bitmap source, float targetDpiX, float targetDpiY)
    {
        float scaleX = targetDpiX / source.HorizontalResolution;
        float scaleY = targetDpiY / source.VerticalResolution;

        int newWidth = (int)(source.Width * scaleX);
        int newHeight = (int)(source.Height * scaleY);

        Bitmap resampled = new Bitmap(newWidth, newHeight);
        resampled.SetResolution(targetDpiX, targetDpiY);

        using (Graphics g = Graphics.FromImage(resampled))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.DrawImage(source, 0, 0, newWidth, newHeight);
        }

        return resampled;
    }
    public static Bitmap ErrorMessageBitmap(int width, int height, string message)
    {
        Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.Clear(System.Drawing.Color.White);
            g.DrawRectangle(System.Drawing.Pens.Red, 0, 0, width - 1, height - 1);
            using Font font = new Font("Arial", 8);
            SizeF textSize = g.MeasureString(message, font);
            g.DrawString(message, font, Brushes.Red, (width - textSize.Width) / 2, (height - textSize.Height) / 2);
        }
        return bitmap;
    }

    public static void SaveBitmapImageToFile(BitmapImage bitmap, string filePath)
    {
        if (bitmap == null)
            throw new ArgumentNullException(nameof(bitmap));

        // Ensure cross-thread safety
        if (bitmap.CanFreeze && !bitmap.IsFrozen)
            bitmap.Freeze();

        BitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using (var stream = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None))
        {
            encoder.Save(stream);
        }
    }
    public static BitmapSource ScaleHighQuality(BitmapSource source, double dpiX,double dpiY=0)
    {
        if(dpiY == 0)
        {
            dpiY = dpiX;
        }
        var width = (int)(source.PixelWidth * dpiX /source.DpiX);
        var height = (int)(source.PixelHeight * dpiY / source.DpiY);
        var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
            width,
            height,
            dpiX,
            dpiY,
            PixelFormats.Pbgra32);

        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            dc.DrawRectangle(new SolidColorBrush(Colors.White), null, new Rect(0, 0, width, height));
            dc.DrawImage(source, new Rect(0, 0, width, height));
        }
        rtb.Render(dv);
        rtb.Freeze();

        return rtb;
    }
    public static BitmapSource Resize(BitmapSource source, int width, int height)
    {
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            dc.DrawImage(source, new Rect(0, 0, width, height));
        }

        var bmp = new RenderTargetBitmap(
            width, height, 203, 203,
            PixelFormats.Pbgra32);

        bmp.Render(dv);
        bmp.Freeze();
        return bmp;
    }

    public static BitmapSource PngBytesToBitmapSourceNormalized(byte[] pngBytes)
    {
        if (pngBytes == null || pngBytes.Length == 0)
            return null;

        using var ms = new MemoryStream(pngBytes);

        var decoder = BitmapDecoder.Create(
            ms,
            BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.OnLoad); // VERY IMPORTANT

        var frame = decoder.Frames[0];
        frame.Freeze(); // makes it cross-thread safe

        return frame;
    }

    public static BitmapSource InvertBeforeThreshold(BitmapSource source,int dpi=96)
    {
        // Ensure grayscale format
        var gray = new FormatConvertedBitmap(
            source,
            System.Windows.Media.PixelFormats.Gray8,
            null,
            0);

        int width = gray.PixelWidth;
        int height = gray.PixelHeight;
        int stride = width;

        byte[] pixels = new byte[height * stride];
        gray.CopyPixels(pixels, stride, 0);

        // Invert grayscale
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = (byte)(255 - pixels[i]);

        var wb = new WriteableBitmap(
            width,
            height,
            dpi,
            dpi,
            System.Windows.Media.PixelFormats.Gray8,
            null);

        wb.WritePixels(
            new System.Windows.Int32Rect(0, 0, width, height),
            pixels,
            stride,
            0);

        wb.Freeze(); // thread-safe
        return wb;
    }

public static void SaveRenderTargetBitmap(RenderTargetBitmap rtb, string filePath)
{
    PngBitmapEncoder encoder = new PngBitmapEncoder();
    encoder.Frames.Add(BitmapFrame.Create(rtb));

    using (FileStream fs = new FileStream(filePath, FileMode.Create))
    {
        encoder.Save(fs);
    }
}

    public static void SaveFormatConvertedBitmap(FormatConvertedBitmap bitmap, string path)
    {
        using (FileStream stream = new FileStream(path, FileMode.Create))
        {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(stream);
        }
    }
    public static void SaveBitmapSource(BitmapSource bitmap, string filePath)
    {
        BitmapEncoder encoder = new PngBitmapEncoder(); // Change if needed

        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using (FileStream stream = new FileStream(filePath, FileMode.Create))
        {
            encoder.Save(stream);
        }
    }
}
 
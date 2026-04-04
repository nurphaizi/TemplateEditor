using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using SkiaSharp;
using Point = System.Windows.Point;
namespace TemplateEdit;
public static class ColorExtensions
{
    public static System.Drawing.Color ToDrawingColor(this Color color)
        => System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);

    public static Color ToMediaColor(this System.Drawing.Color color)
        => Color.FromArgb(color.A, color.R, color.G, color.B);
}

public class TextToBitmapConverter
{
    public static Bitmap ConvertTextToBitmap(string text, Font font, Color textColor, Color backgroundColor, int padding)
    {
        // 1. Create a dummy bitmap and graphics object to measure the text size.
        // This is a common and necessary trick to accurately determine the required image dimensions.
        Bitmap dummyBitmap = new Bitmap(1, 1);
        Graphics dummyGraphics = Graphics.FromImage(dummyBitmap);

        // Set rendering hints for accurate measurement.
        dummyGraphics.TextRenderingHint = TextRenderingHint.AntiAlias;

        // 2. Measure the size of the text with the specified font.
        SizeF textSize = dummyGraphics.MeasureString(text, font);

        // 3. Dispose of the dummy objects to free resources.
        dummyGraphics.Dispose();
        dummyBitmap.Dispose();

        // 4. Create a new bitmap with the correct size, plus padding.
        int width = (int)Math.Ceiling(textSize.Width) + padding * 2;
        int height = (int)Math.Ceiling(textSize.Height) + padding * 2;

        Bitmap finalBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

        // 5. Create a Graphics object from the final bitmap.
        Graphics finalGraphics = Graphics.FromImage(finalBitmap);

        // Set high-quality rendering properties for the final output.
        finalGraphics.SmoothingMode = SmoothingMode.AntiAlias;
        finalGraphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        finalGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

        // 6. Fill the background with the specified color.
        finalGraphics.Clear(backgroundColor);

        // 7. Draw the text onto the bitmap.
        using (Brush textBrush = new SolidBrush(textColor))
        {
            finalGraphics.DrawString(text, font, textBrush, padding, padding);
        }
        // 8. Flush all drawing operations and dispose of the graphics object.
        finalGraphics.Flush();
        finalGraphics.Dispose();

        return finalBitmap;
    }

    public static void Test(string text)
    {
        Font myFont = new Font("Arial", 24, System.Drawing.FontStyle.Bold);
        Color textColor = Color.Black;
        Color backgroundColor = Color.White;
        int padding = 10;
        double angle = 90; // Rotate text by 90 degrees

        try
        {
            Bitmap textBitmap = ConvertTextToBitmap(text, myFont, textColor, backgroundColor, padding);

            // Save the bitmap to a file to verify the result.
            string filePath = "f:\\TextBitmap.png";
            textBitmap.Save(filePath, ImageFormat.Png);
            Console.WriteLine($"Bitmap saved to: {filePath}");

            // Clean up the bitmap object.
            textBitmap.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    public static class BitmapRotator
    {
        /// <summary>
        /// Rotates an SKBitmap by a given angle.
        /// </summary>
        /// <param name="sourceBitmap">The original bitmap to rotate.</param>
        /// <param name="angleDegrees">The angle in degrees to rotate (clockwise).</param>
        /// <returns>A new SKBitmap that is the rotated version of the source.</returns>
        public static SKBitmap RotateBitmap(SKBitmap sourceBitmap, float angleDegrees)
        {
            // Calculate the new dimensions of the bitmap after rotation.
            // This is important for angles other than multiples of 90 degrees.
            float radians = (float)Math.PI * angleDegrees / 180f;
            float cosine = (float)Math.Abs(Math.Cos(radians));
            float sine = (float)Math.Abs(Math.Sin(radians));

            int originalWidth = sourceBitmap.Width;
            int originalHeight = sourceBitmap.Height;

            int rotatedWidth = (int)(cosine * originalWidth + sine * originalHeight);
            int rotatedHeight = (int)(cosine * originalHeight + sine * originalWidth);

            // Create a new bitmap with the calculated dimensions.
            var rotatedBitmap = new SKBitmap(rotatedWidth, rotatedHeight);

            // Use an SKCanvas to draw the rotated image onto the new bitmap.
            using (var canvas = new SKCanvas(rotatedBitmap))
            {
                // Clear the canvas to white (or any background color you prefer).
                // This is important to avoid artifacts from previous drawings if the canvas is reused.
                canvas.Clear(SKColors.White);

                // Translate the canvas so the center of the *new* bitmap is the origin.
                canvas.Translate(rotatedWidth / 2f, rotatedHeight / 2f);

                // Rotate the canvas by the specified angle.
                canvas.RotateDegrees(angleDegrees);

                // Translate the canvas back so that the original bitmap's center aligns 
                // with the canvas's origin *before* rotation. This ensures rotation around the center.
                canvas.Translate(-originalWidth / 2f, -originalHeight / 2f);

                // Draw the original bitmap onto the canvas.
                canvas.DrawBitmap(sourceBitmap, 0, 0);
            }

            return rotatedBitmap;
        }

        // Example of how to use the RotateBitmap method:
        public static void ExampleUsage()
        {
            // Assume you have an SKBitmap named 'originalBitmap'
            // For demonstration, let's create a simple bitmap:
            var originalBitmap = new SKBitmap(200, 150);
            using (var canvas = new SKCanvas(originalBitmap))
            {
                canvas.Clear(SKColors.LightBlue);
                var paint = new SKPaint { Color = SKColors.DarkRed, Style = SKPaintStyle.Fill };
                canvas.DrawRect(20, 30, 100, 50, paint);
            }

            // Rotate the bitmap by 45 degrees clockwise
            float angleToRotate = 45f;
            SKBitmap rotatedBitmap = RotateBitmap(originalBitmap, angleToRotate);

            // Now you can save 'rotatedBitmap' to a file or use it further.
            // Example: Saving to a file
            // BitmapSaver.SaveBitmapToFile(rotatedBitmap, "rotated_image.png"); 
            // (Assuming you have a SaveBitmapToFile method as described previously)
        }
    }
    public static BitmapImage BitmapToBitmapImage(Bitmap bitmap)
    {
        using var memory = new MemoryStream();
        bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
        memory.Position = 0;

        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.StreamSource = memory;
        bitmapImage.EndInit();
        bitmapImage.Freeze(); // важно для WPF-потоков

        return bitmapImage;
    }

    public static byte[] BitmapToBytes(System.Drawing.Bitmap bitmap, ImageFormat? format = null)
    {
        if (bitmap == null)
            throw new ArgumentNullException(nameof(bitmap));

        format ??= ImageFormat.Png; // default: lossless PNG

        using var stream = new MemoryStream();
        bitmap.Save(stream, format);
        return stream.ToArray();
    }


}

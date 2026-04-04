using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SkiaSharp;
using Svg.Skia;

namespace TemplateEdit;

public static class SvgToTSPL
{
    public static byte[] BuildTSPLCommand(int x,int y,byte[] data, int widthBytes, int height)
    {
        List<byte> commandBytes = new List<byte>();
        string bitmapCmd =$"""

            BITMAP {x},{y},{widthBytes},{height},1,

            """;
        commandBytes.AddRange(Encoding.ASCII.GetBytes(bitmapCmd));
        commandBytes.AddRange(data);
       return commandBytes.ToArray();

    }
    public static byte[] GenerateTSPLBitmapCommand(string svgContent, int x, int y, int targetWidth, int targetHeight)
    {

        // Render SVG to SkiaSharp bitmap
        var svg = new Svg.Skia.SKSvg();
        svg.FromSvg(svgContent);
        var rect = svg.Picture.CullRect;
        var bitmap = new SKBitmap((int)rect.Width,(int)rect.Height);
        var bmpScaled = new SKBitmap(targetWidth, targetHeight);
        var result = bitmap.ScalePixels(bmpScaled, SKFilterQuality.High);
        var matrix = SKMatrix.CreateScale(
            (float)targetWidth / rect.Width,
            (float)targetHeight / rect.Height,
            0, 0);
        using (var canvas = new SKCanvas(bmpScaled))
        {
            canvas.Clear(SKColors.White);
            canvas.DrawPicture(svg.Picture,ref matrix);
        }

        // Convert to ImageSharp image
        using var image = new Image<L8>(bmpScaled.Width, bmpScaled.Height);
        for (int j = 0; j < bmpScaled.Height; j++)
        {
            for (int i = 0; i < bmpScaled.Width; i++)
            {
                var color = bmpScaled.GetPixel(i, j);
//                byte brightness = (byte)(0.299 * color.Red + 0.587 * color.Green + 0.114 * color.Blue);
                byte brightness = (byte)(0.299 * color.Red + 0.587 * color.Green + 0.114 * color.Blue);

                image[i, j] = new L8(brightness);
            }
        }
        image.SaveAsBmp("temp.bmp", new BmpEncoder());
        // Convert to 1-bit monochrome
        image.Mutate(x => x.BinaryThreshold(0.6f));
        // Generate hex data
        int widthBytes = (image.Width + 7) / 8;
        int height = image.Height;
        int width = image.Width;
        byte[] hexData = new byte[widthBytes*height];

        int threshold = 128;
        for (int yPos = 0; yPos < height; yPos++)
        {
            byte currentByte = 0;
            int bitIndex = 0;

            for (int xPos = 0; xPos < width; xPos++)
            {
                byte pixel = image[xPos, yPos].PackedValue;
                if (pixel > threshold)
                    currentByte |= (byte)((byte)1 << (7 - bitIndex));

                bitIndex++;
                if (bitIndex == 8 || xPos == width - 1)
                {
                    hexData[yPos * widthBytes + (xPos / 8)] = currentByte;
                    currentByte = 0;
                    bitIndex = 0;
                }
            }
        }
        return BuildTSPLCommand( x, y,  hexData,  widthBytes, height);
    }
    public static byte[] GenerateTSPLBitmapCommand(byte[] imageSource, int x, int y, int targetWidth, int targetHeight)
    {
        var skImage = SKImage.FromEncodedData(imageSource);
        var skBitmap = SKBitmap.FromImage(skImage);
        if (skBitmap == null)
            throw new ArgumentException("Invalid image data provided.");
        // Scale the bitmap to the target dimensions
        targetWidth = targetWidth / 8 * 8;
        var bmpScaled = skBitmap.Resize(new SKImageInfo(targetWidth, targetHeight), SKFilterQuality.High);

        var destRect = new SKRect(0, 0, bmpScaled.Width, bmpScaled.Height);
        // Convert to ImageSharp image
        using var image = new Image<L8>(bmpScaled.Width, bmpScaled.Height);
        for (int j = 0; j < bmpScaled.Height; j++)
        {
            for (int i = 0; i < bmpScaled.Width; i++)
            {
                var color = bmpScaled.GetPixel(i, j);
                //                byte brightness = (byte)(0.299 * color.Red + 0.587 * color.Green + 0.114 * color.Blue);
                byte brightness = (byte)(0.299 * color.Red + 0.587 * color.Green + 0.114 * color.Blue);

                image[i, j] = new L8(brightness);
            }
        }
        // Convert to 1-bit monochrome
        image.Mutate(x => x.BinaryThreshold(0.5f));
        // Generate hex data
         
        int widthBytes = image.Width/8;
        var bits = image.Width % 8;
        if (bits > 0)
        {
            widthBytes++;
        }
        int height = image.Height;
        int width = image.Width;
        byte[] hexData = new byte[widthBytes * height];
        int threshold = 128;
        for (int yPos = 0; yPos < height; yPos++)
        {
            byte currentByte = 0;
            int bitIndex = 0;
            for (int xPos = 0; xPos < width; xPos++)
            {
                byte pixel = image[xPos, yPos].PackedValue;
                if (pixel > threshold )
                    currentByte |= (byte)(1 << (7 - bitIndex));
                bitIndex++;
                if (bitIndex == 8 || xPos == width - 1)
                {
                    hexData[yPos * widthBytes + (xPos / 8)] = currentByte;
                    currentByte = 0;
                    bitIndex = 0;
                }
            }
        }

        return BuildTSPLCommand(x, y, hexData, widthBytes, height);
    }
    public static byte[] GenerateTSPLBitmapCommand(SKBitmap skBitmap, int x, int y, int targetWidth, int targetHeight)
    {
        // Scale the bitmap to the target dimensions
        targetWidth = targetWidth / 8 * 8;
        var targetSize = new SKSizeI(targetWidth, targetHeight);
        var sourceSize = new SKSizeI(skBitmap.Width, skBitmap.Height);
        // Assuming sourceSize and targetSize are SKSize or similar
        bool isUpscaling = targetSize.Width > sourceSize.Width || targetSize.Height > sourceSize.Height;

        SKSamplingOptions samplingOptions;
        if (isUpscaling)
        {
            
            samplingOptions = new SKSamplingOptions(SKCubicResampler.Mitchell); // For upscaling
        }
        else
        {
            samplingOptions = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear); // For downscaling
        }

        // Then use these samplingOptions in your drawing or scaling operation
        // e.g., canvas.DrawImageRect(image, ..., samplingOptions);
        // or bitmap.ScalePixels(..., samplingOptions);


        var bmpScaled = skBitmap.Resize(new SKImageInfo(targetWidth, targetHeight), samplingOptions) ;
        // Convert to ImageSharp image
        using var image = new Image<L8>(bmpScaled.Width, bmpScaled.Height);
        for (int j = 0; j < bmpScaled.Height; j++)
        {
            for (int i = 0; i < bmpScaled.Width; i++)
            {
                var color = bmpScaled.GetPixel(i, j);
                byte brightness = (byte)(0.299 * color.Red + 0.587 * color.Green + 0.114 * color.Blue);

                image[i, j] = new L8(brightness);
            }
        }
        // Convert to 1-bit monochrome
        image.Mutate(x => x.BinaryThreshold(0.5f));
        // Generate hex data
        image.SaveAsBmp(@"f:\temp2.bmp", new BmpEncoder());
        int widthBytes = image.Width / 8;
        var bits = image.Width % 8;
        if (bits > 0)
        {
            widthBytes++;
        }
        int height = image.Height;
        int width = image.Width;
        byte[] hexData = new byte[widthBytes * height];
        int threshold = 128;
        for (int yPos = 0; yPos < height; yPos++)
        {
            byte currentByte = 0;
            int bitIndex = 0;
            for (int xPos = 0; xPos < width; xPos++)
            {
                byte pixel = image[xPos, yPos].PackedValue;
                if (pixel > threshold)
                    currentByte |= (byte)(1 << (7 - bitIndex));
                bitIndex++;
                if (bitIndex == 8 || xPos == width - 1)
                {
                    hexData[yPos * widthBytes + (xPos / 8)] = currentByte;
                    currentByte = 0;
                    bitIndex = 0;
                }
            }
        }
        return BuildTSPLCommand(x, y, hexData, widthBytes, height);
    }
    public static SKBitmap ConvertToSKBitmap(Bitmap bitmap)
    {
        using (var stream = new MemoryStream())
        {
            bitmap.Save(stream, ImageFormat.Png); // Save to stream
            stream.Seek(0, SeekOrigin.Begin);     // Reset stream position
            return SKBitmap.Decode(stream);       // Decode into SKBitmap
        }
    }

  

}

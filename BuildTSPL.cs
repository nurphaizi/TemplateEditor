using System;
using System.Text;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

class TSPLHexGenerator
{
    static void Main()
    {
        string imagePath = "input.png"; // Any format supported by ImageSharp
        int threshold = 128;

        using Image<L8> image = Image.Load<L8>(imagePath);
        int width = image.Width;
        int height = image.Height;
        int widthBytes = (width + 7) / 8;

        byte[] hexData = new byte[widthBytes * height];

        for (int y = 0; y < height; y++)
        {
            byte currentByte = 0;
            int bitIndex = 0;

            for (int x = 0; x < width; x++)
            {
                byte pixel = image[x, y].PackedValue;
                if (pixel < threshold)
                    currentByte |= (byte)(1 << (7 - bitIndex));

                bitIndex++;
                if (bitIndex == 8 || x == width - 1)
                {
                    hexData[y * widthBytes + (x / 8)] = currentByte;
                    currentByte = 0;
                    bitIndex = 0;
                }
            }
        }

        string tspl = BuildTSPL(hexData, widthBytes, height);
        File.WriteAllText("tspl_output.txt", tspl);
        Console.WriteLine("TSPL hex command saved to tspl_output.txt");
    }

    static string BuildTSPL(byte[] data, int widthBytes, int height)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("SIZE 60 mm, 40 mm");
        sb.AppendLine("GAP 3 mm, 0 mm");
        sb.AppendLine("CLS");
        sb.AppendLine($"BITMAP 100,100,{widthBytes},{height},1,");
        sb.AppendLine("{");

        for (int i = 0; i < data.Length; i++)
        {
            sb.Append("0x" + data[i].ToString("X2"));
            if (i < data.Length - 1) sb.Append(", ");
            if ((i + 1) % widthBytes == 0) sb.AppendLine();
        }

        sb.AppendLine("}");
        sb.AppendLine("PRINT 1");
        return sb.ToString();
    }
}

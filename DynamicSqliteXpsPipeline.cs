using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Net.Http;
using System.Printing;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using Serilog;
using Xceed.Wpf.Toolkit.Core.Converters;
using ZXing.QrCode.Internal;
namespace TemplateEdit;

using System.Drawing.Imaging;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SixLabors.ImageSharp.Memory;
using ZXing;
using Brush=System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;



public static class TsplBitmapConverter
{
    public static void SaveTsplBitmapToPng(byte[] bitmapData, int widthBytes,int height,string outputPath)
    {
        int widthPixels = widthBytes * 8;


        using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(widthPixels, height, PixelFormat.Format24bppRgb))
        {

            int byteIndex = 0;

            try
            {
                for (int y = 0; y < height; y++)
                {
                    for (int xb = 0; xb < widthBytes; xb++)
                    {
                        byte b = bitmapData[byteIndex++];

                        for (int bit = 0; bit < 8; bit++)
                        {
                            bool isBlack = (b & (1 << (7 - bit))) != 0;
                            int x = xb * 8 + bit;

                            if (x < widthPixels)
                                bmp.SetPixel(x, y, isBlack ? System.Drawing.Color.Black : System.Drawing.Color.White);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing bitmap data");
                throw;
            }

            bmp.Save(outputPath, ImageFormat.Png);
        }
    }

    public static void SaveSingleLine(Line line, string filePath)
    {
        Rect bounds = VisualTreeHelper.GetDescendantBounds(line);

        RenderTargetBitmap rtb = new RenderTargetBitmap(
            (int)bounds.Width,
            (int)bounds.Height,
            96,
            96,
            PixelFormats.Pbgra32);

        DrawingVisual dv = new DrawingVisual();
        using (DrawingContext dc = dv.RenderOpen())
        {
            VisualBrush vb = new VisualBrush(line);
            dc.DrawRectangle(vb, null, new Rect(new Point(), bounds.Size));
        }

        rtb.Render(dv);

        PngBitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));

        using (FileStream fs = new FileStream(filePath, FileMode.Create))
        {
            encoder.Save(fs);
        }
    }

    const int PrinterDpi = 203;
    const int MaxWidthDots = 576;
    const int threshold = 128;
    // XPRINTER 365B typical max width
    public static void SaveTsplBitmapToPng(string tsplBitmapCommand, string outputPath)
    {
        // Extract data after BITMAP
        var i = tsplBitmapCommand.IndexOf("BITMAP");
        if (i >= 0)
        {
            tsplBitmapCommand = tsplBitmapCommand.Substring(i);
        }
        var parts = tsplBitmapCommand.Replace("BITMAP", "").Trim().Split(',');
        int widthBytes = int.Parse(parts[2]);
        int height = int.Parse(parts[3]);


        int widthPixels = widthBytes * 8;

        i = tsplBitmapCommand.IndexOf(parts[5]);
        if (i >= 0)
        {
            tsplBitmapCommand = tsplBitmapCommand.Substring(i);
        }


        byte[] bitmapData = Encoding.GetEncoding(1251).GetBytes(tsplBitmapCommand);  // HexStringToByteArray(hexData);


        using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(widthPixels, height, PixelFormat.Format24bppRgb))
        {
            int byteIndex = 0;
            try
            {
                for (int y = 0; y < height; y++)
                {
                    for (int xb = 0; xb < widthBytes; xb++)
                    {
                        byte b = bitmapData[byteIndex++];

                        for (int bit = 0; bit < 8; bit++)
                        {
                            bool isBlack = (b & (1 << (7 - bit))) != 0;
                            int x = xb * 8 + bit;

                            if (x < widthPixels)
                                bmp.SetPixel(x, y, isBlack ? System.Drawing.Color.Black : System.Drawing.Color.White);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing bitmap data");
                throw;
            }

            bmp.SetResolution(203, 203);
            bmp.Save(outputPath, ImageFormat.Png);
        }
    }

    private static byte[] HexStringToByteArray(string hex)
    {
        int len = hex.Length;
        byte[] data = new byte[len / 2];

        for (int i = 0; i < len; i += 2)
            data[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);

        return data;
    }
    public static BitmapImage ConvertToBitmapImage(ImageSource imageSource)
    {
        if (imageSource is not BitmapSource bitmapSource)
            throw new ArgumentException("ImageSource must be a BitmapSource");

        using MemoryStream ms = new MemoryStream();
        BitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
        encoder.Save(ms);
        ms.Position = 0;
        BitmapImage bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.StreamSource = ms;
        bitmapImage.EndInit();
        bitmapImage.Freeze(); // Optional but recommended
        return bitmapImage;
    }
    public static string ConvertToTspl(string imagePath, int x = 0, int y = 0)
    {
        BitmapImage original = new BitmapImage(new Uri(imagePath));

        int newWidth = Math.Min(original.PixelWidth, MaxWidthDots);
        double scale = (double)newWidth / original.PixelWidth;
        int newHeight = (int)(original.PixelHeight * scale);

        // Resize
        var resized = new TransformedBitmap(original, new ScaleTransform(scale, scale));

        // Convert to grayscale format
        var grayBitmap = new FormatConvertedBitmap();
        grayBitmap.BeginInit();
        grayBitmap.Source = resized;
        grayBitmap.DestinationFormat = PixelFormats.Gray8;
        grayBitmap.EndInit();

        int stride = newWidth;
        byte[] pixels = new byte[newHeight * stride];
        grayBitmap.CopyPixels(pixels, stride, 0);

        // Apply Floyd–Steinberg dithering
        Dither(pixels, newWidth, newHeight);

        // Convert to 1-bit packed bytes
        (byte[] packed , int widthBytes) = PackBits(pixels, newWidth, newHeight);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("CLS");
        sb.Append($"BITMAP {x},{y},{widthBytes},{newHeight},0,");

        foreach (byte b in packed)
            sb.Append(b.ToString("X2"));

        sb.AppendLine();
        sb.AppendLine("PRINT 1,1");

        return sb.ToString();
    }
    public static string ConvertToTspl(byte[] pngBytes, int x = 0, int y = 0, int width = 0, int height = 0)
    {

        BitmapImage original = ByteArrayToBitmapImageConverter.ConvertByteArrayToBitMapImage(pngBytes);
        int newWidth = Math.Min(original.PixelWidth, Math.Min(MaxWidthDots, (int)width));
        double scale = (double)newWidth / original.PixelWidth;
        int newHeight = (int)(original.PixelHeight * scale);

        // Resize
        var resized = new TransformedBitmap(original, new ScaleTransform(scale, scale));
        // Convert to grayscale format
        var grayBitmap = new FormatConvertedBitmap();
        grayBitmap.BeginInit();
        grayBitmap.Source = resized;
        grayBitmap.DestinationFormat = PixelFormats.Gray8;
        grayBitmap.EndInit();

        int stride = newWidth;
        byte[] pixels = new byte[newHeight * stride];
        grayBitmap.CopyPixels(pixels, stride, 0);

        // Apply Floyd–Steinberg dithering
        Dither(pixels, newWidth, newHeight);

        // Convert to 1-bit packed bytes
        (byte[] packed,int widthBytes) = PackBits(pixels, newWidth, newHeight);
        StringBuilder sb = new StringBuilder();
        sb.Append($"""
            CLS

            BITMAP {x},{y},{widthBytes},{newHeight},0,
            """
            );
        sb.Append(Encoding.GetEncoding(1251).GetString(packed));
        sb.AppendLine();
        return sb.ToString();
    }
    public static string ConvertToTspl(BitmapSource bitmapSource, int x = 0, int y = 0, int width = 0, int height = 0)
    {
        var converted = new System.Windows.Media.Imaging.FormatConvertedBitmap();
        converted.BeginInit();
        converted.Source = bitmapSource;
        converted.DestinationFormat =  PixelFormats.Gray8;
        converted.DestinationPalette = null;
        converted.AlphaThreshold = 0;
        converted.EndInit();
        // Force caching (important for speed)
        var cached = new System.Windows.Media.Imaging.CachedBitmap(converted, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        var newWidth = (int)cached.PixelWidth;
        var newHeight = (int)cached.PixelHeight;
        int stride = newWidth;
        byte[] pixels = new byte[newHeight * stride];
        try
        {
            cached.CopyPixels(pixels, stride, 0);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error copying pixels from grayBitmap");
            throw;
        }
        // Apply Floyd–Steinberg dithering
        Dither(pixels, newWidth, newHeight);
        // Convert to 1-bit packed bytes
        var result = ConvertToThermal1Bit(pixels, newWidth, newHeight);


        //var result = PackBits(pixels, newWidth, newHeight);
        SaveTsplBitmapToPng(result.packed, result.widthBytes, result.height, @$"C:\WB_SQL\Element_{Guid.NewGuid():N}.png");
        StringBuilder sb = new StringBuilder();
        sb.Append($"BITMAP {x},{y},{result.widthBytes},{result.height},0,{Encoding.GetEncoding(1251).GetString(result.packed)}");
        return sb.ToString();
    }
    public static byte Enhance(byte value)
    {
        double contrast = 1.5;   // increase if needed
        double brightness = -20; // darken slightly

        double newValue = contrast * (value - 128) + 128 + brightness;

        return (byte)Math.Clamp(newValue, 0, 255);
    }
    public static ( byte[] packed,int widthBytes,int height) ConvertToThermal1Bit(
    byte[] gray,
    int width,
    int height)
    {
        int widthBytes = (width + 7) / 8;

        byte[] output = new byte[widthBytes * height];

        int threshold = 140;

        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                int i = y * width + x;

                int p = gray[i];

                // edge detection
                int gx = gray[i - 1] - gray[i + 1];
                int gy = gray[i - width] - gray[i + width];

                int edge = Math.Abs(gx) + Math.Abs(gy);

                // boost contrast on edges
                if (edge > 60)
                    p -= 40;

                if (p < threshold)
                {
                    int index = y * widthBytes + (x >> 3);
                    byte mask = (byte)(0x80 >> (x & 7));

                    output[index] |= mask;
                }
            }
        }

        return (output,widthBytes,height);
    }
    private static void Dither(byte[] pixels, int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                byte oldPixel = pixels[index];
                byte newPixel = oldPixel < threshold ? (byte)0 : (byte)255;
                pixels[index] = newPixel;
                int error = oldPixel - newPixel;
                DistributeError(pixels, width, height, x + 1, y, error, 7.0 / 16);
                DistributeError(pixels, width, height, x - 1, y + 1, error, 3.0 / 16);
                DistributeError(pixels, width, height, x, y + 1, error, 5.0 / 16);
                DistributeError(pixels, width, height, x + 1, y + 1, error, 1.0 / 16);
            }
        }
    }

    private static void DistributeError(byte[] pixels, int width, int height, int x, int y, int error, double factor)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return;
        int index = y * width + x;
        int value = pixels[index] + (int)(error * factor);
        pixels[index] = (byte)Math.Clamp(value, 0, 255);
    }

    private static (byte[] packed,int widthBytes) PackBits(byte[] pixels, int width, int height)
    {
        int widthBytes = (width + 7) / 8;
        byte[] packed = new byte[widthBytes * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int pixelIndex = y * width + x;
                int byteIndex = y * widthBytes + (x / 8);

                if (pixels[pixelIndex] == 0) // black pixel
                {
                    packed[byteIndex] |= (byte)(0x80 >> (x % 8));
                }
            }
        }
        return (packed, widthBytes);
    }

    //////////////////////////////////////
    public static BitmapSource ElementToBitmapSource(UIElement element, double dpiX = 203.0, double dpiY = 203.0)
    {

        Rect bounds = VisualTreeHelper.GetDescendantBounds(element);
        RenderTargetBitmap rtb = new RenderTargetBitmap(
        (int)(bounds.Width * dpiX / 96.0),
        (int)(bounds.Height * dpiY / 96.0),
        dpiX,
        dpiY,
        PixelFormats.Pbgra32);

        DrawingVisual dv = new DrawingVisual();
        using (DrawingContext dc = dv.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(new Point(), bounds.Size));
            VisualBrush vb = new VisualBrush(element);
            dc.DrawRectangle(vb, null, new Rect(new Point(), bounds.Size ));
        }
        rtb.Render(dv);
        rtb.Freeze();
        return rtb;
    }

}
    public static class TsplBarcodeMapper
    {
        public static string ToTspl(BarcodeFormat format)
        {
            return format switch
            {
                BarcodeFormat.CODE_128 => "128",
                BarcodeFormat.CODE_39 => "39",
                BarcodeFormat.CODE_93 => "93",
                BarcodeFormat.EAN_13 => "EAN13",
                BarcodeFormat.EAN_8 => "EAN8",
                BarcodeFormat.UPC_A => "UPCA",
                BarcodeFormat.UPC_E => "UPCE",
                BarcodeFormat.ITF => "ITF",
                BarcodeFormat.CODABAR => "CODA",
                BarcodeFormat.QR_CODE => "QRCODE",
                BarcodeFormat.PDF_417 => "PDF417",
                BarcodeFormat.DATA_MATRIX => "DATAMATRIX",
                BarcodeFormat.AZTEC => "AZTEC",
                _ => throw new NotSupportedException(
                        $"Barcode format {format} not supported by XP-365B TSPL")
            };
        }
        public static string Create1DBarcode(
            int x,
            int y,
            BarcodeFormat format,
            string data,
            int height = 100,
            int readable = 1,
            int rotation = 0,
            int narrow = 2,
            int wide = 2)
        {
            var tsplType = TsplBarcodeMapper.ToTspl(format);

            return $"BARCODE {x},{y},\"{tsplType}\",{height},{readable},{rotation},{narrow},{wide},\"{data}\"";
        }
        public static string CreateQRCode(
            int x,
            int y,
            string data,
            string ecc = "L",
            int cellWidth = 5,
            int rotation = 0)
        {
            return $"QRCODE {x},{y},{ecc},{cellWidth},A,{rotation},\"{data}\"";
        }
        public static string BuildLabel(string barcodeData)
        {
            var sb = new StringBuilder();

            sb.AppendLine("SIZE 60 mm,40 mm");
            sb.AppendLine("GAP 2 mm,0 mm");
            sb.AppendLine("CLS");

            sb.AppendLine(Create1DBarcode(
                x: 100,
                y: 100,
                format: BarcodeFormat.CODE_128,
                data: barcodeData));

            sb.AppendLine("PRINT 1");

            return sb.ToString();
        }
    }

    public class AsyncLineWriter : IAsyncDisposable
    {
        private readonly StreamWriter _writer;

        public AsyncLineWriter(string filePath)
        {
            var stream = new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                8192,
                FileOptions.Asynchronous);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            // Now you can use legacy encodings
            _writer = new StreamWriter(stream, Encoding.GetEncoding(1251));
        }

        public async Task WriteLineAsync(string line)
        {
            await _writer.WriteLineAsync(line);
        }

        public async ValueTask DisposeAsync()
        {
            Log.Information("Flushing and disposing AsyncLineWriter");

            await _writer.FlushAsync();
            await _writer.DisposeAsync();
        }
    }

    public static class DynamicSqliteXpsPipelineFactory
    {

        // ======================================================
        // PUBLIC ENTRY
        // ======================================================
        public static Task GenerateAsync(
            string connectionString,
            string availableSources,
            string jsonContent,
            string printerName,
            PrintTicket ticket,
            PrintSettings printSettings,
            CancellationToken token = default)
        {
            return Task.Run(() =>
            {
                //            var xpsPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".xps");
                var xpsPath = System.IO.Path.Combine(@"C:\WB_SQL", Guid.NewGuid().ToString("N") + ".xps");
                var tsplPath = System.IO.Path.Combine(@"C:\WB_SQL", Guid.NewGuid().ToString("N") + ".tspl");
                var done = new ManualResetEvent(false);

                Thread staThread = new Thread(() =>
                {
                    try
                    {
                        DynamicSqliteXpsPipeline dynamicSqliteXpsPipeline = new DynamicSqliteXpsPipeline();
                        var dispatcher = Dispatcher.CurrentDispatcher;
                        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));

                        // Schedule the async work to run on the dispatcher, then run the dispatcher loop.
                        dispatcher.InvokeAsync(async () =>
                        {
                            try
                            {
                                await dynamicSqliteXpsPipeline.GenerateOnSta(connectionString, availableSources, jsonContent, xpsPath, tsplPath, printerName, ticket, printSettings, token)
                                    .ConfigureAwait(false);
                            }
                            catch (Exception e)
                            {
                                Log.Error(e, "Error in GenerateOnSta");
                            }
                            finally
                            {
                                done.Set();
                                // Request shutdown from dispatcher thread
                                dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
                            }
                        });

                        // Start the dispatcher message loop (will exit after BeginInvokeShutdown)
                        Dispatcher.Run();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Error in STA thread");
                    }
                });
                staThread.SetApartmentState(ApartmentState.STA);
                staThread.IsBackground = true;
                staThread.Start();
                done.WaitOne();
            }, token);
        }

    }

    public class DynamicSqliteXpsPipeline
    {
        private static readonly HttpClient _http = new HttpClient();

        private ImagePipelineLoader loader;
        // ======================================================
        // DYNAMIC MODEL GENERATOR
        // ======================================================
        private Type CreateDynamicModelType(
            string connectionString,
            string sql)
        {
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();

            using var cmd = new SQLiteCommand(sql, conn);
            using var reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly);

            var schema = reader.GetColumnSchema();

            var assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("DynamicModels"),
                AssemblyBuilderAccess.Run);

            var module = assembly.DefineDynamicModule("Main");

            var typeBuilder = module.DefineType(
                "DynamicRow_" + Guid.NewGuid().ToString("N"),
                TypeAttributes.Public);

            foreach (var col in schema)
            {
                CreateProperty(typeBuilder,
                    col.ColumnName,
                    MapType(col.DataType));
            }

            return typeBuilder.CreateType();
        }

        private void CreateProperty(
            TypeBuilder builder,
            string name,
            Type type)
        {
            var field = builder.DefineField("_" + name,
                type,
                FieldAttributes.Private);

            var prop = builder.DefineProperty(
                name,
                PropertyAttributes.HasDefault,
                type,
                null);

            var getter = builder.DefineMethod(
                "get_" + name,
                MethodAttributes.Public |
                MethodAttributes.SpecialName |
                MethodAttributes.HideBySig,
                type,
                Type.EmptyTypes);

            var il = getter.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ret);

            var setter = builder.DefineMethod(
                "set_" + name,
                MethodAttributes.Public |
                MethodAttributes.SpecialName |
                MethodAttributes.HideBySig,
                null,
                new[] { type });

            var il2 = setter.GetILGenerator();
            il2.Emit(OpCodes.Ldarg_0);
            il2.Emit(OpCodes.Ldarg_1);
            il2.Emit(OpCodes.Stfld, field);
            il2.Emit(OpCodes.Ret);

            prop.SetGetMethod(getter);
            prop.SetSetMethod(setter);
        }

        private Type MapType(Type dbType)
        {
            if (dbType == typeof(long)) return typeof(long);
            if (dbType == typeof(int)) return typeof(int);
            if (dbType == typeof(double)) return typeof(double);
            if (dbType == typeof(byte[])) return typeof(byte[]);
            if (dbType == typeof(DateTime)) return typeof(DateTime);
            return typeof(string);
        }

        // ======================================================
        // BACKGROUND PRODUCER
        // ======================================================
        private Task StartProducer(
            string connectionString,
            string sql,
            Type modelType,
            BlockingCollection<object> buffer,
            CancellationToken token)
        {
            return Task.Run(() =>
            {
                using var conn = new SQLiteConnection(connectionString);
                conn.Open();

                using var cmd = new SQLiteCommand(sql, conn);
                using var reader = cmd.ExecuteReader();

                var props = modelType.GetProperties();

                while (reader.Read())
                {
                    token.ThrowIfCancellationRequested();

                    var instance = Activator.CreateInstance(modelType);

                    foreach (var prop in props)
                    {
                        var val = reader[prop.Name];
                        if (val == DBNull.Value) val = null;
                        prop.SetValue(instance, val);
                    }

                    buffer.Add(instance, token);
                }

                buffer.CompleteAdding();
            }, token);
        }

        // ======================================================
        // STA XPS CONSUME
        //
        // R
        // ======================================================
        public string ReportDataSql(TemplateRecord template, ObservableCollection<KeyValueItem> AvailableSources)
        {
            var source =
        template.textFieldValues
            .Select(x => new { x.DataSourceName, x.DataSourceType, x.DataFieldName }).Where(x => !string.IsNullOrEmpty(x.DataSourceName) && x.DataSourceType == DataSourceType.Database)
        .Concat(template.barcodeImage
            .Select(x => new { x.DataSourceName, x.DataSourceType, x.DataFieldName })).Where(x => !string.IsNullOrEmpty(x.DataSourceName) && x.DataSourceType == DataSourceType.Database)
        .Concat(template.qRCodeImages
            .Select(x => new { x.DataSourceName, x.DataSourceType, x.DataFieldName}))
        .Concat(template.imageProperties
            .Select(x => new { x.DataSourceName, x.DataSourceType, x.DataFieldName })).Where(x => !string.IsNullOrEmpty(x.DataSourceName) && x.DataSourceType == DataSourceType.Database)
        .Concat(template.rectangleFigures
            .Select(x => new { x.DataSourceName, x.DataSourceType, x.DataFieldName})).Where(x => !string.IsNullOrEmpty(x.DataSourceName) && x.DataSourceType == DataSourceType.Database)
        .Concat(template.lineProperties
            .Select(x => new { x.DataSourceName, x.DataSourceType, x.DataFieldName }))
        .Distinct()
        .Concat(template.polygonProperties
            .Select(x => new { x.DataSourceName, x.DataSourceType, x.DataFieldName }))
        .Distinct().Select(x => x.DataSourceName).Distinct().FirstOrDefault();
        if (string.IsNullOrEmpty(source))
        {
            return String.Empty;
        }
        ;
            var query = AvailableSources.FirstOrDefault(x => x.Key == source);
        if (query == null || query.Key == null)
        {
            return String.Empty;
        }
        return query.Value;
        }
        public Task PrintOnStaThreadAsync(string printerName, string xpsPath)
        {
            var tcs = new TaskCompletionSource<bool>();

            var thread = new Thread(() =>
            {
                try
                {
                    using (var printServer = new LocalPrintServer())
                    using (var printQueue = printServer.GetPrintQueue(printerName))
                    using (var xpsDoc = new XpsDocument(xpsPath, FileAccess.Read))
                    {
                        var writer = PrintQueue.CreateXpsDocumentWriter(printQueue);
                        writer.Write(xpsDoc.GetFixedDocumentSequence());
                    }

                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            return tcs.Task;
        }

        public Task TsplPrintOnStaThreadAsync(string printerName, string tspCommandsPath)
        {
            var tcs = new TaskCompletionSource<bool>();

            var thread = new Thread(async () =>
            {
                try
                {
                    using (var printServer = new LocalPrintServer())
                    using (var printQueue = printServer.GetPrintQueue(printerName))
                        await SendFileAsync(printerName, tspCommandsPath);

                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            return tcs.Task;
        }

        public async Task SendTsplAsync(string printerIp, string filePath)
        {
            byte[] data = await File.ReadAllBytesAsync(filePath);

            using System.Net.Sockets.TcpClient client = new TcpClient();
            await client.ConnectAsync(printerIp, 9100);

            using NetworkStream stream = client.GetStream();
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();
        }

        public static async Task SendFileAsync(string printerName, string filePath)
        {
            await Task.Run(async () =>
            {
                byte[] bytes = await File.ReadAllBytesAsync(filePath);
                RawPrinterHelper.SendBytesToPrinter(printerName, bytes);
            });
        }
    public async Task GenerateOnSta(
            string connectionString,
            string availableSources,
            string jsonContent,
            string xpsPath,
            string tsplPath,
            string printerName,
            PrintTicket ticket,
            PrintSettings printSettings,
            CancellationToken token)
    {
        loader = new ImagePipelineLoader();
        using var xps = new XpsDocument(xpsPath, FileAccess.Write);
        var writer = XpsDocument.CreateXpsDocumentWriter(xps);
        FixedDocument doc = new FixedDocument();

        var tsplWriter = new AsyncLineWriter(tsplPath);
        var AvailableSources = JsonHelper.Deserialize<ObservableCollection<KeyValueItem>>(availableSources);
        var report = JsonHelper.Deserialize<Dictionary<CrystalReportSection, TemplateRecord>>(jsonContent);
        var header = GenerateTSPLCommands(null, ticket, printSettings);
        await tsplWriter.WriteLineAsync(header);
        foreach (var section in report)
        {
            var template = section.Value;
            var ForcePageBreakBefore = template.elements.Where(x => x.Name.StartsWith("PageBreak_") == true).FirstOrDefault() == null ? false : true;
            var sql = ReportDataSql(template, AvailableSources);
            if (string.IsNullOrEmpty(sql))
            {
                sql = "SELECT date('now','localtime') AS CurrentTime";
            }
            Type modelType = CreateDynamicModelType(connectionString, sql);

            using (BlockingCollection<object> buffer = new BlockingCollection<object>(50))
            {
                var producer = StartProducer(
                    connectionString,
                    sql,
                    modelType,
                    buffer,
                    token);

                foreach (var obj in buffer.GetConsumingEnumerable(token))
                {
                    var page = await CreatePageAsync(obj, modelType, template, ticket, printSettings);
                    var pageContent = new PageContent();
                    ((IAddChild)pageContent).AddChild(page);
                    doc.Pages.Add(pageContent);
                    string tsplCommands = GenerateTSPLCommands(page, ticket, printSettings);
                    await tsplWriter.WriteLineAsync(tsplCommands);
                    if (section.Key != CrystalReportSection.Details)
                    {
                        break;
                    }
                }
                Log.Information("Producer completed for section {Section}", section.Key);
                await producer.WaitAsync(token);
            }
        }
        try
        {
            writer.Write(doc);

        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error writing XPS document");
            throw;
        }
        xps.CoreDocumentProperties.Description = "report output";
        xps.Close();
        await tsplWriter.DisposeAsync();
        if (printSettings.Terminal)
        {
            await TsplPrintOnStaThreadAsync(printerName, tsplPath);
        }
        else
        {
            await PrintOnStaThreadAsync(printerName, xpsPath);
        }

    }
        private int DotsToMm(double dots)
        {
            return (int)(dots / 96.0 * 25.4);
        }

        public static int ConvertToPrinterPixels(double wpfValue, int printerDpi)
        {
            return (int)Math.Round((wpfValue * printerDpi) / 96.0);
        }
        public static int MmToDots(double mm, int printerDpi)
        {
            return (int)Math.Round(mm * printerDpi / 25.4);
        }


        private string GenerateTSPLCommands(FixedPage page, PrintTicket ticket, PrintSettings printSettings)
        {
            var Width = ticket.PageMediaSize.Width.Value;
            var Height = ticket.PageMediaSize.Height.Value;

            var pageWidth = ticket.PageMediaSize.Width.Value * 25.4 / 96;
            var pageHeight = ticket.PageMediaSize.Height.Value * 25.4 / 96;
            var dpiX = (int)(ticket.PageResolution.X);
            var dpiY = (int)(ticket.PageResolution.Y);
            // Register the provider
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            // Now you can use legacy encodings
            var encoding = Encoding.GetEncoding(1251);

            string header = $"""
                SIZE {(int)pageWidth} mm,{(int)pageHeight} mm
                GAP 3 mm,0 mm 
                DENSITY 8 
                DIRECTION 0
                REFERENCE 0,0
                OFFSET 0 mm
                SET PEEL OFF
                SET CUTTER OFF
                SET TEAR ON
                CODEPAGE 1251
                CLS
                """;


            StringBuilder sb = new StringBuilder();
            if (page == null)
            {
                sb.AppendLine(header);
                return sb.ToString();
            }
            foreach (var child in page.Children)
            {
                if (child is Canvas canvas)
                {
                    foreach (var element in canvas.Children)
                    {
                    if (element is TextBox textBox)
                    {
                        if (TextFieldProperties.ContainsKey(textBox.Name))
                        {
                            var textProps = TextFieldProperties[textBox.Name];
                            if (textProps != null && textProps.ConvertToBitmap)
                            {

                                var bitmapSource = TsplBitmapConverter.ElementToBitmapSource(textBox, dpiX, dpiY);
                                var tspl =
                                    TsplBitmapConverter.ConvertToTspl(bitmapSource, ConvertToPrinterPixels(Canvas.GetLeft(textBox), dpiX), ConvertToPrinterPixels(Canvas.GetTop(textBox), dpiY),
                                    bitmapSource.PixelWidth, bitmapSource.PixelHeight);
                                sb.AppendLine(tspl);
                            }
                        }
                        else
                        {
                            sb.AppendLine($"TEXT {ConvertToPrinterPixels(Canvas.GetLeft(textBox), dpiX)} , {ConvertToPrinterPixels(Canvas.GetTop(textBox), dpiY)}, \"{textBox.FontFamily.Source}\", {textBox.FontSize}, 0, 1, \"{textBox.Text}\"");
                        }
                    }
                    else if (element is System.Windows.Shapes.Rectangle rect)
                    {
                        var x_start = ConvertToPrinterPixels(Canvas.GetLeft(rect), dpiX);
                        var y_start = ConvertToPrinterPixels(Canvas.GetTop(rect), dpiY);
                        var x_end = ConvertToPrinterPixels(Canvas.GetLeft(rect) + rect.Width, dpiX);
                        var y_end = ConvertToPrinterPixels(Canvas.GetTop(rect) + rect.Height, dpiY);
                        if (RectangleFigures.ContainsKey(rect.Name))
                        {
                            var rectProps = RectangleFigures[rect.Name];
                            if (rectProps != null && rectProps.ConvertToBitmap)
                            {
                                var bitmapSource = TsplBitmapConverter.ElementToBitmapSource(rect, dpiX, dpiY);
                                var tspl =
                                    TsplBitmapConverter.ConvertToTspl(bitmapSource, ConvertToPrinterPixels(Canvas.GetLeft(rect), dpiX), ConvertToPrinterPixels(Canvas.GetTop(rect), dpiY),
                                    bitmapSource.PixelWidth, bitmapSource.PixelHeight);
                                sb.AppendLine(tspl);
                                continue;
                            }
                        }
                        if (rect.Fill is SolidColorBrush brush && (brush.Color == Colors.White || brush.Color == Colors.Transparent))
                        {
                            sb.AppendLine($"""

                            BOX {x_start},{y_start} ,{x_end} , {y_end} , {(int)(rect.StrokeThickness)}
                            
                            """);
                        }
                        else
                        {
                            sb.AppendLine($"""

                            BAR {x_start},{y_start} ,{x_end} , {y_end} 
                            
                            """);
                        }
                    }
                    else if (element is System.Windows.Shapes.Line line)
                    {

                        if (Lines.ContainsKey(line.Name))
                        {
                            var lineProp = Lines[line.Name];
                            if (lineProp != null && lineProp.ConvertToBitmap)
                            {
                                var bitmapSource = TsplBitmapConverter.ElementToBitmapSource(line, dpiX, dpiY);
                                var tspl =
                                    TsplBitmapConverter.ConvertToTspl(bitmapSource, ConvertToPrinterPixels(Canvas.GetLeft(line), dpiX), ConvertToPrinterPixels(Canvas.GetTop(line), dpiY),
                                    bitmapSource.PixelWidth, bitmapSource.PixelHeight);
                                sb.AppendLine(tspl);
                            }
                        }
                        else
                        {

                            var X1 = ConvertToPrinterPixels(line.X1, dpiX);
                            var Y1 = ConvertToPrinterPixels(line.Y1, dpiY);
                            var X2 = ConvertToPrinterPixels(line.X2, dpiX);
                            var Y2 = ConvertToPrinterPixels(line.Y2, dpiY);
                            var left = Math.Min(X1, X2);
                            var top = Math.Min(Y1, Y2);
                            var right = Math.Max(X1, X2);
                            var bottom = Math.Max(Y1, Y2);

                            int width = right - left + 1;
                            int height = bottom - top + 1;

                            int StrokeThickness = (int)(line.StrokeThickness * dpiY / 96);
                            int widthBytes = (width + 7) / 8;
                            byte[] lineData = new byte[widthBytes * height];
                            BitmapExtensions.DrawDLine(lineData, widthBytes, X1 - left, Y1 - top, X2 - left, Y2 - top, StrokeThickness);
                            for (int i = 0; i < lineData.Length; i++)
                            {
                                lineData[i] = (byte)~lineData[i];
                            }
                            string tspl = $"BITMAP {left},{top} ,{widthBytes} , {height},1,{Encoding.GetEncoding(1251).GetString(lineData)}";
                            sb.AppendLine(tspl);
                        }

                    }
                    else if (element is System.Windows.Shapes.Polygon polygon)
                    {
                        var polygonProps = Polygons[polygon.Name];
                        if (polygonProps != null && polygonProps.ConvertToBitmap)
                        {
                            var bitmapSource = TsplBitmapConverter.ElementToBitmapSource(polygon, dpiX, dpiY);
                            var tspl =
                                TsplBitmapConverter.ConvertToTspl(bitmapSource, ConvertToPrinterPixels(Canvas.GetLeft(polygon), dpiX), ConvertToPrinterPixels(Canvas.GetTop(polygon), dpiY),
                                bitmapSource.PixelWidth, bitmapSource.PixelHeight);
                            sb.AppendLine(tspl);
                        }

                    }
                    else if (element is Image image)
                    {
                        if (Barcodes.ContainsKey(image.Name))
                        {
                            var barcodeProps = Barcodes[image.Name];
                            if (barcodeProps.BarcodeFormat == BarcodeFormat.QR_CODE)
                            {
                                sb.AppendLine(TsplBarcodeMapper.CreateQRCode(
                                       x: ConvertToPrinterPixels(Canvas.GetLeft(image), dpiX), y: ConvertToPrinterPixels(Canvas.GetTop(image), dpiY),
                                       data: barcodeProps.Barcode, cellWidth: ConvertToPrinterPixels(image.Height, dpiY),
                                       rotation: ToTSPLAngle(barcodeProps.Angle)));
                            }
                            else
                            {
                                sb.AppendLine(TsplBarcodeMapper.Create1DBarcode(
                                    x: ConvertToPrinterPixels(Canvas.GetLeft(image), dpiX), y: ConvertToPrinterPixels(Canvas.GetTop(image), dpiY),
                                    format: barcodeProps.BarcodeFormat, data: barcodeProps.Barcode,
                                    height: ConvertToPrinterPixels(image.Height, dpiY), rotation: ToTSPLAngle(barcodeProps.Angle),
                                    readable: barcodeProps.Readable ? 1 : 0,
                                    narrow: barcodeProps.Narrow, wide: barcodeProps.Wide));
                            }
                        }
                        else
                        {

                            if (Images.ContainsKey(image.Name))
                            {
                                var bitmapSource = TsplBitmapConverter.ElementToBitmapSource(image, dpiX, dpiY);
                                var tspl =
                                    TsplBitmapConverter.ConvertToTspl(bitmapSource, ConvertToPrinterPixels(Canvas.GetLeft(image), dpiX), ConvertToPrinterPixels(Canvas.GetTop(image), dpiY),
                                    bitmapSource.PixelWidth, bitmapSource.PixelHeight)
                                ;
                                sb.AppendLine(tspl);
                            }
                        }
                    }
                    }
                }
            }
            sb.AppendLine("PRINT 1,1");
            return sb.ToString();

        }


        private int ToTSPLAngle(double angle)
        {
            int normalizedAngle = ((int)angle % 360 + 360) % 360;
            if (normalizedAngle == 0) return 0;
            if (normalizedAngle == 90) return 90;
            if (normalizedAngle == 180) return 180;
            if (normalizedAngle == 270) return 270;
            return 0; // Default to 0 if it's not a standard angle

        }

        // ======================================================
        // PAGE CREATION
        // ======================================================
        private async Task<FixedPage> CreatePageAsync(
            object model,
            Type type,
            TemplateRecord template,
            PrintTicket ticket,
            PrintSettings printSettings
            )
        {
            var page = new FixedPage { Width = ticket.PageMediaSize.Width.Value, Height = ticket.PageMediaSize.Height.Value };
            var canvas = new Canvas
            {
                Width = ticket.PageMediaSize.Width.Value,
                Height = ticket.PageMediaSize.Height.Value,
                Background = Brushes.White
            };
            CreateSectionCanvas(canvas, template);
            var props = type.GetProperties();
            await SetSectionBindingsAsync(canvas, template, props, model);
            page.Children.Add(canvas);
            page.Measure(new Size(canvas.Width, canvas.Height));
            page.Arrange(new Rect(0, 0, canvas.Width, canvas.Height));
            page.UpdateLayout();
            return page;
        }
        private void CreateSectionCanvas(Canvas templateCanvas, TemplateRecord template)
        {
            templateCanvas.BeginInit();
            templateCanvas.Children.Clear(); // Очистка текущего холста перед загрузкой нового макета
            foreach (var canvasElement in template.elements)
            {
                switch (canvasElement.Type)
                {
                    case "Rectangle":
                        {
                            var rectangle = new System.Windows.Shapes.Rectangle
                            {
                                Name = canvasElement.Name,
                                Width = canvasElement.Width,
                                Height = canvasElement.Height,
                                Fill = JsonHelper.Deserialize<Brush>(canvasElement.Fill),
                                Stroke = JsonHelper.Deserialize<Brush>(canvasElement.Stroke),
                                StrokeThickness = canvasElement.StrokeThickness,
                                RadiusX = canvasElement.RadiusX,
                                RadiusY = canvasElement.RadiusY,
                                HorizontalAlignment = canvasElement.HorizontalAlignment != null ? JsonHelper.Deserialize<System.Windows.HorizontalAlignment>(canvasElement.HorizontalAlignment) : System.Windows.HorizontalAlignment.Left,
                                VerticalAlignment = canvasElement.VerticalAlignment != null ? JsonHelper.Deserialize<System.Windows.VerticalAlignment>(canvasElement.VerticalAlignment) : System.Windows.VerticalAlignment.Top,
                                Opacity = canvasElement.Opacity,
                                Stretch = JsonHelper.Deserialize<Stretch>(canvasElement.Stretch)
                            };
                            templateCanvas.Children.Add(rectangle);
                        }
                        break;
                    case "Line":
                        {
                            var line = new System.Windows.Shapes.Line
                            {
                                Name = canvasElement.Name,
                                X1 = canvasElement.X1,
                                Y1 = canvasElement.Y1,
                                X2 = canvasElement.X2,
                                Y2 = canvasElement.Y2,
                                Stroke = JsonHelper.Deserialize<Brush>(canvasElement.Stroke),
                                StrokeThickness = canvasElement.StrokeThickness,
                                StrokeStartLineCap = JsonHelper.Deserialize<PenLineCap>(canvasElement.StrokeStartLineCap),
                                StrokeEndLineCap = JsonHelper.Deserialize<PenLineCap>(canvasElement.StrokeEndLineCap),
                            };
                            templateCanvas.Children.Add(line);
                        }
                        break;
                case "Polygon":
                    {
                        var polygon = new System.Windows.Shapes.Polygon
                        {
                            Name = canvasElement.Name,
                            Stroke = JsonHelper.Deserialize<Brush>(canvasElement.Stroke),
                            Fill = JsonHelper.Deserialize<Brush>(canvasElement.Fill),
                            StrokeThickness = canvasElement.StrokeThickness,
                            HorizontalAlignment = canvasElement.HorizontalAlignment != null ? JsonHelper.Deserialize<System.Windows.HorizontalAlignment>(canvasElement.HorizontalAlignment) : System.Windows.HorizontalAlignment.Left,
                            VerticalAlignment = canvasElement.VerticalAlignment != null ? JsonHelper.Deserialize<System.Windows.VerticalAlignment>(canvasElement.VerticalAlignment) : System.Windows.VerticalAlignment.Top,
                            Opacity = canvasElement.Opacity,
                            Stretch = JsonHelper.Deserialize<Stretch>(canvasElement.Stretch),
                            Points = new PointCollection(canvasElement.Points)
                        };
                        templateCanvas.Children.Add(polygon);
                    }
                    break;
                }
            }
            foreach (var textBoxElement in template.textBoxCanvasElements)
            {


                var textBox = new TextBox
                {
                    Name = textBoxElement.Name,
                    Text = textBoxElement.Text,
                    Width = textBoxElement.Width,
                    Height = textBoxElement.Height,
                    Background = string.IsNullOrWhiteSpace(textBoxElement.Background) ? new SolidColorBrush(System.Windows.Media.Colors.Transparent) : JsonHelper.Deserialize<Brush>(textBoxElement.Background),
                    Foreground = string.IsNullOrWhiteSpace(textBoxElement.Foreground) ? new SolidColorBrush(System.Windows.Media.Colors.Black) : JsonHelper.Deserialize<Brush>(textBoxElement.Foreground),
                    FontFamily = new System.Windows.Media.FontFamily(!string.IsNullOrWhiteSpace(textBoxElement.FontFamily) ? textBoxElement.FontFamily : "Arial"),
                    FontSize = textBoxElement.FontSize,
                    FontStyle = string.IsNullOrWhiteSpace(textBoxElement.FontStyle) ? new System.Windows.FontStyle() : JsonHelper.Deserialize<System.Windows.FontStyle>(textBoxElement.FontStyle),
                    FontWeight = string.IsNullOrWhiteSpace(textBoxElement.FontWeight) ? System.Windows.FontWeights.Normal : JsonHelper.Deserialize<System.Windows.FontWeight>(textBoxElement.FontWeight),
                    FontStretch = string.IsNullOrWhiteSpace(textBoxElement.FontStretch) ? System.Windows.FontStretches.Normal : JsonHelper.Deserialize<System.Windows.FontStretch>(textBoxElement.FontStretch),
                    TextWrapping = string.IsNullOrWhiteSpace(textBoxElement.TextWrapping) ? System.Windows.TextWrapping.NoWrap : JsonHelper.Deserialize<TextWrapping>(textBoxElement.TextWrapping),
                    RenderTransform = new RotateTransform(textBoxElement.Angle)
                };
                templateCanvas.Children.Add(textBox);
            }
            foreach (var imageElement in template.imageCanvasElements)
            {
                var image = new System.Windows.Controls.Image
                {
                    Name = imageElement.Name,
                    Width = imageElement.Width,
                    Height = imageElement.Height,
                    HorizontalAlignment = JsonHelper.Deserialize<System.Windows.HorizontalAlignment>(imageElement.HorizontalAlignment),
                    VerticalAlignment = JsonHelper.Deserialize<System.Windows.VerticalAlignment>(imageElement.VerticalAlignment),
                    Opacity = imageElement.Opacity,
                    Stretch = JsonHelper.Deserialize<Stretch>(imageElement.Stretch),
                    Source = (string.IsNullOrEmpty(imageElement.ImageSourceString)) ? JsonHelper.Deserialize<ImageSource>(imageElement.Source) : ImageCanvasElement.Base64ToImage(imageElement.ImageSourceString)
                };

                templateCanvas.Children.Add(image);
            }
            templateCanvas.EndInit();
        }
        public Dictionary<string, TextFieldValue> TextFieldProperties
        {
            get; private set;
        }
        public Dictionary<string, ImageProperties> Images
        {
            get; private set;
        }
        public Dictionary<string, BarcodeImageProperties> Barcodes
        {
            get; private set;
        }
        public Dictionary<string, QRCodeImageProperties> QRCodes
        {
            get; private set;
        }
        public Dictionary<string, RectangleFigureProperties> RectangleFigures
        {
            get; private set;
        }
        public Dictionary<string, LineProperties> Lines
        {
            get; private set;
        }
    public Dictionary<string, PolygonProperties> Polygons
    {
        get; private set;
    }
        public void SetBindingLineProperties(ref Line line)
        {
            BindingOperations.ClearAllBindings(line);
            var bindingGroup = new BindingGroup();
            line.BindingGroup = bindingGroup;
            var lineFigure = Lines[line.Name];
            Binding bindingStrokeEndLineCap = new Binding();
            bindingStrokeEndLineCap.Source = Lines;
            bindingStrokeEndLineCap.Path = new PropertyPath("[" + lineFigure.Name + "].StrokeEndLineCap");
            bindingStrokeEndLineCap.Mode = BindingMode.TwoWay;
            bindingStrokeEndLineCap.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            line.SetBinding(Line.StrokeEndLineCapProperty, bindingStrokeEndLineCap);

            Binding bindingStrokeStartLineCap = new Binding();
            bindingStrokeStartLineCap.Source = Lines;
            bindingStrokeStartLineCap.Path = new PropertyPath("[" + lineFigure.Name + "].StrokeStartLineCap");
            bindingStrokeStartLineCap.Mode = BindingMode.TwoWay;
            bindingStrokeStartLineCap.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            line.SetBinding(Line.StrokeStartLineCapProperty, bindingStrokeStartLineCap);

            Binding bindingThickness = new Binding();
            bindingThickness.Source = Lines;
            bindingThickness.Path = new PropertyPath("[" + lineFigure.Name + "].StrokeThickness");
            bindingThickness.Mode = BindingMode.TwoWay;
            bindingThickness.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            line.SetBinding(Line.StrokeThicknessProperty, bindingThickness);

            Binding bindingX1 = new Binding();
            bindingX1.Source = Lines;
            bindingX1.Path = new PropertyPath("[" + lineFigure.Name + "].X1");
            bindingX1.Mode = BindingMode.TwoWay;
            bindingX1.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            line.SetBinding(Line.X1Property, bindingX1);

            Binding bindingY1 = new Binding();
            bindingY1.Source = Lines;
            bindingY1.Path = new PropertyPath("[" + lineFigure.Name + "].Y1");
            bindingY1.Mode = BindingMode.TwoWay;
            bindingY1.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            line.SetBinding(Line.Y1Property, bindingY1);

            Binding bindingX2 = new Binding();
            bindingX2.Source = Lines;
            bindingX2.Path = new PropertyPath("[" + lineFigure.Name + "].X2");
            bindingX2.Mode = BindingMode.TwoWay;
            bindingX2.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            line.SetBinding(Line.X2Property, bindingX2);

            Binding bindingY2 = new Binding();
            bindingY2.Source = Lines;
            bindingY2.Path = new PropertyPath("[" + lineFigure.Name + "].Y2");
            bindingY2.Mode = BindingMode.TwoWay;
            bindingY2.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            line.SetBinding(Line.Y2Property, bindingY2);

            Binding bindingStroke = new Binding();
            bindingStroke.Source = Lines;
            bindingStroke.Converter = new ColorToBrushConverter();
            bindingStroke.Path = new PropertyPath("[" + lineFigure.Name + "].Stroke");
            bindingStroke.Mode = BindingMode.TwoWay;
            bindingStroke.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            line.SetBinding(Line.StrokeProperty, bindingStroke);

            Binding bindingAngle = new Binding();
            bindingAngle.Source = Lines;
            bindingAngle.Path = new PropertyPath("[" + lineFigure.Name + "].Angle");
            bindingAngle.Mode = BindingMode.TwoWay;
            bindingAngle.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

            var rotate = new RotateTransform();
            BindingOperations.SetBinding(rotate, RotateTransform.AngleProperty, bindingAngle);
            line.RenderTransform = rotate;

            Binding bindingLeft = new Binding()
            {
                Mode = BindingMode.TwoWay,
                Source = Lines,
                Path = new PropertyPath("[" + lineFigure.Name + "].Left"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            };
            BindingOperations.SetBinding(line, Canvas.LeftProperty, bindingLeft);
            Binding bindingTop = new Binding()
            {
                Mode = BindingMode.TwoWay,
                Source = Lines,
                Path = new PropertyPath("[" + lineFigure.Name + "].Top"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            };
            BindingOperations.SetBinding(line, Canvas.TopProperty, bindingTop);
        }
        public void SetBindingRectangleProperties(ref Rectangle rectangle)
        {
            BindingOperations.ClearAllBindings(rectangle);
            var bindingGroup = new BindingGroup();
            rectangle.BindingGroup = bindingGroup;
            var rectangleFigure = RectangleFigures[rectangle.Name];
            Binding bindingWidth = new Binding();
            bindingWidth.Source = RectangleFigures;
            bindingWidth.Path = new PropertyPath("[" + rectangleFigure.Name + "].Width");
            bindingWidth.Mode = BindingMode.TwoWay;
            bindingWidth.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            rectangle.SetBinding(Rectangle.WidthProperty, bindingWidth);
            BindingOperations.SetBinding(rectangle, Canvas.LeftProperty, new Binding()
            {
                Source = RectangleFigures,
                Path = new PropertyPath("[" + rectangleFigure.Name + "].Left"),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            BindingOperations.SetBinding(rectangle, Canvas.TopProperty, new Binding()
            {
                Source = RectangleFigures,
                Path = new PropertyPath("[" + rectangleFigure.Name + "].Top"),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });

            Binding bindingHeight = new Binding();
            bindingHeight.Source = RectangleFigures;
            bindingHeight.Path = new PropertyPath("[" + rectangleFigure.Name + "].Height");
            bindingHeight.Mode = BindingMode.TwoWay;
            bindingHeight.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            rectangle.SetBinding(Rectangle.HeightProperty, bindingHeight);


            Binding bindingThickness = new Binding();
            bindingThickness.Source = RectangleFigures;
            bindingThickness.Path = new PropertyPath("[" + rectangleFigure.Name + "].StrokeThickness");
            bindingThickness.Mode = BindingMode.TwoWay;
            bindingThickness.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            rectangle.SetBinding(Rectangle.StrokeThicknessProperty, bindingThickness);

            Binding bindingRadiusX = new Binding();
            bindingRadiusX.Source = RectangleFigures;
            bindingRadiusX.Path = new PropertyPath("[" + rectangleFigure.Name + "].RadiusX");
            bindingRadiusX.Mode = BindingMode.TwoWay;
            bindingRadiusX.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            rectangle.SetBinding(Rectangle.RadiusXProperty, bindingRadiusX);

            Binding bindingRadiusY = new Binding();
            bindingRadiusY.Source = RectangleFigures;
            bindingRadiusY.Path = new PropertyPath("[" + rectangleFigure.Name + "].RadiusY");
            bindingRadiusY.Mode = BindingMode.TwoWay;
            bindingRadiusY.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            rectangle.SetBinding(Rectangle.RadiusYProperty, bindingRadiusY);

            Binding bindingStroke = new Binding();
            bindingStroke.Source = RectangleFigures;
            bindingStroke.Converter = new ColorToBrushConverter();
            bindingStroke.Path = new PropertyPath("[" + rectangleFigure.Name + "].Stroke");
            bindingStroke.Mode = BindingMode.TwoWay;
            bindingStroke.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            rectangle.SetBinding(Rectangle.StrokeProperty, bindingStroke);

            Binding bindingFill = new Binding();
            bindingFill.Source = RectangleFigures;
            bindingFill.Converter = new ColorToBrushConverter();
            bindingFill.Path = new PropertyPath("[" + rectangleFigure.Name + "].Fill");
            bindingFill.Mode = BindingMode.TwoWay;
            bindingFill.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(rectangle, Rectangle.FillProperty, bindingFill);
            Binding bindingStretch = new Binding();
            bindingStretch.Source = RectangleFigures;
            bindingStretch.Path = new PropertyPath("[" + rectangleFigure.Name + "].Stretch");
            bindingStretch.Mode = BindingMode.TwoWay;
            bindingStretch.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            rectangle.SetBinding(Rectangle.StretchProperty, bindingStretch);

            Binding bindingAngle = new Binding();
            bindingAngle.Source = RectangleFigures;
            bindingAngle.Path = new PropertyPath("[" + rectangleFigure.Name + "].Angle");
            bindingAngle.Mode = BindingMode.TwoWay;
            bindingAngle.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

            var rotate = new RotateTransform();
            BindingOperations.SetBinding(rotate, RotateTransform.AngleProperty, bindingAngle);
            rectangle.RenderTransform = rotate;

        }
        public void SetBinding(ref TextBox textBox)
        {
            BindingOperations.ClearAllBindings(textBox);
            var bindingGroup = new BindingGroup();
            textBox.BindingGroup = bindingGroup;
            var textFieldValue = TextFieldProperties[textBox.Name];
            textBox.SetBinding(TextBox.TextProperty, new Binding()
            {
                Source = TextFieldProperties,
                Path = new PropertyPath("[" + textFieldValue.Name + "].Value"),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            Binding bindingWidth = new Binding();
            bindingWidth.Source = TextFieldProperties;
            bindingWidth.Path = new PropertyPath("[" + textFieldValue.Name + "].Width");
            bindingWidth.Mode = BindingMode.TwoWay;
            bindingWidth.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            textBox.SetBinding(TextBox.WidthProperty, bindingWidth);

            Binding bindingHeight = new Binding();
            bindingHeight.Source = TextFieldProperties;
            bindingHeight.Path = new PropertyPath("[" + textFieldValue.Name + "].Height");
            bindingHeight.Mode = BindingMode.TwoWay;
            bindingHeight.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            textBox.SetBinding(TextBox.HeightProperty, bindingHeight);

            Binding bindingFontFamily = new Binding();
            bindingFontFamily.Source = TextFieldProperties;
            bindingFontFamily.Converter = new StringToFontFamilyConverter();
            bindingFontFamily.Path = new PropertyPath("[" + textFieldValue.Name + "].FontFamily");
            bindingFontFamily.Mode = BindingMode.TwoWay;
            bindingFontFamily.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            textBox.SetBinding(TextBox.FontFamilyProperty, bindingFontFamily);

            Binding bindingFontSize = new Binding();
            bindingFontSize.Source = TextFieldProperties;
            bindingFontSize.Path = new PropertyPath("[" + textFieldValue.Name + "].FontSize");
            bindingFontSize.Mode = BindingMode.TwoWay;
            bindingFontSize.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            textBox.SetBinding(TextBox.FontSizeProperty, bindingFontSize);

            Binding bindingForeground = new Binding();
            bindingForeground.Source = TextFieldProperties;
            bindingForeground.Converter = new ColorToSolidColorBrushConverter();
            bindingForeground.Path = new PropertyPath("[" + textFieldValue.Name + "].Foreground");
            bindingForeground.Mode = BindingMode.TwoWay;
            bindingForeground.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            textBox.SetBinding(TextBox.ForegroundProperty, bindingForeground);

            Binding bindingAlignment = new Binding();
            bindingAlignment.Source = TextFieldProperties;
            bindingAlignment.Path = new PropertyPath("[" + textFieldValue.Name + "].TextAlignment");
            bindingAlignment.Mode = BindingMode.TwoWay;
            bindingAlignment.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            textBox.SetBinding(TextBox.TextAlignmentProperty, bindingAlignment);

            Binding bindingHorizontalAlignment = new Binding();
            bindingHorizontalAlignment.Source = TextFieldProperties;
            bindingHorizontalAlignment.Path = new PropertyPath("[" + textFieldValue.Name + "].HorizontalAlignment");
            bindingHorizontalAlignment.Mode = BindingMode.TwoWay;
            bindingHorizontalAlignment.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            textBox.SetBinding(TextBox.HorizontalAlignmentProperty, bindingHorizontalAlignment);

            Binding bindingVerticalAlignment = new Binding();
            bindingVerticalAlignment.Source = TextFieldProperties;
            bindingVerticalAlignment.Path = new PropertyPath("[" + textFieldValue.Name + "].VerticalAlignment");
            bindingVerticalAlignment.Mode = BindingMode.TwoWay;
            bindingVerticalAlignment.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            textBox.SetBinding(TextBox.VerticalAlignmentProperty, bindingVerticalAlignment);


            Binding bindingBackground = new Binding();
            bindingBackground.Source = TextFieldProperties;
            bindingBackground.Converter = new ColorToSolidColorBrushConverter();
            bindingBackground.Path = new PropertyPath("[" + textFieldValue.Name + "].Background");
            bindingBackground.Mode = BindingMode.TwoWay;
            bindingBackground.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            textBox.SetBinding(TextBox.BackgroundProperty, bindingBackground);

            textBox.RenderTransformOrigin = new Point(0.5, 0.5);
            Binding bindingAngle = new Binding();
            bindingAngle.Converter = new AngleToRotationConverter();
            bindingAngle.Mode = BindingMode.TwoWay;
            bindingAngle.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            bindingAngle.Source = TextFieldProperties;
            bindingAngle.Path = new PropertyPath("[" + textFieldValue.Name + "].Angle");
            textBox.SetBinding(TextBox.RenderTransformProperty, bindingAngle);

            Binding bindingLeft = new Binding()
            {
                Mode = BindingMode.TwoWay,
                Source = TextFieldProperties,
                Path = new PropertyPath("[" + textFieldValue.Name + "].Left"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            };
            BindingOperations.SetBinding(textBox, Canvas.LeftProperty, bindingLeft);

            Binding bindingTop = new Binding()
            {
                Mode = BindingMode.TwoWay,
                Source = TextFieldProperties,
                Path = new PropertyPath("[" + textFieldValue.Name + "].Top"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            };
            BindingOperations.SetBinding(textBox, Canvas.TopProperty, bindingTop);
        }
        public void BarcodeSetBinding(ref Image barcodeImage)
        {
            BindingOperations.ClearAllBindings(barcodeImage);
            BindingGroup group = new BindingGroup();
            barcodeImage.BindingGroup = group;
            var barcodeImageProperties = Barcodes[barcodeImage.Name];
            Binding binding = new Binding();
            binding.Converter = new BarcodeImagePropertiesToBitmapImageConverter();
            binding.Mode = BindingMode.OneWay;
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            binding.Source = Barcodes;
            binding.ConverterParameter = barcodeImageProperties.Name;
            barcodeImage.SetBinding(Image.SourceProperty, binding);

            barcodeImage.RenderTransformOrigin = new Point(0.5, 0.5);
            var bindingAngle = new Binding();
            bindingAngle.Converter = new AngleToRotationConverter();
            bindingAngle.Source = Barcodes;
            bindingAngle.Path = new PropertyPath("[" + barcodeImageProperties.Name + "].Angle");
            bindingAngle.Mode = BindingMode.TwoWay;
            bindingAngle.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            barcodeImage.SetBinding(Image.RenderTransformProperty, bindingAngle);

            Binding bindingWidth = new Binding()
            {
                Source = Barcodes,
                Path = new PropertyPath("[" + barcodeImage.Name + "].Width"),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            };
            barcodeImage.SetBinding(Image.WidthProperty, bindingWidth);

            var bindingHeight = new Binding()
            {
                Source = Barcodes,
                Path = new PropertyPath("[" + barcodeImage.Name + "].Height"),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            };
            barcodeImage.SetBinding(Image.HeightProperty, bindingHeight);

            Binding bindingLeft = new Binding()
            {
                Mode = BindingMode.TwoWay,
                Source = Barcodes,
                Path = new PropertyPath("[" + barcodeImageProperties.Name + "].Left"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            };
            BindingOperations.SetBinding(barcodeImage, Canvas.LeftProperty, bindingLeft);
            Binding bindingTop = new Binding()
            {
                Mode = BindingMode.TwoWay,
                Source = Barcodes,
                Path = new PropertyPath("[" + barcodeImageProperties.Name + "].Top"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            };
            BindingOperations.SetBinding(barcodeImage, Canvas.TopProperty, bindingTop);

        }
        public void QRCodeSetBinding(ref Image qrcodeImage)
        {
            BindingOperations.ClearAllBindings(qrcodeImage);
            var qrCodeImageProperties = QRCodes[qrcodeImage.Name];
            var group = new BindingGroup();
            qrcodeImage.BindingGroup = group;

            Binding binding = new Binding();
            binding.Converter = new SvgStringToBitmapImageConverter();
            binding.Mode = BindingMode.OneWay;
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            binding.Source = QRCodes;
            binding.Path = new PropertyPath("[" + qrCodeImageProperties.Name + "].Value");
            qrcodeImage.SetBinding(Image.SourceProperty, binding);

            qrcodeImage.RenderTransformOrigin = new Point(0.5, 0.5);
            var bindingAngle = new Binding();
            bindingAngle.Converter = new AngleToRotationConverter();
            bindingAngle.Source = QRCodes;
            bindingAngle.Path = new PropertyPath("[" + qrCodeImageProperties.Name + "].Angle");
            bindingAngle.Mode = BindingMode.TwoWay;
            bindingAngle.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(qrcodeImage, Image.RenderTransformProperty, bindingAngle);

            Binding bindingWidth = new Binding();
            bindingWidth.Converter = new QRCodePropertiesToImagePropertieConverter();
            bindingWidth.ConverterParameter = qrCodeImageProperties.Name;
            bindingWidth.Mode = BindingMode.TwoWay;
            bindingWidth.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            bindingWidth.Source = QRCodes;
            bindingWidth.Path = new PropertyPath("[" + qrcodeImage.Name + "].Width");
            qrcodeImage.SetBinding(Image.WidthProperty, bindingWidth);

            Binding bindingHeight = new Binding();
            bindingHeight.Converter = new QRCodePropertiesToImagePropertieConverter();
            bindingHeight.ConverterParameter = qrcodeImage.Name;
            bindingHeight.Mode = BindingMode.TwoWay;
            bindingHeight.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            bindingHeight.Source = QRCodes;
            bindingHeight.Path = new PropertyPath("[" + qrCodeImageProperties.Name + "].Height");
            qrcodeImage.SetBinding(Image.HeightProperty, bindingHeight);

            Binding bindingLeft = new Binding()
            {
                Mode = BindingMode.TwoWay,
                Source = QRCodes,
                Path = new PropertyPath("[" + qrCodeImageProperties.Name + "].Left"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            };
            BindingOperations.SetBinding(qrcodeImage, Canvas.LeftProperty, bindingLeft);
            Binding bindingTop = new Binding()
            {
                Mode = BindingMode.TwoWay,
                Source = QRCodes,
                Path = new PropertyPath("[" + qrCodeImageProperties.Name + "].Top"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            };
            BindingOperations.SetBinding(qrcodeImage, Canvas.TopProperty, bindingTop);

        }
        public void ImageSetBinding(ref Image image)
        {
            BindingOperations.ClearAllBindings(image);
            var imageProperties = Images[image.Name];
            var bindingGroup = new BindingGroup();
            image.BindingGroup = bindingGroup;
            Binding binding = new Binding();
            binding.Converter = new ByteArrayToBitmapImageConverter();
            binding.Mode = BindingMode.OneWay;
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            binding.Source = Images;
            binding.Path = new PropertyPath("[" + imageProperties.Name + "].ImageSource");
            image.SetBinding(Image.SourceProperty, binding);

            image.RenderTransformOrigin = new Point(0.5, 0.5);
            var bindingAngle = new Binding();
            bindingAngle.Converter = new AngleToRotationConverter();
            bindingAngle.Source = Images;
            bindingAngle.Path = new PropertyPath("[" + imageProperties.Name + "].Angle");
            bindingAngle.Mode = BindingMode.TwoWay;
            bindingAngle.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            image.SetBinding(Image.RenderTransformProperty, bindingAngle);

            Binding bindingWidth = new Binding();
            bindingWidth.Mode = BindingMode.TwoWay;
            bindingWidth.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            bindingWidth.Source = Images;
            bindingWidth.Path = new PropertyPath("[" + imageProperties.Name + "].Width");
            image.SetBinding(Image.WidthProperty, bindingWidth);

            Binding bindingHeight = new Binding();
            bindingHeight.Mode = BindingMode.TwoWay;
            bindingHeight.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            bindingHeight.Source = Images;
            bindingHeight.Path = new PropertyPath("[" + imageProperties.Name + "].Height");
            image.SetBinding(Image.HeightProperty, bindingHeight);
            Binding bindingLeft = new Binding()
            {
                Mode = BindingMode.TwoWay,
                Source = Images,
                Path = new PropertyPath("[" + imageProperties.Name + "].Left"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            };
            BindingOperations.SetBinding(image, Canvas.LeftProperty, bindingLeft);
            Binding bindingTop = new Binding()
            {
                Mode = BindingMode.TwoWay,
                Source = Images,
                Path = new PropertyPath("[" + imageProperties.Name + "].Top"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            };
            BindingOperations.SetBinding(image, Canvas.TopProperty, bindingTop);
        }
        public void SetBindingPolygonProperties(ref Polygon polygon)
    {
        var bindingGroup = new BindingGroup();
        bindingGroup.Name = "BindingGroup_" + polygon.Name;
        polygon.BindingGroup = bindingGroup;
        var polygonFigure = Polygons[polygon.Name];

        Binding bindingPoints = new Binding
        {
            Source = Polygons,
            Path = new PropertyPath($"[{polygonFigure.Name}].Points"),
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        };

        BindingOperations.SetBinding(
            polygon,
            Polygon.PointsProperty,
            bindingPoints);


        Binding bindingThickness = new Binding();
        bindingThickness.Source = Polygons;
        bindingThickness.Path = new PropertyPath($"[{polygonFigure.Name}].StrokeThickness");
        bindingThickness.Mode = BindingMode.TwoWay;
        bindingThickness.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        bindingThickness.BindingGroupName = bindingGroup.Name;
        polygon.SetBinding(Polygon.StrokeThicknessProperty, bindingThickness);

        Binding bindingStroke = new Binding();
        bindingStroke.Source = Polygons;
        bindingStroke.Converter = new ColorToBrushConverter();
        bindingStroke.Path = new PropertyPath($"[{polygonFigure.Name}].Stroke");
        bindingStroke.Mode = BindingMode.TwoWay;
        bindingStroke.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        polygon.SetBinding(Polygon.StrokeProperty, bindingStroke);

        Binding bindingLeft = new Binding()
        {
            Mode = BindingMode.TwoWay,
            Source = Polygons,
            Path = new PropertyPath($"[{polygonFigure.Name}].Left"),
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        };
        BindingOperations.SetBinding(polygon, Canvas.LeftProperty, bindingLeft);
        Binding bindingTop = new Binding()
        {
            Mode = BindingMode.TwoWay,
            Source = Polygons,
            Path = new PropertyPath($"[{polygonFigure.Name}].Top"),
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        };
        BindingOperations.SetBinding(polygon, Canvas.TopProperty, bindingTop);
    }

    private async Task SetSectionBindingsAsync(Canvas templateCanvas, TemplateRecord template, PropertyInfo[] properties, object model = null)
        {

            // templateCanvas.BeginInit();
            // Десериализация и загрузка макета
            if (Barcodes == null)
            {
                Barcodes = new Dictionary<string, BarcodeImageProperties>();
            }
            else
            {
                Barcodes.Clear();
            }
            foreach (var barcodeImageProperty in template.barcodeImage)
            {
                if (model != null && barcodeImageProperty.DataSourceType == DataSourceType.Database)
                {
                    var prop = properties.First(x => x.Name == barcodeImageProperty.DataFieldName);
                    if (prop != null)
                    {
                        var value = prop.GetValue(model);
                        if (value is string s)
                        {
                            barcodeImageProperty.Barcode = s;
                        }
                    }
                }
                Barcodes[barcodeImageProperty.Name] = barcodeImageProperty;
                var image = templateCanvas.Children.OfType<Image>().FirstOrDefault(x => x.Name == barcodeImageProperty.Name);
                if (image == null)
                {
                    continue;
                }

                BarcodeSetBinding(ref image);
                foreach (var bindingExp in image.BindingGroup.BindingExpressions)
                {
                    bindingExp.UpdateTarget();
                }
            }
            if (QRCodes == null)
            {
                QRCodes = new Dictionary<string, QRCodeImageProperties>();
            }
            else
            {
                QRCodes.Clear();
            }
            foreach (var qrcodeImageProperty in template.qRCodeImages)
            {
                QRCodes[qrcodeImageProperty.Name] = qrcodeImageProperty;
                if (model != null && qrcodeImageProperty.DataSourceType == DataSourceType.Database)
                {
                    var prop = properties.First(x => x.Name == qrcodeImageProperty.DataFieldName);
                    if (prop != null)
                    {
                        var value = prop.GetValue(model);

                        if (value is byte[] blob)
                        {

                            // Detect UTF‑16 BOM
                            if (blob.Length >= 2 && blob[0] == 0xFF && blob[1] == 0xFE)
                            {
                                qrcodeImageProperty.Value = Encoding.Unicode.GetString(blob); // UTF‑16 LE
                            }
                            else if (blob.Length >= 2 && blob[0] == 0xFE && blob[1] == 0xFF)
                            {
                                qrcodeImageProperty.Value = Encoding.BigEndianUnicode.GetString(blob); // UTF‑16 BE
                            }
                            else
                            {
                                qrcodeImageProperty.Value = Encoding.UTF8.GetString(blob); // fallback
                            }
                        }
                    }

                }

                var image = templateCanvas.Children.OfType<Image>().FirstOrDefault(x => x.Name == qrcodeImageProperty.Name);
                if (image == null)
                {
                    continue;
                }
                QRCodeSetBinding(ref image);
                foreach (var bindingExp in image.BindingGroup.BindingExpressions)
                {
                    bindingExp.UpdateTarget();
                }
            }

            if (TextFieldProperties == null)
            {

                TextFieldProperties = new Dictionary<string, TextFieldValue>();
            }
            else
            {
                TextFieldProperties.Clear();

            }
            if (template.textFieldValues != null && template.textFieldValues.Count > 0)
            {

                foreach (var textFieldValue in template.textFieldValues)
                {
                    TextFieldProperties[textFieldValue.Name] = textFieldValue;
                    if (model != null && textFieldValue.DataSourceType == DataSourceType.Database)
                    {
                        var value = properties.First(x => x.Name == textFieldValue.DataFieldName)?.GetValue(model);
                        if (value != null && value is string s)
                        {
                            textFieldValue.Value = s;
                        }
                    }
                    var textBox = templateCanvas.Children.OfType<TextBox>().FirstOrDefault(x => x.Name == textFieldValue.Name);
                    if (textBox == null)
                    {
                        continue;
                    }
                    SetBinding(ref textBox);
                    foreach (var bindingExp in textBox.BindingGroup.BindingExpressions)
                    {
                        bindingExp.UpdateTarget();
                    }
                }
            }
            if (Images == null)
            {
                Images = new Dictionary<string, ImageProperties>();
            }
            else
            {
                Images.Clear();
            }
            if (template.imageProperties != null && template.imageProperties.Count() > 0)
            {


                foreach (ImageProperties imageProperty in template.imageProperties)
                {
                    if (model != null && imageProperty.DataSourceType == DataSourceType.Database)
                    {
                        var value = properties.First(x => x.Name == imageProperty.DataFieldName)?.GetValue(model);

                        switch (imageProperty.DataSourceFieldType)
                        {
                            case FieldTypes.URL:
                                {
                                    if (value is string s)
                                    {
                                        imageProperty.ImageSource = await Task.Run(() => loader.LoadForPrintAsync(s)).ContinueWith(t =>
                                        {
                                            if (t.IsCompletedSuccessfully)
                                            {
                                                return ImagePipelineLoader.BitmapImageToBytes(t.Result);
                                            }
                                            else
                                            {
                                                // Log or handle the error
                                                return null;
                                            }
                                        });
                                    }
                                }
                                break;
                            case FieldTypes.Base64String:
                                {
                                    if (value is string base64String && !string.IsNullOrEmpty(base64String))
                                    {
                                        imageProperty.ImageSource = Convert.FromBase64String(base64String);
                                    }
                                }
                                break;
                            case FieldTypes.ByteArray:

                                if (value is byte[] byteArray)
                                {
                                    imageProperty.ImageSource = byteArray;
                                }
                                break;
                            case FieldTypes.String:
                                {
                                    imageProperty.ImageSource = null;
                                    if (value is string url && !string.IsNullOrEmpty(url))
                                    {
                                        var resolvedUrl = InputResolver.Resolve(url);
                                        if (resolvedUrl != null)
                                        {
                                            if (resolvedUrl.Kind == InputKind.ArbitraryText)
                                            {
                                                //var bitmap = TextToBitmapConverter.ConvertTextToBitmap(resolvedUrl.Original,new System.Drawing.Font("Arial",12),
                                                //    System.Drawing.Color.Red,System.Drawing.Color.Transparent,0);

                                                var bitmap = BitmapExtensions.ErrorMessageBitmap((int)imageProperty.Width, (int)imageProperty.Height, resolvedUrl.Original);
                                                imageProperty.ImageSource = TextToBitmapConverter.BitmapToBytes(bitmap);
                                                break;
                                            }
                                        }

                                        try
                                        {
                                            var bitmap = await loader.LoadForPrintAsync(url);
                                            imageProperty.ImageSource = ImagePipelineLoader.BitmapImageToBytes(bitmap);
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Error($"LoadForPrintAsync({url}) {ex.Message}");
                                        }


                                    }
                                }
                                break;
                            default:
                                Log.Information($"Code for default case{imageProperty.DataSourceFieldType}");  // Code for default case
                                break;
                        }

                    }
                    Images[imageProperty.Name] = imageProperty;
                    var image = templateCanvas.Children.OfType<Image>().FirstOrDefault(x => x.Name == imageProperty.Name);
                    if (image == null)
                    {
                        continue;
                    }
                    image.Source = null;
                    ImageSetBinding(ref image);
                    foreach (var bindingexp in image.BindingGroup.BindingExpressions)
                    {
                        bindingexp.UpdateTarget();
                    }
                }
            }
            if (RectangleFigures == null)
            {
                RectangleFigures = new Dictionary<string, RectangleFigureProperties>();
            }
            else
            {
                RectangleFigures.Clear();
            }
            if (template.rectangleFigures != null && template.rectangleFigures.Count() > 0)
            {
                foreach (RectangleFigureProperties rectangleProperty in template.rectangleFigures)
                {
                    RectangleFigures[rectangleProperty.Name] = rectangleProperty;
                    var rectangle = templateCanvas.Children.OfType<Rectangle>().FirstOrDefault(x => x.Name == rectangleProperty.Name);
                    if (rectangle == null)
                    {
                        continue;
                    }
                    SetBindingRectangleProperties(ref rectangle);
                    foreach (var bindingexp in rectangle.BindingGroup.BindingExpressions)
                    {
                        bindingexp.UpdateTarget();
                    }
                }
            }
            if (Lines == null)
            {
                Lines = new Dictionary<string, LineProperties>();
            }
            else
            {
                Lines.Clear();
            }
            if (template.lineProperties != null && template.lineProperties.Count() > 0)
            {

                foreach (LineProperties lineProperty in template.lineProperties)
                {
                    Lines[lineProperty.Name] = lineProperty;
                    var line = templateCanvas.Children.OfType<Line>().FirstOrDefault(x => x.Name.Equals(lineProperty.Name, StringComparison.OrdinalIgnoreCase));
                    if (line == null)
                    {
                        continue;
                    }
                    SetBindingLineProperties(ref line);
                    var group = line.BindingGroup;
                    if (group != null)
                    {
                        foreach (var exp in group.BindingExpressions)
                        {
                            exp.UpdateTarget();
                        }
                    }
                }
            }
        if (Polygons == null)
        {
            Polygons = new Dictionary<string, PolygonProperties>();
        }
        else
        {
            Polygons.Clear();
        }
        if (template.polygonProperties != null && template.polygonProperties.Count() > 0)
        {

            foreach (var polygonProperty in template.polygonProperties)
            {
                Polygons[polygonProperty.Name] = polygonProperty;
                var polygon = templateCanvas.Children.OfType<Polygon>().FirstOrDefault(x => x.Name.Equals(polygonProperty.Name, StringComparison.OrdinalIgnoreCase));
                if (polygon == null)
                {
                    continue;
                }
                SetBindingPolygonProperties(ref polygon);
                var group = polygon.BindingGroup;
                if (group != null)
                {
                    foreach (var exp in group.BindingExpressions)
                    {
                        exp.UpdateTarget();
                    }
                }
            }
        }
        //  templateCanvas.EndInit();

    }
    private BitmapImage LoadFromBytes(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
    }

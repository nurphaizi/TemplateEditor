using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;
namespace TemplateEdit;

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
/// 
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Markup;
using Serilog;
using Color = System.Drawing.Color;
public static class TSPL_FONTS
{
    public static  readonly Dictionary<string, string> TSPL_Font = new Dictionary<string, string>
    {
        {"Monotye CG Triumvirate Bold Condensed,font width and height is stretchable","0"},
        {"Standard font (8×12 pixels)", "1"},
        {"Condensed font (6×8 pixels",  "2"},
        {"Large font (12×20 pixels)",   "3"},
        {"OCR-B style font (10×16 pixels)","4"},
        {"OCR-A style font (8×12 pixels)","5"},
        {"Bold large font (14×24 pixels)","6"},
        {"Extra large font (24×40 pixels)","7"},
        {"Wide font (16×16 pixels)","8"}
    };
    public static readonly Dictionary<string, string> TSPLFontMap = new Dictionary<string, string>()
    {
        {"Xprinter Font 1","1"},
        {"Xprinter Font 2","2"},
        {"Xprinter Font 3","3"},
        {"Xprinter Font 4","4"},
        {"Xprinter Font 5","5"},
        {"Xprinter OCR-B","6"},
        {"Xprinter OCR-B Font 7","7"},
        {"Xprinter OCR-A","8"},
        {"Xprinter Korean","K"},
        {"Xprinter-Sample Bar Code Font","39" },
        {"Xprinter Chinese Big5","TST24.BF2"},
        {"Xprinter Chinese GB","TSS24.BF2"}
    };
}

public class PrimaryFieldProperties
{
    // Enums default to the first defined value (index 0) automatically.
    public DataSourceType DataSourceType {get; set;}= DataSourceType.None;
    public FieldTypes DataSourceFieldType {get; set;} = FieldTypes.String;
   

    // Use string.Empty to avoid null-reference headaches in XAML bindings
    public string DataSourceName { get; set; } = string.Empty;
    public string DataFieldName { get; set; } = string.Empty;
    public string Name { get; set; } = "NewField";
    public string Value { get; set; } = string.Empty;

    // Layout defaults - 0 is often dangerous for Width/Height in WPF
    public double Width { get; set; } = 100.0;
    public double Height { get; set; } = 30.0;
    public double Left { get; set; } = 0;
    public double Top { get; set; } = 0;
    public double Angle { get; set; } = 0;

    // Typography
    public double FontSize { get; set; } = 12.0;
    public string FontFamily { get; set; } = "Segoe UI";
    public System.Windows.FontWeight FontWeight { get; set; } = System.Windows.FontWeights.Normal;
    public System.Windows.FontStyle FontStyle { get; set; } = System.Windows.FontStyles.Normal;

    // Visuals
    public System.Windows.Media.Color Background { get; set; } = System.Windows.Media.Colors.Transparent;
    public System.Windows.Media.Color Foreground { get; set; } = System.Windows.Media.Colors.Black;
    public Boolean ConvertToBitmap { get; set; } = false;
}
public class RectangleFigureProperties : PrimaryFieldProperties

{
    public  Thickness Thickness { get; set; } = new Thickness(1);
    public double RadiusX { get; set; } = 0;
    public double RadiusY { get; set; } = 0;
    public double Opacity { get; set; } = 1.0;
    public System.Windows.Media.Color Stroke { get; set; } = System.Windows.Media.Colors.Black;
    public System.Windows.Media.Color Fill { get; set; } = System.Windows.Media.Colors.Transparent;
    public double StrokeThickness { get; set; } = 1.0;
    public Stretch Stretch { get; set; } = Stretch.Fill;
    public RectangleFigureProperties()
    {
        Height = 25;
        Width=50;
        Thickness = new Thickness(1);
        RadiusX = 0;
        RadiusY = 0;

    }
}
public class LineProperties : PrimaryFieldProperties

{
    public Thickness Thickness { get; set; } = new Thickness(1);
    public double Opacity { get; set; } = 1.0;
    public System.Windows.Media.Color Stroke { get; set; } = System.Windows.Media.Colors.Black;
    public double StrokeThickness { get; set; } = 3.0;
    public double X1 { get; set; } = 0;
    public double Y1 { get; set; } = 0;
    public double X2 { get; set; } = 0;
    public double Y2 { get; set; } = 0;
    public PenLineCap StrokeStartLineCap { get; set; } = PenLineCap.Flat;
    public PenLineCap StrokeEndLineCap { get; set; } = PenLineCap.Flat;
    public Boolean IsDashed { get; set; } = false;
   
}

public class PolygonProperties : PrimaryFieldProperties
{
    public Thickness Thickness { get; set; } = new Thickness(1);
    public double Opacity { get; set; } = 1.0;
    public System.Windows.Media.Color Stroke { get; set; } = System.Windows.Media.Colors.Black;
    public double StrokeThickness { get; set; } = 3.0;
    public System.Windows.Point[] Vertices;
    public PenLineCap StrokeStartLineCap { get; set; } = PenLineCap.Flat;
    public PenLineCap StrokeEndLineCap { get; set; } = PenLineCap.Flat;
    public PointCollection Points
    {
        get
        {

            return new PointCollection(Vertices);
            
        }
        set => Vertices = value.ToArray();
    }
}
public class TextFieldValue:PrimaryFieldProperties
{ 
    public TextWrapping TextWrapping { get; set; } = TextWrapping.NoWrap;
    public bool AcceptsReturn {get; set; } = false;
    public string Padding { get; set; } = "4";
    public string TSPLFont { get; set; } = "3";
    public double XMultiplication { get; set; } = 1;
    public double YMultiplication { get; set; } = 1;
    public TextFieldValue()
    {
        Height = 25;
        Value = "Образец текста";
    }

    public TextFieldValue(string name, string value) 
    {
        this.Name = name;
        this.Value = value;
        Height = 25;
    }
    public TextAlignment TextAlignment { get; set; } = TextAlignment.Left;
    public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Left;
    public VerticalAlignment VerticalAlignment  {get;set;} = VerticalAlignment.Top;



}
public class ImageProperties : PrimaryFieldProperties
{
    // Initialize strings to Empty rather than null to avoid binding issues
    private string _ImagePath = string.Empty;
    public string ImagePath
    {
        get => _ImagePath;
        set => _ImagePath = value ?? string.Empty;
    }

    // Initialize byte arrays to an empty array to prevent null checks in converters
    private byte[] _ImageSource = Array.Empty<byte>();
    public byte[] ImageSource
    {
        get => _ImageSource;
        set => _ImageSource = value ?? Array.Empty<byte>();
    } 


}

public class BarcodeImageProperties : PrimaryFieldProperties
{
    private string _Barcode=string.Empty;
    public string Barcode
    {
        get => _Barcode;
        set => _Barcode = value;
    }
    private BarcodeFormat _BarcodeFormat=BarcodeFormat.EAN_13;
    public BarcodeFormat BarcodeFormat
    {
        get => _BarcodeFormat;
        set => _BarcodeFormat = value;
    }
    public System.Windows.Media.Color BarcodeBackground { get; set; } = Colors.Transparent;
    public System.Windows.Media.Color BarcodeForeground { get; set; } = Colors.Black;
    public bool Readable { get; set; } = false; //Print human-readable text (0=no, 1=yes)
    public int Narrow { get; set; }  //Narrow bar width (dots)
    public int Wide { get; set;}  //Wide bar width multiplier  

}
public class QRCodeImageProperties : PrimaryFieldProperties
{
    private string _QRCodeContent=string.Empty;
}
public class AngleToRotationConverter : IValueConverter
    {
    object IValueConverter.Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double angle) return null;
        return new RotateTransform(angle);
    }
    object IValueConverter.ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not RotateTransform rotateTransform) return null;
        return rotateTransform.Angle;

    }

}

public class StringToTextConverter : IValueConverter
{
    object IValueConverter.Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not KeyValueItem keyValue) return string.Empty;
        return (string)keyValue.Value;
    }
    object IValueConverter.ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
    {
        return value;

    }

}


public class TextBoxNameToTextPropertyConverter : IValueConverter
{
    object IValueConverter.Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ObservableCollection<TextFieldValue> textFieldValues) return null;
        if (textFieldValues.Count == 0) return null;
        if (parameter is not string name || string.IsNullOrWhiteSpace(name)) return null;
        var textFieldValue = textFieldValues.FirstOrDefault(x => x.Name == name);
        if (textFieldValue == null) return null;
        return textFieldValue.Value;

    }
    object IValueConverter.ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
    {
        return value;

    }

}
public class ByteArrayToBitmapImageConverter : IValueConverter
{
   static public BitmapImage ConvertByteArrayToBitMapImage(byte[] imageByteArray)
    {
        BitmapImage img = new BitmapImage();
        if (imageByteArray == null || imageByteArray.Length == 0)
            return img;
        using (MemoryStream memStream = new MemoryStream(imageByteArray))
        {
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.StreamSource = memStream;
            img.EndInit();
            img.Freeze();
        }
        return img;
    }
    public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
    {
        var imageByteArray = value as byte[];
        if (imageByteArray == null) return null;
        var bitmapImage = ConvertByteArrayToBitMapImage(imageByteArray);
        return bitmapImage;
    }

    public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}
public class SvgStringToBitmapImageConverter : IValueConverter
{

public static BitmapImage ConvertStringToBitMapImage(string svgContent)
    {
        
        byte[] imageByteArray = Encoding.UTF8.GetBytes(svgContent);
        if (imageByteArray == null || imageByteArray.Length == 0)
        {
            return TextToBitmapConverter.BitmapToBitmapImage(TextToBitmapConverter.ConvertTextToBitmap("Нет данных",new Font("Arial",16),Color.Red,Color.Azure,0));
        }

        WpfDrawingSettings settings = new WpfDrawingSettings();
        settings.IncludeRuntime = true;
        settings.TextAsGeometry = false;
        settings.CanUseBitmap = false;
        StreamSvgConverter converter = new StreamSvgConverter(settings);
        BitmapImage img = new BitmapImage();
        
        if (imageByteArray == null || imageByteArray.Length == 0)
        {
            return img;
        }
        using (MemoryStream memStream = new MemoryStream(imageByteArray))
        {
            using (MemoryStream imageStream = new MemoryStream())
            {
                converter.Convert(memStream, imageStream);
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.StreamSource = imageStream;
                // img.Rotation = Rotation.Rotate90;
                img.EndInit();
                img.Freeze();
            }
        }
        return img;
    }
    public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
    {
        var svgContent = value as string;
        if (string.IsNullOrWhiteSpace(svgContent)) return TextToBitmapConverter.BitmapToBitmapImage(TextToBitmapConverter.ConvertTextToBitmap("Нет данных", new Font("Arial", 16), Color.Red, Color.Azure, 0)); ;
        return ConvertStringToBitMapImage(svgContent);
    }

    public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}


public class BarcodeStringToBitmapImageConverter : IMultiValueConverter
{
public static System.Drawing.Color ConvertMediaColorToDrawingColor(System.Windows.Media.Color mediaColor)
{
    return System.Drawing.Color.FromArgb(mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B);
}

public static BitmapImage GenerateBarcode(string barcode, BarcodeFormat barcodeFormat,int width,int height, string fontFamily, float fontSize
        ,System.Drawing.Color fgColor, System.Drawing.Color bgColor)
    {
        System.Drawing.Font font = null;
        if (fontSize <= 0.0f)
        {
            fontSize = 12.0f;
        }

        if (fontFamily == null)
        {
            font = new System.Drawing.Font(SystemFonts.DefaultFont.Name, 12.0f);
        }
        else
        {
            font = new System.Drawing.Font(fontFamily, fontSize);
        }
        ArgumentException.ThrowIfNullOrEmpty(barcode);
        ArgumentNullException.ThrowIfNull(font);
        System.Drawing.Bitmap bitmap;
        BarcodeWriter writer = null;
        try
        {
            writer = new BarcodeWriter
            {
                Format = barcodeFormat,

                Options = new EncodingOptions
                {
                    Width = width,
                    Height = height,
                    Margin = 0,
                    NoPadding = true,
                    PureBarcode = false,
                    GS1Format = true
                },
                Renderer = new BitmapRenderer()
                {
                    TextFont = new System.Drawing.Font(font, FontStyle.Regular),
                    Foreground = fgColor,
                    Background = bgColor
                }
            };

            bitmap = writer.Write(barcode);
        }
        catch (ArgumentException argEx)
        {
            MessageBox.Show("No encoder available for: " + argEx.Message, "Barcode Generation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            bitmap = BitmapExtensions.ErrorMessageBitmap(80,40, $"{barcodeFormat}");

        }
        catch (Exception ex)
        {
            throw new Exception("Error generating barcode: " + ex.Message);
        }
        
        using MemoryStream stream = new MemoryStream();
        stream.Position = 0;
        bitmap.Save(stream, ImageFormat.Png);
        stream.Seek(0, SeekOrigin.Begin);
        BitmapImage img = new BitmapImage();
        img.BeginInit();
        img.CacheOption = BitmapCacheOption.OnLoad;
        img.StreamSource = stream;
        img.EndInit();
        img.Freeze();
        return img;
    }
    public object Convert(object[] value, System.Type targetType, object parameter, CultureInfo culture)
    {
        var barcode = value[0] as string;
        if (barcode == null) return DependencyProperty.UnsetValue;
        if (string.IsNullOrEmpty(barcode) || string.IsNullOrWhiteSpace(barcode)) return DependencyProperty.UnsetValue;
        if (value[4] == null || value[5] == null) return DependencyProperty.UnsetValue; 
        int width =  System.Convert.ToInt32((double)value[4]);
        int height = System.Convert.ToInt32((double)value[5]);
        Color fgColor = ConvertMediaColorToDrawingColor((System.Windows.Media.Color)value[6]);
        Color bgColor = ConvertMediaColorToDrawingColor((System.Windows.Media.Color)value[7]);

        return GenerateBarcode((string)value[0], (BarcodeFormat)value[1],width,height, (string)value[2]
            , (float)value[3],fgColor,bgColor);
    }
    public object[] ConvertBack(object value, System.Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return null;
    }
}


public class BarcodeImagePropertiesToBitmapImageConverter : IValueConverter
{
    public static System.Drawing.Color ConvertMediaColorToDrawingColor(System.Windows.Media.Color mediaColor)
    {
        return System.Drawing.Color.FromArgb(mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B);
    }
    public static BitmapImage GenerateBarcode(string barcode, BarcodeFormat barcodeFormat,int width,int height, string fontFamily, float fontSize
                , System.Drawing.Color fgColor, System.Drawing.Color bgColor)
    {
        BarcodeWriter writer = null;
        System.Drawing.Bitmap bitmap;
        using MemoryStream stream = new MemoryStream();
        BitmapImage img = new BitmapImage();
        if (string.IsNullOrEmpty(barcode) || string.IsNullOrWhiteSpace(barcode))
        {
         bitmap = BitmapExtensions.ErrorMessageBitmap(80, 40, "Нет данных");
            stream.Position = 0;
            bitmap.Save(stream, ImageFormat.Png);
            stream.Seek(0, SeekOrigin.Begin);
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.StreamSource = stream;
            img.EndInit();
            img.Freeze();
            return img;

        }

        System.Drawing.Font font = null;
        if (fontSize <= 0.0f)
        {
            fontSize = 12.0f;
        }

        if (fontFamily == null)
        {
            font = new System.Drawing.Font(SystemFonts.DefaultFont.Name, 12.0f);
        }
        else
        {
            font = new System.Drawing.Font(fontFamily, fontSize);
        }
        ArgumentException.ThrowIfNullOrEmpty(barcode);
        ArgumentNullException.ThrowIfNull(font);

        try
        {
            writer = new BarcodeWriter
            {
                Format = barcodeFormat,

                Options = new EncodingOptions
                {
                    Width = width,
                    Height = height,
                    Margin = 0,
                    NoPadding = true,
                    PureBarcode = false,
                    GS1Format = true
                },
                Renderer = new BitmapRenderer()
                {
                    TextFont = new System.Drawing.Font(font, FontStyle.Regular),
                    Foreground = fgColor,
                    Background = bgColor
                }
            };
            bitmap = writer.Write(barcode);
        }
                catch (ArgumentException argEx)
        {
            MessageBox.Show("No encoder available for: " + argEx.Message, "Barcode Generation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            bitmap = BitmapExtensions.ErrorMessageBitmap(80, 40, $"{barcodeFormat}");
        }
        catch (Exception ex)
        {
            throw new Exception("Error generating barcode: " + ex.Message);
        }
        stream.Position = 0;
        bitmap.Save(stream, ImageFormat.Png);
        stream.Seek(0, SeekOrigin.Begin);
        img.BeginInit();
        img.CacheOption = BitmapCacheOption.OnLoad;
        img.StreamSource = stream;
        img.EndInit();
        img.Freeze();
        return img;
    }
    public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Dictionary<string,BarcodeImageProperties> barcodes) return null;
        var name = parameter as string;
        if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name)) return null;
        var barcodeImageProperties = barcodes[name];
        if (barcodeImageProperties == null) return null;
        Color fgColor = ConvertMediaColorToDrawingColor((System.Windows.Media.Color)barcodeImageProperties.BarcodeForeground);
        Color bgColor = ConvertMediaColorToDrawingColor((System.Windows.Media.Color)barcodeImageProperties.BarcodeBackground);

        return GenerateBarcode(barcodeImageProperties.Barcode, barcodeImageProperties.BarcodeFormat, (int)barcodeImageProperties.Width,(int)barcodeImageProperties.Height, barcodeImageProperties.FontFamily,(float)barcodeImageProperties.FontSize,fgColor,bgColor); 
    }

    public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}

public class QRCodePropertiesToImagePropertieConverter : IValueConverter
{
    object IValueConverter.Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
    {
        return (double)value;
    }
    object IValueConverter.ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
    {
        return (double)value;
    }
}
[ValueConversion(typeof(Color), typeof(SolidColorBrush))]
public class ColorToBrushConverter : IValueConverter
{
    public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
    {

        if (value is not System.Windows.Media.Color color)
            return null;
        return new SolidColorBrush(color)  ;
    }

    public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not SolidColorBrush brush)
        {
            return null;
        }
        return brush.Color;
    }
}

[ValueConversion(typeof(SolidColorBrush), typeof(Color))]
public class BrushToColorConverter : IValueConverter
{
    public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not SolidColorBrush brush)
        {
            return null;
        }
        return brush.Color;
    }

    public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
    {

        if (value is not System.Windows.Media.Color color)
            return null;
        return new SolidColorBrush(color);
    }
}


[ValueConversion(typeof(int), typeof(string))]
public class ConverterIntToString : IValueConverter
{
    public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return String.Empty;
        }
        return int.TryParse(value.ToString(), out int number) ? number.ToString() : String.Empty;
    }

    public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return 0;
        }
        string s = (string)value;
        if (string.IsNullOrEmpty(s) || string.IsNullOrWhiteSpace(s))
        {
            return 0;
        }
        int number = 0;
        if (int.TryParse(s, out number))
        {
            return number;
        }
        return 0;
    }
}



[ValueConversion(typeof(double), typeof(string))]
public class ConverterDoubleToString : IValueConverter
{
    public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return String.Empty;
        }
        var units = (string)parameter;
        if (string.IsNullOrEmpty(units) || units=="px")
        {
            return ((double)value).ToString("0");
        }
        if(units == "mm")
        {
            double number = (25.4 * (double)value) / 96;
            return number.ToString("0.##");
        }
        return String.Empty;
    }

    public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return 0;
        }
        string s = (string)value;
        if (string.IsNullOrEmpty(s) || string.IsNullOrWhiteSpace(s))
        {
            return 0.0f;
        }
        var units = (string)parameter;
        if (string.IsNullOrEmpty(units) ||units=="px")
        {
            double number = 0.0f;
            if (double.TryParse(s, out number))
            {
                return number;
            }
        }
        if (units == "mm")
        {
            double number = 0.0f;
            if (double.TryParse(s, out number))
            {
                return (number*96)/25.4;
            }
        }
        return 0.0f;
    }
}


[ValueConversion(typeof(System.Windows.Media.FontFamily), typeof(string))]
public class ConverterFontFamilyToString : IValueConverter
{
    public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
    {
        var ss = value;

        var fontFamily = (System.Windows.Media.FontFamily)value;
        if (fontFamily == null)
        {
            return String.Empty;
        }
        return fontFamily.Source;  
    }

    public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
    {
        string s = (string)value;
        if (string.IsNullOrEmpty(s) || string.IsNullOrWhiteSpace(s))
        {
            return null;
        }
        return  new FontFamily(s);
    }
}
public enum HumanReadable:short
{
    NOT_READABLE = 0, 
    LEFT = 1, 
    CENTER = 2 ,
    RIGHT = 3

}

public class SvgBlobToImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not byte[] bytes || bytes.Length == 0)
            return null;

        string svgText = Encoding.UTF8.GetString(bytes);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(svgText));

        var settings = new WpfDrawingSettings();
        var reader = new FileSvgReader(settings);

        DrawingGroup drawing = reader.Read(stream);
        if (drawing == null)
            return null;

        var image = new DrawingImage(drawing);
        image.Freeze();

        return image;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public static class SvgImageCache
{
    private static readonly ConcurrentDictionary<string, DrawingImage> _cache
        = new();

    public static DrawingImage GetOrCreate(byte[] svgBytes)
    {
        if (svgBytes == null || svgBytes.Length == 0)
            return null;

        string key = System.Convert.ToBase64String(svgBytes);

        return _cache.GetOrAdd(key, _ =>
        {
            string svgText = Encoding.UTF8.GetString(svgBytes);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(svgText));

            var settings = new WpfDrawingSettings();
            var reader = new FileSvgReader(settings);

            var drawing = reader.Read(stream);
            if (drawing == null)
                return null;

            drawing.Freeze();

            var image = new DrawingImage(drawing);
            image.Freeze();

            return image;
        });
    }
}

public class SvgBlobCachedConverter : IValueConverter
{
    private static readonly ConcurrentDictionary<string, DrawingImage> _cache
        = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not byte[] bytes || bytes.Length == 0)
            return null;

        //string key = System.Convert.ToBase64String(bytes);
        string key = System.Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(bytes));
        return _cache.GetOrAdd(key, _ =>
        {
            string svgText = Encoding.UTF8.GetString(bytes);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(svgText));

            var settings = new WpfDrawingSettings();
            var reader = new FileSvgReader(settings);

            var drawing = reader.Read(stream);
            if (drawing == null)
                return null;

            drawing.Freeze();

            var image = new DrawingImage(drawing);
            image.Freeze();

            return image;
        });
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public partial class MainWindow : Window
{

    public static  async Task<byte[]> GetImageBytes(string? mediaFile)
    {
        if (string.IsNullOrWhiteSpace(mediaFile))
        {
            return null;
        }

        // If it's an existing local file path
        if (File.Exists(mediaFile))
            return await File.ReadAllBytesAsync(mediaFile);
        

        // Try parse an absolute URI
        if (!Uri.TryCreate(mediaFile, UriKind.Absolute, out var uri))
        {
            // Try resolving relative path
            var full = Path.GetFullPath(mediaFile);
            if (File.Exists(full)) return await File.ReadAllBytesAsync(full);
            return null;
        }

        if (uri.IsFile)
        {
            var local = uri.LocalPath;
            if (File.Exists(local)) return await File.ReadAllBytesAsync(local);
            return null;
        }

        if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        {
            using var client = new HttpClient();
            using var response = await client.GetAsync(uri);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            return await response.Content.ReadAsByteArrayAsync();
        }

        // TODO: support data: URIs or other schemes as required
        return null;
    }
    public static  byte[] ConvertWebPToPng(byte[] webpBytes)
    {
        using var inputStream = new MemoryStream(webpBytes);
        using var image = SixLabors.ImageSharp.Image.Load(inputStream); // Auto-detects WebP format
        using var outputStream = new MemoryStream();
        image.Save(outputStream, new PngEncoder()); // Save as PNG
        return outputStream.ToArray(); // Return PNG bytes
    }
    public MainWindow()
    {
        InitializeComponent();
    }
}
using System.Collections;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using ExCSS;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Serilog;
using TemplateEdit;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;
public record TemplateRecord
{
    public CrystalReportSection Section { get; set;}
    public System.Collections.Generic.List<CanvasElement> elements=new();
    public System.Collections.Generic.List<TextBoxCanvasElement> textBoxCanvasElements=new();
    public System.Collections.Generic.List<ImageCanvasElement> imageCanvasElements = new();
    public System.Collections.Generic.List<TextFieldValue> textFieldValues = new();
    public System.Collections.Generic.List<ImageProperties> imageProperties = new();
    public System.Collections.Generic.List<BarcodeImageProperties> barcodeImage= new();
    public System.Collections.Generic.List<QRCodeImageProperties> qRCodeImages = new();
    public System.Collections.Generic.List<RectangleFigureProperties> rectangleFigures= new();
    public System.Collections.Generic.List<LineProperties> lineProperties = new();
    public System.Collections.Generic.List<PolygonProperties> polygonProperties = new();

}
public static class NetSQLiteTypeMapper
{
    public static string MapToSqlite(System.Type type)
    {
        if (type == typeof(bool)) return "INTEGER";
        if (type == typeof(byte) || type == typeof(sbyte) ||
            type == typeof(short) || type == typeof(int) || type == typeof(long))
            return "INTEGER";
        if (type == typeof(float) || type == typeof(double))
            return "REAL";
        if (type == typeof(decimal))
            return "TEXT"; // safer for precision
        if (type == typeof(string) || type == typeof(char) ||
            type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
            type == typeof(TimeSpan) || type == typeof(Guid))
            return "TEXT";
        if (type == typeof(byte[]))
            return "BLOB";
        return "NONE"; 
    }
}
public enum FieldTypes
{
    None,
    URL,
    ByteArray,
    ByteStream,
    Base64String,
    Boolean,
    Byte,
    SByte,
    Short,
    Int,
    Long,
    Float,
    Double,
    Decimal,
    String,
    Char,
    DateTime,
    DateTimeOffset,
    TimeSpan,
    Guid
}
public enum DataSourceType
{
    None,
    Embedded,
    Database,
    File
}
public class CompositeContractResolver : DefaultContractResolver
{
    public IList<IContractResolver> Resolvers { get; } = new List<IContractResolver>();

    public CompositeContractResolver(IEnumerable<IContractResolver> resolvers)
    {
        foreach (var resolver in resolvers)
            Resolvers.Add(resolver);
    }

    public override JsonContract ResolveContract(Type type)
    {
        JsonContract contract = null;

        foreach (var resolver in Resolvers)
        {
            contract = resolver.ResolveContract(type);
            if (contract != null)
                return contract;
        }

        return base.ResolveContract(type);
    }
}
public class IgnoreEmptyCollectionsResolver : DefaultContractResolver
{
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);

        property.ShouldSerialize = instance =>
        {
            var value = property.ValueProvider.GetValue(instance);

            if (value == null)
                return false;

            if (value is string s && string.IsNullOrWhiteSpace(s))
                return false;

            if (value is System.Collections.ICollection c && c.Count == 0)
                return false;

            return true;
        };

        return property;
    }
}
public class SkipEmptyResolver : DefaultContractResolver
{
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);

        property.ShouldSerialize = instance =>
        {
            var value = property.ValueProvider.GetValue(instance);

            // Skip null
            if (value == null)
                return false;

            // Skip empty or whitespace strings
            if (value is string s && string.IsNullOrWhiteSpace(s))
                return false;

            // Skip empty collections
            if (value is ICollection c && c.Count == 0)
                return false;

            return true;
        };

        return property;
    }
}
public static class JsonHelper
{
    public static CompositeContractResolver  resolver = new CompositeContractResolver(new IContractResolver[] { new SkipEmptyResolver(),
         new CamelCasePropertyNamesContractResolver(),
         new DefaultContractResolver() });
    public  static System.Windows.Media.Brush ColorToBrush(string color)
    {
        Brush brush;
        try
        {
            brush = (Brush)new BrushConverter().ConvertFromString("#FF336699");
        }
        catch
        {
            brush = Brushes.Transparent; // fallback
        }
        return brush;
    }

    public readonly static  JsonSerializerSettings options = new JsonSerializerSettings
    {                                                                                                  // красивый вывод с отступами
        NullValueHandling = NullValueHandling.Ignore,                                                  // игнорировать null-свойства
        DefaultValueHandling = DefaultValueHandling.Ignore,                                            // игнорировать значения по умолчанию
        MissingMemberHandling = MissingMemberHandling.Ignore,                                          // игнорировать лишние поля в JSON
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,                                          // избегать циклических ссылок
        TypeNameHandling = TypeNameHandling.Auto,                                                      // сохранять типы (для наслед }
        ContractResolver = resolver, 
        Converters = { new StringEnumConverter() ,new BrushJsonConverter()}
    };
    public static JsonSerializerSettings GetJsonSerializerSettings()=> options;

    // Serialize using Newtonsoft.Json
    public static string Serialize<T>(T obj)
    {
        try {
            var json = JsonConvert.SerializeObject(obj, options);
            return json;
        }
        catch (Exception ex)
        {
            var error = ex.Message;
            Log.Error($"Serialization error: {error}");
            return string.Empty;
        }
    }

    // Deserialize using Newtonsoft.Json
    
    public static T Deserialize<T>(string json)
    {
        try
        {
            if (string.IsNullOrEmpty(json))
            {
    
                return default(T);
            }
            var value = JsonConvert.DeserializeObject<T>(json, options);
            if (value == null)
            {
                return default(T);
            }
            return value;
        }
        catch (Exception ex)
        {
            var error = ex.Message;
            Log.Error($"Deserialization error: {error}");
            
            return default(T);
        }

    }
    public static T DeserializeFromStream<T>(Stream stream)
    {
        if (stream == null || !stream.CanRead)
            throw new ArgumentException("Stream is not readable.", nameof(stream));

        using (var sr = new StreamReader(stream))
        using (var reader = new JsonTextReader(sr))
        {
            var serializer = new JsonSerializer();
            return serializer.Deserialize<T>(reader);
        }
    }
}
public class CanvasElement
{
    public string Name
    {
        get;
        set;
    }
    public string Type
    {
        get;
        set;
    }
    public double Left
    {
        get;
        set;
    }
    public double Top
    {
        get;
        set;
    }
    public double Width
    {
        get;
        set;
    }
    public double Height
    {
        get;
        set;
    }
    public string Fill
    {
        get;
        set;
    }
    public string Stroke
    {
        get;
        set;
    }
    public string Color
    {
        get;
        set;
    }
    public double StrokeThickness
    {
        get;
        set;
    }
    public double RadiusX
    {
        get;
        set;
    }
    public double RadiusY
    {
        get;
        set;
    }
    public string HorizontalAlignment
    {
        get;
        set;
    }
    public string VerticalAlignment
    {
        get;
        set;
    }
    public double Opacity
    {
        get;
        set;
    }
    public string Stretch
    {
        get;
        set;
    }
    public double X1
    {
        get;
        set;
    }
    public double Y1
    {
        get;
        set;
    }
    public double X2
    {
        get;
        set;
    }
    public double Y2
    {
        get;
        set;
    }
    public string StrokeStartLineCap
    {
        get;
        set;
    }
    public string StrokeEndLineCap
    {
        get;
        set;
    }
    public System.Windows.Point[] Points
    {
        get;
        set;
    }
    public double[] StrokeDashArray
    {
        get;
        set;
    }
}
public class TextBoxCanvasElement 
{
    public required string Name
    {
        get; set;
    }
    public required string Type
    {
        get; set;
    }
    public double Left
    {
        get; set;
    }
    public double Top
    {
        get; set;
    }
    public double Width
    {
        get; set;
    }
    public double Height
    {
        get; set;
    }
    public double X1
    {
        get; set;
    }
    public double Y1
    {
        get; set;
    }
    public double X2
    {
        get; set;
    }
    public double Y2
    {
        get; set;
    }
    public string Fill //System.Windows.Media.Brush
    {
        get; set;
    }
    public string Stroke //System.Windows.Media.Brush
    {
        get; set;
    }
    public double StrokeThickness
    {
        get; set;
    }
    public string Color
    {
        get; set;
    }
    public string Stretch //System.Windows.Media.Stretch
    {
        get; set;
    }
    public double RadiusX
    {
        get; set;
    }
    public double RadiusY
    {
        get; set;
    }
    public string HorizontalAlignment // System.Windows.HorizontalAlignment
    {
        get; set;
    }
    public string VerticalAlignment  //System.Windows.VerticalAlignment
    {
        get; set;
    }
    public double Opacity
    {
        get; set;
    }
    public string DataSourceType   //DataSourceType
    {
        get; set;
    }
    public string? DataSourceName
    {
        get; set;
    }
    public double Angle
    {
        get; set;
    }
    public string StrokeStartLineCap  //PenLineCap
    {
        get; set;
    }
    public string StrokeEndLineCap //PenLineCap
    {
        get; set;
    }

    public string Text
    {
        get; set;
    }
    public string Background   //System.Windows.Media.Brush
    {
        get; set;
    }
    public string Foreground
    {
        get; set;
    }
    public string FontFamily   //System.Windows.Media.FontFamily.Source
    {
        get; set;
    }
    public double FontSize
    {
        get; set;
    }
    public string FontStyle  //System.Windows.FontStyle
    {
        get; set;
    }
    public string FontWeight //FontWeight
    {
        get; set;
    }
    public string FontStretch  //System.Windows.FontStretch
    {
        get; set;
    }
    public string TextWrapping  //System.Windows.TextWrapping 
    {
        get; set;
    }
    public Boolean AcceptsReturn
    {
        get; set;
    }
}
public class ImageCanvasElement
{
    public string Name
    {
        get;
        set;
    }
    public string Type
    {
        get;
        set;
    }
    public double Left
    {
        get;
        set;
    }
    public double Top
    {
        get;
        set;
    }
    public double Width
    {
        get;
        set;
    }
    public double Height
    {
        get;
        set;
    }
    public string Fill
    {
        get;
        set;
    }
    public string Stroke
    {
        get;
        set;
    }
    public string Color
    {
        get;
        set;
    }
    public double StrokeThickness
    {
        get;
        set;
    }
    public double RadiusX
    {
        get;
        set;
    }
    public double RadiusY
    {
        get;
        set;
    }
    public string HorizontalAlignment
    {
        get;
        set;
    }
    public string VerticalAlignment
    {
        get;
        set;
    }
    public double Opacity
    {
        get;
        set;
    }
    public string Stretch
    {
        get;
        set;
    }
    public double X1
    {
        get;
        set;
    }
    public double Y1
    {
        get;
        set;
    }
    public double X2
    {
        get;
        set;
    }
    public double Y2
    {
        get;
        set;
    }
    public string StrokeStartLineCap
    {
        get;
        internal set;
    }
    public string StrokeEndLineCap
    {
        get;
        internal set;
    }
    public string Source  //System.Windows.Media.ImageSource
    {
        get; set;
    }
    public string ImageSourceString
    {
        get; set;
    }
    public static string ImageToBase64(BitmapSource bitmap)
    {
        try
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = new MemoryStream();
            encoder.Save(stream);
            return Convert.ToBase64String(stream.ToArray());
        }
        catch (Exception ex)
        {
            // log or return empty
            return string.Empty;
        }
    }
    private static ImageSource LoadImageFromBytes(byte[] imageData)
    {
        using (var stream = new MemoryStream(imageData))
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze(); // Freeze for thread safety
            return bitmap;
        }
    }


    public static ImageSource Base64ToImage(string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        return LoadImageFromBytes(bytes);

    }
}
public static class CanvasSerializer
{
    public static TemplateRecord CreateTemplateRecord(Canvas canvas
        , List<BarcodeImageProperties> BarcodeProperties
        , List<TextFieldValue> TextFieldValues
        , List<QRCodeImageProperties> QRCodeProperties
        , List<ImageProperties> ImageProperties
        , List<RectangleFigureProperties> RectangleFigureProperties
        , List<LineProperties> LineFigureProperties
        , List<PolygonProperties> PolygonProperties
        )
    {
        var canvasElements = new TemplateRecord()
        {
            textFieldValues = TextFieldValues,
            imageProperties = ImageProperties,
            barcodeImage = BarcodeProperties,
            qRCodeImages = QRCodeProperties,
            rectangleFigures = RectangleFigureProperties,
            lineProperties = LineFigureProperties,
            polygonProperties = PolygonProperties

        };

        canvasElements.elements = canvas.Children.OfType<System.Windows.Shapes.Rectangle>().Select(shape => new CanvasElement
        {
            Name = shape.Name ?? string.Empty,
            Type = shape.GetType().Name,
            Left = Canvas.GetLeft(shape),
            Top = Canvas.GetTop(shape),
            Width = shape.Width,
            Height = shape.Height,
            Fill = JsonHelper.Serialize<Brush>(shape.Fill),
            Stroke = JsonHelper.Serialize<Brush>(shape.Stroke),
            StrokeThickness = shape.StrokeThickness,
            RadiusX = shape.RadiusX,
            RadiusY = shape.RadiusY,
            HorizontalAlignment = JsonHelper.Serialize(shape.HorizontalAlignment),
            VerticalAlignment = JsonHelper.Serialize(shape.VerticalAlignment),
            Opacity = shape.Opacity,
            Stretch = JsonHelper.Serialize(shape.Stretch),
        }).ToList();

        var lines = canvas.Children.OfType<System.Windows.Shapes.Line>().Select(shape => new CanvasElement
        {
            Name = shape.Name ?? string.Empty,
            Type = shape.GetType().Name,
            Left = Canvas.GetLeft(shape),
            Top = Canvas.GetTop(shape),
            X1 = shape.X1,
            Y1 = shape.Y1,
            X2 = shape.X2,
            Y2 = shape.Y2,
            Stroke = JsonHelper.Serialize<Brush>(shape.Stroke),
            StrokeThickness = shape.StrokeThickness,
            StrokeStartLineCap = JsonHelper.Serialize(shape.StrokeStartLineCap),
            StrokeEndLineCap = JsonHelper.Serialize(shape.StrokeEndLineCap),
        }).ToList();
        if (lines.Count() > 0)
        {
            canvasElements.elements.AddRange(lines);
        }

        var polygons = canvas.Children.OfType<System.Windows.Shapes.Polygon>().Select(shape => new CanvasElement
        {
            Name = shape.Name ?? string.Empty,
            Type = shape.GetType().Name,
            Left = Canvas.GetLeft(shape),
            Top = Canvas.GetTop(shape),
            Stroke = JsonHelper.Serialize<Brush>(shape.Stroke),
            StrokeThickness = shape.StrokeThickness,
            Points = shape.Points.ToArray(),
            Fill = JsonHelper.Serialize<Brush>(shape.Fill),
            HorizontalAlignment = JsonHelper.Serialize(shape.HorizontalAlignment),
            VerticalAlignment = JsonHelper.Serialize(shape.VerticalAlignment),
            Width = shape.Width,
            Height = shape.Height,
            Opacity = shape.Opacity,

        }).ToList();
        if (polygons.Count() > 0)
        {
            canvasElements.elements.AddRange(polygons);
        }

        var polylines = canvas.Children.OfType<System.Windows.Shapes.Polyline>().Select(shape => new CanvasElement
        {
            Name = shape.Name ?? string.Empty,
            Type = shape.GetType().Name,
            Stroke = JsonHelper.Serialize<Brush>(shape.Stroke),
            StrokeThickness = shape.StrokeThickness,
            Points = shape.Points.ToArray(),
            Fill = JsonHelper.Serialize<Brush>(shape.Fill),
            Opacity = shape.Opacity,

        }).ToList();
        if (polylines.Count() > 0)
        {
            canvasElements.elements.AddRange(polylines);
        }

        canvasElements.imageCanvasElements = canvas.Children.OfType<System.Windows.Controls.Image>().Select(image => new ImageCanvasElement
        {
            Name = image.Name ?? string.Empty,
            Type = image.GetType().Name,
            Left = Canvas.GetLeft(image),
            Top = Canvas.GetTop(image),
            Width = image.Width,
            Height = image.Height,
            HorizontalAlignment = JsonHelper.Serialize<System.Windows.HorizontalAlignment>(image.HorizontalAlignment),
            VerticalAlignment = JsonHelper.Serialize<System.Windows.VerticalAlignment>(image.VerticalAlignment),
            Opacity = image.Opacity,
            Stretch = JsonHelper.Serialize<Stretch>(image.Stretch),
            ImageSourceString = image.Source is BitmapSource bitmapSource ? ImageCanvasElement.ImageToBase64(bitmapSource) : string.Empty,
        }).ToList();

        canvasElements.textBoxCanvasElements = canvas.Children.OfType<TextBox>().Select(tb => new TextBoxCanvasElement
        {
            Name = tb.Name ?? string.Empty,
            Type = tb.GetType().Name,
            Text = tb.Text,
            Left = Canvas.GetLeft(tb),
            Top = Canvas.GetTop(tb),
            Width = tb.Width,
            Height = tb.Height,
            Background = JsonHelper.Serialize<Brush>(tb.Background),
            Foreground = JsonHelper.Serialize<Brush>(tb.Foreground),
            FontFamily = JsonHelper.Serialize(tb.FontFamily.Source),
            FontSize = tb.FontSize,
            FontStyle = JsonHelper.Serialize(tb.FontStyle),
            FontWeight = JsonHelper.Serialize(tb.FontWeight),
            FontStretch = JsonHelper.Serialize(tb.FontStretch),
            Angle = tb.RenderTransform is RotateTransform rotateTransform ? rotateTransform.Angle : 0,
            TextWrapping = JsonHelper.Serialize(tb.TextWrapping),
            AcceptsReturn = tb.AcceptsReturn
        }).ToList();
        return canvasElements;
    }
    public static string SerializeCanvas(Canvas canvas
        , List<BarcodeImageProperties> BarcodeProperties
        , List<TextFieldValue> TextFieldValues
        , List<QRCodeImageProperties> QRCodeProperties
        , List<ImageProperties> ImageProperties
        , List<RectangleFigureProperties> RectangleFigureProperties
        , List<LineProperties> LineFigureProperties
        , List<PolygonProperties> PolygonProperties
        )
    {
        var canvasElements = CreateTemplateRecord(canvas, BarcodeProperties, TextFieldValues, QRCodeProperties, ImageProperties, RectangleFigureProperties, LineFigureProperties, PolygonProperties);
        return JsonHelper.Serialize<TemplateRecord>(canvasElements);
    }

    public static void DeserializeCanvas(MainPage mainPage, ref Canvas templateCanvas, string jsonContent
        , ref List<BarcodeImageProperties> BarcodeProperties
        , ref List<TextFieldValue> TextFieldValues
        , ref List<QRCodeImageProperties> QRCodeProperties
        , ref List<ImageProperties> ImageProperties
        , ref List<RectangleFigureProperties> RectangleFigureProperties
        , ref List<LineProperties> LineFigureProperties
        , ref List<PolygonProperties> PolygonProperties
        )
    {
        var canvasElements = JsonHelper.Deserialize<TemplateRecord>(jsonContent);
        foreach (var canvasElement in canvasElements.elements)
        {
            switch (canvasElement.Type)
            {
                case "Rectangle":
                    {
                        var rectangle = new System.Windows.Shapes.Rectangle
                        {
                            Width = canvasElement.Width,
                            Height = canvasElement.Height,
                            Fill = JsonHelper.Deserialize<Brush>(canvasElement.Fill),
                            Stroke = JsonHelper.Deserialize<Brush>(canvasElement.Stroke),
                            StrokeThickness = canvasElement.StrokeThickness,
                            RadiusX = canvasElement.RadiusX,
                            RadiusY = canvasElement.RadiusY,
                            HorizontalAlignment = JsonHelper.Deserialize<System.Windows.HorizontalAlignment>(canvasElement.HorizontalAlignment),
                            VerticalAlignment = JsonHelper.Deserialize<System.Windows.VerticalAlignment>(canvasElement.VerticalAlignment),
                            Opacity = canvasElement.Opacity,
                            Stretch = JsonHelper.Deserialize<Stretch>(canvasElement.Stretch)
                        };
                        Canvas.SetLeft(rectangle, canvasElement.Left);
                        Canvas.SetTop(rectangle, canvasElement.Top);
                        templateCanvas.Children.Add(rectangle);
                        //Удалить
                        rectangle.ContextMenu = new ContextMenu();
                        rectangle.ContextMenu.FontSize = 12;
                        MenuItem menuItem = new MenuItem() { Header = "Удалить" };
                        menuItem.Click += mainPage.MenuItem_Click_Remove;
                        rectangle.ContextMenu.Items.Add(menuItem);
                        //Размеры
                        MenuItem menuItemRect = new MenuItem() { Header = "Размеры" };
                        menuItemRect.Click += mainPage.MenuItem_Click_ImageProperties;
                        rectangle.ContextMenu.Items.Add(menuItemRect);
                        //Свойства
                        MenuItem menuItemRectProperties = new MenuItem() { Header = "Свойства прямоугольника" };
                        menuItemRectProperties.Click += mainPage.MenuItem_Click_SetRectangleProperties;
                        rectangle.ContextMenu.Items.Add(menuItemRectProperties);
                    }
                    break;
                case "Line":
                    {
                        var line = new System.Windows.Shapes.Line
                        {
                            X1 = canvasElement.X1,
                            Y1 = canvasElement.Y1,
                            X2 = canvasElement.X2,
                            Y2 = canvasElement.Y2,
                            Stroke = JsonHelper.Deserialize<Brush>(canvasElement.Stroke),
                            StrokeThickness = canvasElement.StrokeThickness,
                            StrokeStartLineCap = JsonHelper.Deserialize<PenLineCap>(canvasElement.StrokeStartLineCap),
                            StrokeEndLineCap = JsonHelper.Deserialize<PenLineCap>(canvasElement.StrokeEndLineCap),
                        };
                        Canvas.SetLeft(line, canvasElement.Left);
                        Canvas.SetTop(line, canvasElement.Top);
                        templateCanvas.Children.Add(line);
                        //Удалить
                        line.ContextMenu = new ContextMenu();
                        line.ContextMenu.FontSize = 12;
                        MenuItem menuItem = new MenuItem() { Header = "Удалить" };
                        menuItem.Click += mainPage.MenuItem_Click_Remove;
                        line.ContextMenu.Items.Add(menuItem);
                        //Размеры
                        MenuItem menuItemRect = new MenuItem() { Header = "Размеры" };
                        menuItemRect.Click += mainPage.MenuItem_Click_Rectangle;
                        line.ContextMenu.Items.Add(menuItemRect);
                    }
                    break;
                case "Polygon":
                    {
                        var polygon = new System.Windows.Shapes.Polygon
                        {
                            Stroke = JsonHelper.Deserialize<Brush>(canvasElement.Stroke),
                            StrokeThickness = canvasElement.StrokeThickness,
                        };
                        Canvas.SetLeft(polygon, canvasElement.Left);
                        Canvas.SetTop(polygon, canvasElement.Top);
                        templateCanvas.Children.Add(polygon);
                        //Удалить
                        polygon.ContextMenu = new ContextMenu();
                        polygon.ContextMenu.FontSize = 12;
                        MenuItem menuItem = new MenuItem() { Header = "Удалить" };
                        menuItem.Click += mainPage.MenuItem_Click_Remove;
                        polygon.ContextMenu.Items.Add(menuItem);
                        //Размеры
                        MenuItem menuItemRect = new MenuItem() { Header = "Размеры" };
                        menuItemRect.Click += mainPage.MenuItem_Click_PolygonProperties;
                        polygon.ContextMenu.Items.Add(menuItemRect);
                    }
                    break;

                case "Polyline":
                    {
                        var polyline = new System.Windows.Shapes.Polyline
                        {
                            Stroke = JsonHelper.Deserialize<Brush>(canvasElement.Stroke),
                            StrokeThickness = canvasElement.StrokeThickness,
                        };
                        templateCanvas.Children.Add(polyline);
                        //Удалить
                        polyline.ContextMenu = new ContextMenu();
                        polyline.ContextMenu.FontSize = 12;
                        MenuItem menuItem = new MenuItem() { Header = "Удалить" };
                        menuItem.Click += mainPage.MenuItem_Click_Remove;
                        polyline.ContextMenu.Items.Add(menuItem);
                    }
                    break;
            }
        }

        foreach (var textBoxElement in canvasElements.textBoxCanvasElements)
        {
            var Name = textBoxElement.Name;
            var Text = textBoxElement.Text;
            var Width = textBoxElement.Width;
            var Height = textBoxElement.Height;
            var Background = JsonHelper.Deserialize<Brush>(textBoxElement.Background);
            var Foreground = JsonHelper.Deserialize<Brush>(textBoxElement.Foreground);
            var FontFamily = JsonHelper.Deserialize<FontFamily>(textBoxElement.FontFamily);
            var FontSize = textBoxElement.FontSize;
            var FontStyle = JsonHelper.Deserialize<System.Windows.FontStyle>(textBoxElement.FontStyle);
            var FontWeight = JsonHelper.Deserialize<System.Windows.FontWeight>(textBoxElement.FontWeight);
            var FontStretch = JsonHelper.Deserialize<System.Windows.FontStretch>(textBoxElement.FontStretch);
            var TextWrapping = JsonHelper.Deserialize<TextWrapping>(textBoxElement.TextWrapping);
            var RenderTransform = new RotateTransform(textBoxElement.Angle);


            var textBox = new TextBox
            {
                Name = textBoxElement.Name,
                Text = textBoxElement.Text,
                Width = textBoxElement.Width,
                Height = textBoxElement.Height,
                Background = JsonHelper.Deserialize<Brush>(textBoxElement.Background),
                Foreground = JsonHelper.Deserialize<Brush>(textBoxElement.Foreground),
                FontFamily = JsonHelper.Deserialize<FontFamily>(textBoxElement.FontFamily),
                FontSize = textBoxElement.FontSize,
                FontStyle = JsonHelper.Deserialize<System.Windows.FontStyle>(textBoxElement.FontStyle),
                FontWeight = JsonHelper.Deserialize<System.Windows.FontWeight>(textBoxElement.FontWeight),
                FontStretch = JsonHelper.Deserialize<System.Windows.FontStretch>(textBoxElement.FontStretch),
                TextWrapping = JsonHelper.Deserialize<TextWrapping>(textBoxElement.TextWrapping),
                RenderTransform = new RotateTransform(textBoxElement.Angle)
            };
            Canvas.SetLeft(textBox, textBoxElement.Left);
            Canvas.SetTop(textBox, textBoxElement.Top);
            //Удалить
            textBox.ContextMenu = new ContextMenu();
            textBox.ContextMenu.FontSize = 12;
            MenuItem menuItem = new MenuItem() { Header = "Удалить" };
            menuItem.Click += mainPage.MenuItem_Click_Remove;
            textBox.ContextMenu.Items.Add(menuItem);
            //Размеры
            MenuItem menuItemRect = new MenuItem() { Header = "Размеры" };
            menuItemRect.Click += mainPage.MenuItem_Click_Rectangle;
            textBox.ContextMenu.Items.Add(menuItemRect);
            //Значение
            MenuItem menuItemText = new MenuItem() { Header = "Текст" };
            menuItemText.Click += mainPage.OnNavigateButtonClick;
            textBox.ContextMenu.Items.Add(menuItemText);
            templateCanvas.Children.Add(textBox);
        }
        foreach (var imageElement in canvasElements.imageCanvasElements)
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

            Canvas.SetLeft(image, imageElement.Left);
            Canvas.SetTop(image, imageElement.Top);
            //Удалить
            image.ContextMenu = new ContextMenu();
            image.ContextMenu.FontSize = 12;
            MenuItem menuItem = new MenuItem() { Header = "Удалить" };
            menuItem.Click += mainPage.MenuItem_Click_Remove;
            image.ContextMenu.Items.Add(menuItem);
            //Размеры
            MenuItem menuItemRect = new MenuItem() { Header = "Размеры" };
            menuItemRect.Click += mainPage.MenuItem_Click_ImageProperties;
            image.ContextMenu.Items.Add(menuItemRect);
            templateCanvas.Children.Add(image);
            if (image.Name != null && image.Name.StartsWith("BarcodeImage"))
            {
                // BarcodeFunction string
                MenuItem menuItemBarcode = new MenuItem() { Header = "Символ штрих кода" };
                menuItemBarcode.Click += mainPage.MenuItem_Click_ChangeBarcode;
                image.ContextMenu.Items.Add(menuItemBarcode);
            }
        }

        TextFieldValues = canvasElements.textFieldValues;
        ImageProperties = canvasElements.imageProperties;
        BarcodeProperties = canvasElements.barcodeImage;
        QRCodeProperties = canvasElements.qRCodeImages;
        RectangleFigureProperties = canvasElements.rectangleFigures;
        LineFigureProperties = canvasElements.lineProperties;
        PolygonProperties = canvasElements.polygonProperties;
    }

}

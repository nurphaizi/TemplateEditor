using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Management;
using System.Net.Http.Json;
using System.Printing;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using System.Xml;
using Microsoft.Win32;
using Newtonsoft.Json;
using Serilog;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using Xceed.Wpf.Toolkit.Core.Converters;
using ZXing;
using ZXing.QrCode.Internal;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;
using FontStyle = System.Windows.FontStyle;
using Image = System.Windows.Controls.Image;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace TemplateEdit;
/// <summary>
/// Interaction logic for PagePrint.xaml
/// </summary>

public struct Dimension
{
    public int Width
    {
        get; set;
    }
    public int Height
    {
        get; set;
    }
    public Dimension()
    {
        Width = 0;
        Height = 0;
    }
    public Dimension(int width, int height)
    {
        Width = 0;
        Height = 0;
    }
    public override string ToString()
    {
        return $"Размер:{Width}ММ Х {Height}ММ";
    }
}
    public struct ImageableArea
{
    public int? OriginWidth
    {
        get; set;
    }
    public int? OriginHeight
    {
        get; set;
    }
    public int? ExtentWidth
    {
        get; set;
    }
    public int? ExtentHeight
    {
        get; set;
    }
    public ImageableArea()
    {
        ExtentHeight = 0;
        ExtentWidth = 0;
        OriginHeight = 0;
        OriginWidth = 0;
    }
    public ImageableArea(int extentWidth, int extentHeight, int originWidth, int originHeight)
    {
        ExtentWidth = extentWidth;
        ExtentHeight = extentHeight;
        OriginWidth = originWidth;
        OriginHeight = originHeight;
    }
    public ImageableArea(ImageableArea imageableArea)
    {
        ExtentWidth = imageableArea.ExtentWidth;
        ExtentHeight = imageableArea.ExtentHeight;
        OriginWidth = imageableArea.OriginWidth;
        OriginHeight = imageableArea.OriginHeight;
    }

}

public class PageImageableSize
{
    public string? DisplayName
    {
        get; set;
    }
    public int? ImageableSizeWidth
    {
        get; set;
    }
    public int? ImageableSizeHeight
    {
        get; set;
    }
    public ImageableArea ImageableArea
    {
        get; set;
    }
    public PageImageableSize()
    {
        DisplayName = String.Empty;
        ImageableSizeWidth = 0;
        ImageableSizeHeight = 0;
        ImageableArea = new ImageableArea();
    }
    public PageImageableSize(string displayName, int imageableSizeWidth, int imageableSizeHeight, ImageableArea imageableArea)
    {
        DisplayName = displayName;
        ImageableSizeWidth = imageableSizeWidth;
        ImageableSizeHeight = imageableSizeHeight;
        ImageableArea = new ImageableArea(imageableArea);
    }
}
public class CustomPageResolution : IEquatable<CustomPageResolution>, IComparable<CustomPageResolution>

{
    public string CustomDisplayName
    {
        get; set;
    }
    public int? X
    {
        get; set;
    }
    public int? Y
    {
        get; set;
    }
    public CustomPageResolution()
    {
        X = 0;
        Y = 0;
        CustomDisplayName = String.Empty;
    }
    // Custom name for the media size

    public CustomPageResolution(string CustomDisplayName, int? X, int? Y)
    {
        this.X = X;
        this.Y = Y;
        this.CustomDisplayName = CustomDisplayName;
    }

    public override string ToString()
    {
        return $"{CustomDisplayName}: {X}x{Y}";
    }
    public bool Equals(CustomPageResolution other)
    {
        if (other == null) return false;
        return CustomDisplayName == other.CustomDisplayName && X == other.X && Y == other.Y;
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(CustomDisplayName, X, Y);
    }
    public override bool Equals(object? obj)
    {
        return Equals(obj as CustomPageResolution);
    }
    public int CompareTo(CustomPageResolution other)
    {
        if (other == null) return 1;
        // Compare by CustomDisplayName first, then by X and Y
        int displayNameComparison = string.Compare(CustomDisplayName, other.CustomDisplayName, StringComparison.OrdinalIgnoreCase);
        if (displayNameComparison != 0) return displayNameComparison;
        int xComparison = X.GetValueOrDefault().CompareTo(other.X.GetValueOrDefault());
        if (xComparison != 0) return xComparison;
        return Y.GetValueOrDefault().CompareTo(other.Y.GetValueOrDefault());

    }

}

public class CustomPageOrientation : IEquatable<CustomPageOrientation>, IComparable<CustomPageOrientation>
{
    public PageOrientation Orientation
    {
        get; set;
    }

    public string Description
    {
        get; set;
    } // Add custom property

    public CustomPageOrientation()
    {
        Orientation = PageOrientation.Unknown;
        Description = String.Empty;
    }
    public CustomPageOrientation(PageOrientation orientation, string description)
    {
        Orientation = orientation;
        Description = description;
    }

    public override string ToString()
    {
        return $"{Orientation}: {Description}";
    }

    public bool Equals(CustomPageOrientation other)
    {
    
    if (other == null) return false;
        return Orientation == other.Orientation &&  Description == other.Description;
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(Description, Orientation);
    }
    public override bool Equals(object? obj)
    {
        return Equals(obj as CustomPageOrientation);
    }
    public int CompareTo(CustomPageOrientation other)
    {
        if (other == null) return 1;
        // Compare by Orientation first, then by Description
        int orientationComparison = Orientation.CompareTo(other.Orientation);
        if (orientationComparison != 0) return orientationComparison;
        return string.Compare(Description, other.Description, StringComparison.OrdinalIgnoreCase);
    }
}

public class CustomPageMediaSize : IEquatable<CustomPageMediaSize>, IComparable<CustomPageMediaSize>

{
    public PageMediaSizeName PageMediaSizeName
    {
        get; set;
    }
    public double? Width
    {
        get; set;
    }
    public double? Height
    {
        get; set;
    }
    public string CustomMediaSizeName
    {
        get; set;
    }
    // Custom name for the media size
    public string CustomMediaSizeDisplayName
    {
        get; set;
    }
    // Custom name for the media size
    public CustomPageMediaSize()
    {
        PageMediaSizeName = PageMediaSizeName.Unknown;
        Width = 0;
        Height = 0;
        CustomMediaSizeName = String.Empty;
        CustomMediaSizeDisplayName = String.Empty;
    }
    public CustomPageMediaSize(PageMediaSizeName mediaSizeName, double width, double height, string customName, string customMediaSizeDisplayName)
    {
        PageMediaSizeName = mediaSizeName;
        Width = width;
        Height = height;
        CustomMediaSizeName = customName;
        CustomMediaSizeDisplayName = customMediaSizeDisplayName;
    }

    public override string ToString()
    {
        return $"{CustomMediaSizeDisplayName}: {Width} x {Height}";
    }
    public bool Equals(CustomPageMediaSize other)
    {
        if (other == null) return false;
        return PageMediaSizeName == other.PageMediaSizeName
            && CustomMediaSizeName == other.CustomMediaSizeName
            && CustomMediaSizeDisplayName == other.CustomMediaSizeDisplayName
            && Width == other.Width
            && Height == other.Height;
    }
    public override bool Equals(object obj)
    {
        return Equals(obj as CustomPageMediaSize);
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(PageMediaSizeName, CustomMediaSizeName, CustomMediaSizeDisplayName,Width,Height);
    }
    public int CompareTo(CustomPageMediaSize other)
    {
        if (other == null) return 1;
        return ToString().CompareTo(other.ToString());
    }

}



public class PrintSettings

{

    public PrintSettings()
    {
        PageMediaSize = new CustomPageMediaSize();
        PageResolution = new CustomPageResolution();
        PageOrientation = new CustomPageOrientation();
        PageImageableSize = new PageImageableSize();
        Terminal = false;
        Scale = 1.0f;
    }

    public PrintSettings(string jsonString)
    {
        var printSettings = JsonHelper.Deserialize<PrintSettings>(jsonString);
        PageMediaSize = printSettings.PageMediaSize;
        PageResolution = printSettings.PageResolution;
        PageOrientation = printSettings.PageOrientation;
        PageImageableSize = printSettings.PageImageableSize;
        Terminal = printSettings.Terminal;
        Scale = printSettings.Scale;
        if (Scale <= 0) { Scale = 1.0f; }
        ;
    }
    public PrintSettings(MemoryStream ms)
    {
        var printSettings = JsonHelper.DeserializeFromStream<PrintSettings>(ms);
        PageMediaSize = printSettings.PageMediaSize;
        PageResolution = printSettings.PageResolution;
        PageOrientation = printSettings.PageOrientation;
        PageImageableSize = printSettings.PageImageableSize;
        Terminal = printSettings.Terminal;
        Scale = printSettings.Scale;
        if (Scale <= 0) { Scale = 1.0f; }
        ;
    }
    [JsonProperty("customPageMediaSize")]
    public CustomPageMediaSize PageMediaSize
    {
        get; set;
    }
    [JsonProperty("customPageResolution")]
    public CustomPageResolution PageResolution
    {
        get; set;
    }
    [JsonProperty("customPageOrientation")]
    public CustomPageOrientation PageOrientation
    {
        get; set;
    }
    [JsonProperty("PageImageableSize")]
    public PageImageableSize PageImageableSize
    {
        get; set;
    }
    [JsonProperty("terminal")]
    public bool Terminal
    {
        get; set;
    }
    public float Scale
    {
        get; set;
    }

    public Dimension Dimension
    {
        get;
        set;
    }


    internal void SaveTo(MemoryStream ms)
    {
        string jsonString = JsonHelper.Serialize<PrintSettings>(this);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);
        ms.Position = 0;
        ms.Write(jsonBytes, 0, jsonBytes.Length);
    }
}
public static class PrintQueueExtensions
{
    private static XmlNodeList? xmlNodeList;

    public static System.Collections.ObjectModel.ObservableCollection<CustomPageOrientation> GetPrintPageOrientationCapabality(this PrintQueue printQueue)
    {
        XmlNodeList xmlNodeList = null;
        // Retrieve printer capabilities as XML
        MemoryStream capabilitiesStream = printQueue.GetPrintCapabilitiesAsXml();
        // Load the capabilities XML
        XmlDocument capabilitiesXml = new XmlDocument();
        capabilitiesStream.Position = 0;
        capabilitiesXml.Load(capabilitiesStream);
        capabilitiesStream.Position = 0;
        XmlNamespaceManager nsmgr = new XmlNamespaceManager(capabilitiesXml.NameTable);
        nsmgr.AddNamespace("psf", "http://schemas.microsoft.com/windows/2003/08/printing/printschemaframework");
        nsmgr.AddNamespace("psk", "http://schemas.microsoft.com/windows/2003/08/printing/printschemakeywords");
        nsmgr.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
        nsmgr.AddNamespace("xsd", "http://www.w3.org/2001/XMLSchema");
        nsmgr.AddNamespace("seagull.driver.unique", "http://schemas.seagullscientific.com/print/seagull/driver/unique");
        nsmgr.AddNamespace("seagull.driver.base", "http://schemas.seagullscientific.com/print/seagull/driver/base");
        nsmgr.AddNamespace("seagull.driver.common", "http://schemas.seagullscientific.com/print/seagull/driver/common");
        nsmgr.AddNamespace("seagull.driver.printer.tsc", "http://schemas.seagullscientific.com/print/seagull/driver/printer/tsc");
        //[substring-after(@name, ':') = 'PageMediaSize']
        var query = """
             psf:PrintCapabilities/psf:Feature[substring-after(@name, ':') = 'PageOrientation']/psf:Option[name(namespace::*[.='http://schemas.microsoft.com/windows/2003/08/printing/printschemakeywords'])=substring-before(@name,':')]
            """;
        xmlNodeList = capabilitiesXml.SelectNodes(query, nsmgr);
        var pageOrientationCapability = new System.Collections.ObjectModel.ObservableCollection<CustomPageOrientation>();
        foreach (XmlNode node in xmlNodeList)
        {
            var parent = node.ParentNode.Attributes["name"].Value;
            if (node.Name == "psf:Option" && parent == "psk:PageOrientation")
            {
                string name = node.Attributes["name"].Value;
                string customName = name.Substring(name.IndexOf(':') + 1);
                string displayName = String.Empty;
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    if (childNode.Attributes["name"].Value == "psk:DisplayName")
                    {
                        displayName = childNode.InnerText;
                        if (childNode.InnerText == "Landscape" || childNode.InnerText == "Альбомная")
                        {
                            pageOrientationCapability.Add(new CustomPageOrientation(PageOrientation.Landscape, displayName));
                        }
                        else if (childNode.InnerText == "Portrait" || childNode.InnerText == "Книжная")
                        {
                            pageOrientationCapability.Add(new CustomPageOrientation(PageOrientation.Portrait, displayName));
                        }
                        else if (childNode.InnerText == "ReverseLandscape" || childNode.InnerText == "Альбомная реверсивная")
                        {
                            pageOrientationCapability.Add(new CustomPageOrientation(PageOrientation.ReverseLandscape, displayName));
                        }
                        else if (childNode.InnerText == "ReversePortrait" || childNode.InnerText == "Книжная реверсивная")
                        {
                            pageOrientationCapability.Add(new CustomPageOrientation(PageOrientation.ReversePortrait, displayName));
                        }
                        else if (childNode.InnerText == "Unknown" || childNode.InnerText == "Неопределена")
                        {
                            pageOrientationCapability.Add(new CustomPageOrientation(PageOrientation.ReversePortrait, displayName));
                        }
                    }
                }
            }

        }
        return pageOrientationCapability;
    }
    public static System.Collections.ObjectModel.ObservableCollection<CustomPageMediaSize> GetPageMediaSizeCapabality(this PrintQueue printQueue)
    {
        MemoryStream capabilitiesStream = printQueue.GetPrintCapabilitiesAsXml();
        // Load the capabilities XML
        XmlDocument capabilitiesXml = new XmlDocument();
        capabilitiesStream.Position = 0;
        capabilitiesXml.Load(capabilitiesStream);
        capabilitiesStream.Position = 0;
        XmlNamespaceManager nsmgr = new XmlNamespaceManager(capabilitiesXml.NameTable);
        nsmgr.AddNamespace("psf", "http://schemas.microsoft.com/windows/2003/08/printing/printschemaframework");
        nsmgr.AddNamespace("psk", "http://schemas.microsoft.com/windows/2003/08/printing/printschemakeywords");
        nsmgr.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
        nsmgr.AddNamespace("xsd", "http://www.w3.org/2001/XMLSchema");
        nsmgr.AddNamespace("seagull.driver.unique", "http://schemas.seagullscientific.com/print/seagull/driver/unique");
        nsmgr.AddNamespace("seagull.driver.base", "http://schemas.seagullscientific.com/print/seagull/driver/base");
        nsmgr.AddNamespace("seagull.driver.common", "http://schemas.seagullscientific.com/print/seagull/driver/common");
        nsmgr.AddNamespace("seagull.driver.printer.tsc", "http://schemas.seagullscientific.com/print/seagull/driver/printer/tsc");
        //[substring-after(@name, ':') = 'PageMediaSize']
        var query = """
            psf:PrintCapabilities/psf:Feature[substring-after(@name, ':') = 'PageMediaSize'][name(namespace::*[.='http://schemas.microsoft.com/windows/2003/08/printing/printschemakeywords'])=substring-before(@name,':')]/psf:Option
            |psf:PrintCapabilities/psf:Property[substring-after(@name, ':') = 'PageImageableSize'][name(namespace::*[.='http://schemas.microsoft.com/windows/2003/08/printing/printschemakeywords'])=substring-before(@name,':')]/psf:Property
            """;
        var customPageMediaSizeCapability = new System.Collections.ObjectModel.ObservableCollection<CustomPageMediaSize>();
        xmlNodeList = capabilitiesXml.SelectNodes(query, nsmgr);
        foreach (XmlNode node in xmlNodeList)
        {
            var parent = node.ParentNode.Attributes["name"].Value;
            if (node.Name == "psf:Option" && parent == "psk:PageMediaSize")
            {
                string name = node.Attributes["name"].Value;
                string customName = name.Substring(name.IndexOf(':') + 1);
                PageMediaSizeName pageMediaSizeName = new PageMediaSizeName();
                try
                {
                    if (Enum.TryParse(customName, out pageMediaSizeName))
                    {
                    }
                }
                catch (ArgumentException ex)
                {
                    pageMediaSizeName = PageMediaSizeName.Unknown;
                }
                catch (InvalidOperationException ex)
                {
                    pageMediaSizeName = PageMediaSizeName.Unknown;
                }
                double Width = 0;
                double Height = 0;
                string displayName = String.Empty;
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    if (childNode.Attributes["name"].Value == "psk:DisplayName")
                    {
                        displayName = childNode.InnerText;
                    }
                    if (childNode.Attributes["name"].Value == "psk:MediaSizeWidth")
                    {
                        if (String.IsNullOrEmpty(childNode.InnerText))
                        {
                            Width = 0;
                        }
                        else
                            Width = double.Parse(childNode.InnerText);
                    }
                    if (childNode.Attributes["name"].Value == "psk:MediaSizeHeight")
                    {
                        if (String.IsNullOrEmpty(childNode.InnerText))
                        {
                            Height = 0;
                        }
                        else
                            Height = double.Parse(childNode.InnerText);
                    }

                }
                CustomPageMediaSize customPageMediaSize = new CustomPageMediaSize(pageMediaSizeName, Width, Height, customName, displayName);
                customPageMediaSizeCapability.Add(customPageMediaSize);
            }

        }
        return customPageMediaSizeCapability;
    }
    public static System.Collections.ObjectModel.ObservableCollection<CustomPageResolution> GetPageResolutionCapabality(this PrintQueue printQueue)
    {
        XmlNodeList xmlNodeList = null;
        // Retrieve printer capabilities as XML
        MemoryStream capabilitiesStream = printQueue.GetPrintCapabilitiesAsXml();
        // Load the capabilities XML
        XmlDocument capabilitiesXml = new XmlDocument();
        capabilitiesStream.Position = 0;
        capabilitiesXml.Load(capabilitiesStream);
        capabilitiesStream.Position = 0;
        XmlNamespaceManager nsmgr = new XmlNamespaceManager(capabilitiesXml.NameTable);
        nsmgr.AddNamespace("psf", "http://schemas.microsoft.com/windows/2003/08/printing/printschemaframework");
        nsmgr.AddNamespace("psk", "http://schemas.microsoft.com/windows/2003/08/printing/printschemakeywords");
        nsmgr.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
        nsmgr.AddNamespace("xsd", "http://www.w3.org/2001/XMLSchema");
        nsmgr.AddNamespace("seagull.driver.unique", "http://schemas.seagullscientific.com/print/seagull/driver/unique");
        nsmgr.AddNamespace("seagull.driver.base", "http://schemas.seagullscientific.com/print/seagull/driver/base");
        nsmgr.AddNamespace("seagull.driver.common", "http://schemas.seagullscientific.com/print/seagull/driver/common");
        nsmgr.AddNamespace("seagull.driver.printer.tsc", "http://schemas.seagullscientific.com/print/seagull/driver/printer/tsc");
        //[substring-after(@name, ':') = 'PageMediaSize']
        var query = """
            psf:PrintCapabilities/psf:Feature[substring-after(@name, ':') = 'PageResolution'][name(namespace::*[.='http://schemas.microsoft.com/windows/2003/08/printing/printschemakeywords'])=substring-before(@name,':')]/psf:Option
            """;
        var customPageResolutionCapability = new System.Collections.ObjectModel.ObservableCollection<CustomPageResolution>();
        xmlNodeList = capabilitiesXml.SelectNodes(query, nsmgr);
        foreach (XmlNode node in xmlNodeList)
        {
            var parent = node.ParentNode.Attributes["name"].Value;
            if (node.Name == "psf:Option" && parent == "psk:PageResolution")
            {
                string name = node.Attributes["name"].Value;
                string customName = name.Substring(name.IndexOf(':') + 1);
                int ResolutionX = 0;
                int ResolutionY = 0;
                string displayName = String.Empty;
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    if (childNode.Attributes["name"].Value == "psk:DisplayName")
                    {
                        displayName = childNode.InnerText;
                    }
                    if (childNode.Attributes["name"].Value == "psk:ResolutionX")
                    {
                        if (String.IsNullOrEmpty(childNode.InnerText))
                        {
                            ResolutionX = 0;
                        }
                        else
                            ResolutionX = int.Parse(childNode.InnerText);
                    }
                    if (childNode.Attributes["name"].Value == "psk:ResolutionY")
                    {
                        if (String.IsNullOrEmpty(childNode.InnerText))
                        {
                            ResolutionY = 0;
                        }
                        else
                            ResolutionY = int.Parse(childNode.InnerText);
                    }

                }
                CustomPageResolution pageResolution = new CustomPageResolution(displayName, ResolutionX, ResolutionY);
                customPageResolutionCapability.Add(pageResolution);
            }

        }
        return customPageResolutionCapability;
    }
    //PageImageableSize
    public static PageImageableSize GetPageImageableSizeCapabality(this PrintQueue printQueue)
    {
        var pageImageableSize = new PageImageableSize();
        MemoryStream capabilitiesStream = printQueue.GetPrintCapabilitiesAsXml();
        // Load the capabilities XML
        XmlDocument capabilitiesXml = new XmlDocument();
        capabilitiesStream.Position = 0;
        capabilitiesXml.Load(capabilitiesStream);
        capabilitiesStream.Position = 0;
        XmlNamespaceManager nsmgr = new XmlNamespaceManager(capabilitiesXml.NameTable);
        nsmgr.AddNamespace("psf", "http://schemas.microsoft.com/windows/2003/08/printing/printschemaframework");
        nsmgr.AddNamespace("psk", "http://schemas.microsoft.com/windows/2003/08/printing/printschemakeywords");
        nsmgr.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
        nsmgr.AddNamespace("xsd", "http://www.w3.org/2001/XMLSchema");
        nsmgr.AddNamespace("seagull.driver.unique", "http://schemas.seagullscientific.com/print/seagull/driver/unique");
        nsmgr.AddNamespace("seagull.driver.base", "http://schemas.seagullscientific.com/print/seagull/driver/base");
        nsmgr.AddNamespace("seagull.driver.common", "http://schemas.seagullscientific.com/print/seagull/driver/common");
        nsmgr.AddNamespace("seagull.driver.printer.tsc", "http://schemas.seagullscientific.com/print/seagull/driver/printer/tsc");
        //[substring-after(@name, ':') = 'PageMediaSize']
        var query = """
            psf:PrintCapabilities/psf:Property[substring-after(@name, ':') = 'PageImageableSize'][name(namespace::*[.='http://schemas.microsoft.com/windows/2003/08/printing/printschemakeywords'])=substring-before(@name,':')]/psf:Property
            """;
        var customPageMediaSizeCapability = new System.Collections.ObjectModel.ObservableCollection<CustomPageMediaSize>();
        xmlNodeList = capabilitiesXml.SelectNodes(query, nsmgr);
        string displayName = String.Empty;
        int Width = 0;
        int Height = 0;
        int ExtentWidth = 0;
        int ExtentHeight = 0;
        int OriginWidth = 0;
        int OriginHeight = 0;
        foreach (XmlNode node in xmlNodeList)
        {
            var parent = node.ParentNode.Attributes["name"].Value;
            if (node.Name == "psf:Property" && parent == "psk:PageImageableSize")
            {
                string name = node.Attributes["name"].Value;
                string attrName = name.Substring(name.IndexOf(':') + 1);
                if (attrName == "DisplayName")
                {
                    displayName = node.InnerText;
                }
                if (node.Attributes["name"].Value == "psk:ImageableSizeWidth")
                {
                    if (String.IsNullOrEmpty(node.InnerText))
                    {
                        Width = 0;
                    }
                    else
                        Width = int.Parse(node.InnerText);
                }
                if (node.Attributes["name"].Value == "psk:ImageableSizeHeight")
                {
                    if (String.IsNullOrEmpty(node.InnerText))
                    {
                        Height = 0;
                    }
                    else
                        Height = int.Parse(node.InnerText);
                }
                if (node.Attributes["name"].Value == "psk:ImageableArea")
                {
                    foreach (XmlNode childNode in node.ChildNodes)
                    {
                        if (childNode.Attributes["name"].Value == "psk:OriginWidth")
                        {
                            if (String.IsNullOrEmpty(childNode.InnerText))
                            {
                                OriginWidth = 0;
                            }
                            else
                                OriginWidth = int.Parse(childNode.InnerText);
                        }
                        if (childNode.Attributes["name"].Value == "psk:OriginHeight")
                        {
                            if (String.IsNullOrEmpty(childNode.InnerText))
                            {
                                OriginHeight = 0;
                            }
                            else
                                OriginHeight = int.Parse(childNode.InnerText);
                        }
                        if (childNode.Attributes["name"].Value == "psk:ExtentWidth")
                        {
                            if (String.IsNullOrEmpty(childNode.InnerText))
                            {
                                ExtentWidth = 0;
                            }
                            else
                                ExtentWidth = int.Parse(childNode.InnerText);
                        }
                        if (childNode.Attributes["name"].Value == "psk:ExtentHeight")
                        {
                            if (String.IsNullOrEmpty(childNode.InnerText))
                            {
                                ExtentHeight = 0;
                            }
                            else
                                ExtentHeight = int.Parse(childNode.InnerText);
                        }
                    }
                }
            }
        }
        pageImageableSize.DisplayName = displayName;
        pageImageableSize.ImageableArea = new ImageableArea(ExtentWidth, ExtentHeight, OriginWidth, OriginHeight);
        pageImageableSize.ImageableSizeWidth = Width;
        pageImageableSize.ImageableSizeHeight = Height;
        return pageImageableSize;
    }
}

public class SQLiteHelper
{
    private string connectionString;
    private SQLiteConnection connection;

    public SQLiteHelper(string connectionString)
    {
        // Initialize connection string (change to your SQLite database file path)
        this.connectionString = connectionString;
    }

    public void ExecuteQuery(string query)
    {
        try
        {
            // Establish a connection to the SQLite database
            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                // Create an SQLiteCommand object to execute the query
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    // Execute the query (non-query, i.e. no result returned)
                    int affectedRows = cmd.ExecuteNonQuery();
                    Log.Information($"Query executed successfully. {affectedRows} rows affected.");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error executing query: {ex.Message}");
        }
    }

    // Optionally, method to fetch data from the query
    public DataTable ExecuteQueryWithResults(string query)
    {
        try
        {
            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, conn))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    return dt;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing query: {ex.Message}");
            return null;
        }
    }
    public async Task<DataTable> ExecuteQueryWithResultsAsync(string query)
    {
        try
        {
            connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();
                await using var cmd = connection.CreateCommand();
                cmd.CommandText = query;
                await using var reader = await cmd.ExecuteReaderAsync();
                return await Task.Run(() =>
                {
                    var table = new DataTable();
                    table.Load(reader);
                    return table;
                }); 
        }
        catch (Exception ex)
        {
            Log.Error($"Error executing query {query}: {ex.Message}");
            return null;
        }
    }
    public async Task<DbDataReader> ExecuteQueryWithResultDataReaderAsync(string query)
    {
        try
        {
            connection = new SQLiteConnection(connectionString);
            {
                await connection.OpenAsync();
                await using var cmd = connection.CreateCommand();
                cmd.CommandText = query;
                var reader = await cmd.ExecuteReaderAsync();
                return  reader;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error executing query {query}: {ex.Message}");
            return null;
        }
    }

}

public class StaPrintWorker : IDisposable
{
    private readonly Thread _thread;
    private Dispatcher _dispatcher;
    private readonly TaskCompletionSource<bool> _ready = new();

    public StaPrintWorker()
    {
        _thread = new Thread(() =>
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _ready.SetResult(true);
            Dispatcher.Run();
        });

        _thread.SetApartmentState(ApartmentState.STA);
        _thread.IsBackground = true;
        _thread.Start();
    }

    public async Task InvokeAsync(Func<Task> action)
    {
        await _ready.Task;

        await _dispatcher.InvokeAsync(async () =>
        {
            await action();
        });
    }

    public void Dispose()
    {
        _dispatcher?.InvokeShutdown();
    }
}
public partial class PagePrint : Page, INotifyPropertyChanged
{
    private string _ConnectionString;
    public string ConnectionString
    {
        get => _ConnectionString;
        set
        {
            _ConnectionString = value;
            NotifyPropertyChanged(nameof(ConnectionString));
        }
    }
    public NavigationWindow Nv
    {
        get; set;
    }
    public static PrintCapabilities printCapabilities;

    public static PrintQueue? GetPrintQueue(string printQueueName)
    {
        LocalPrintServer printServer;
        try
        {
            printServer = new LocalPrintServer(PrintSystemDesiredAccess.AdministrateServer);
        }
        catch (PrintServerException printSrvEx)
        {
            Log.Error(printSrvEx.Message);
            return null;
        }
        PrintQueueCollection printQueues = printServer.GetPrintQueues();
        // var printQueueName = "Microsoft Print to PDF";
        // var printQueueName = "EPSON Stylus Photo R270 Series";
        // var printQueueName = "Microsoft XPS Document Writer";
        try
        {
            PrintQueue printQueue = printQueues.First(printerqueue => printerqueue.Name == printQueueName);
            return printQueue;
        }
        catch (ArgumentNullException ex)
        {
            Log.Error($"PrintQueue:{ex.Message}");
            return null;
        }
        catch (InvalidOperationException ex)
        {
            Log.Error($"PrintQueue:{ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Log.Error($"PrintQueue:{ex.Message}");
            return null;
        }
    }
    public static PrintTicket? GetPrintTicket(PrintQueue printQueue)
    {
        try
        {
            PrintTicket printTicket = printQueue.UserPrintTicket;
            printCapabilities = printQueue.GetPrintCapabilities(printTicket);

            //get scale of the print wrt to screen of WPF visual
            var OriginHeight = printCapabilities.PageImageableArea.OriginHeight;
            var OriginWidth = printCapabilities.PageImageableArea.OriginWidth;
            var ExtentWidth = printCapabilities.PageImageableArea.ExtentWidth;
            var ExtentHeight = printCapabilities.PageImageableArea.ExtentHeight;
            Log.Information($"(OriginWidth,OriginHeight)=({OriginWidth},{OriginHeight})");
            Log.Information($"Extent width {ExtentWidth}");
            Log.Information($"Extent height {ExtentHeight}");

            //PrintCapabilities printCapabilites = printQueue.GetPrintCapabilities();
            // Get a default print ticket from printer.
            //                PrintTicket printTicket = printQueue.DefaultPrintTicket;
            // Modify the print ticket.
            //printCapabilites
            //if (printCapabilites.CollationCapability.Contains(Collation.Collated))
            //    printTicket.Collation = Collation.Collated;
            //if (printCapabilites.DuplexingCapability.Contains(Duplexing.TwoSidedLongEdge))
            //    printTicket.Duplexing = Duplexing.TwoSidedLongEdge;
            //if (printCapabilites.StaplingCapability.Contains(Stapling.StapleDualLeft))
            //    printTicket.Stapling = Stapling.StapleDualLeft;

            // Returns a print ticket, which is a set of instructions telling a printer how
            // to set its various features, such as duplexing, collating, and stapling.
            if (printTicket != null)
            {
                Log.Information($"Настройки пользователя принтера {printQueue.FullName}-{printQueue.Name} ");
                Log.Information($"Ориентация {printTicket.PageOrientation} Размер листа {printTicket.PageMediaSize}");
                if (printTicket.PageBorderless.HasValue) Log.Information($"Граница листа {printTicket.PageBorderless.Value}");
                if (printTicket.PageScalingFactor.HasValue) Log.Information($"Масштабирование {printTicket.PageScalingFactor.Value}");
                return printTicket;
            }
            Log.Information($"Настройки пользователя принтера не определены ");
            return null;
        }
        catch (PrintQueueException ex)
        {
            Log.Error($"PrintqueueException {ex.Message} ");
            return null;
        }
        catch (Exception ex)
        {
            Log.Error($"GetPrintTicket: Exception {ex.Message} ");
            return null;
        }

    }
    private string _jsonReportTemplate = string.Empty;
    public string JsonReportTemplate
    {
        get
        {
            return _jsonReportTemplate;
        }
        set
        {
            _jsonReportTemplate = value;
        }
    }


    private List<TextFieldValue> _textFieldValuesList = new();
    public List<TextFieldValue> TextFieldValuesList
    {
        get => _textFieldValuesList;
        set
        {
            _textFieldValuesList = value;
            NotifyPropertyChanged(nameof(TextFieldValuesList));
        }
    }


    private Mutex mutex
    {
        get; set;
    }
    private ObservableCollection<KeyValueItem> _availableSources = new();
    public ObservableCollection<KeyValueItem> AvailableSources
    {
        get => _availableSources;
        set
        {
            _availableSources = value;
            NotifyPropertyChanged(nameof(AvailableSources));
        }
    }
    public static void EnableWriteAheadLogging(SQLiteConnection connection)
    {
        if (TASWALMode(connection)) return;
        var walCommand = connection.CreateCommand();
        walCommand.CommandText = @"PRAGMA journal_mode = 'wal'";
        walCommand.ExecuteNonQuery();

    }
    public static bool TASWALMode(SQLiteConnection connection)
    {
        try
        {
            using (SQLiteCommand command = new SQLiteCommand("PRAGMA journal_mode;", connection))
            {
                string journalMode = command.ExecuteScalar().ToString();
                if (string.IsNullOrEmpty(journalMode))
                {
                    return false;
                }
                if (string.IsNullOrWhiteSpace(journalMode))
                {
                    return false;
                }
                if (journalMode == "wal")
                {
                    return true;
                }
                return false;
            }
            return true;
        }
        catch (Exception ex) { Log.Error($"TASWALMode: {ex.Message}"); }
        return false;
    }

    public string SetConnectionToReadOnly(string connection, string key, string value)
    {
        SQLiteConnectionStringBuilder builder = new SQLiteConnectionStringBuilder(ConnectionString);
        if (!builder.ContainsKey(key))
            builder.Add(key, value);
        var connectionString = builder.ConnectionString;
        return connectionString;
    }

    public void DbClient()
    {
        var dbFileName = Properties.Settings.Default.dbFileName;
        string connectionString = $"Data Source={dbFileName}";
        this.ConnectionString = $"{connectionString};Cache=Shared;";
    }


    public PagePrint()
    {
        InitializeComponent();
    }
    public PagePrint(NavigationWindow navigationWindow, string jsonReportTemplate)
    {
        InitializeComponent();
        this.Nv = navigationWindow;
        mutex = new();
        DbClient();
        _availableSources.Clear();
        _availableSources = JsonHelper.Deserialize<ObservableCollection<KeyValueItem>>(Properties.Settings.Default.DataSources);
        NotifyPropertyChanged(nameof(AvailableSources));
    }

    public event PropertyChangedEventHandler PropertyChanged;
    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        if (PropertyChanged != null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public static PrintTicket CreateDeltaPrintTicket(PrintSettings printSettings)
    {
        // Create a new PrintTicket
        PrintTicket deltaPrintTicket = new PrintTicket();
        var scale = 96 / 25.4 / 1000;
        // Set the desired media size
        deltaPrintTicket.PageMediaSize = new PageMediaSize(printSettings.PageMediaSize.PageMediaSizeName, printSettings.PageMediaSize.Width.Value / 25.4 * 96 / 1000, printSettings.PageMediaSize.Height.Value / 25.4 * 96 / 1000);
        // Set the desired page orientation
        deltaPrintTicket.PageOrientation = printSettings.PageOrientation.Orientation;
        return deltaPrintTicket;
    }
    public static void PrintXpsDocument(string xpsFilePath, PrintQueue printQueue, PrintTicket printTicket)
    {
        // Create an XpsDocumentWriter object for the print queue.
        XpsDocumentWriter xpsDocumentWriter = PrintQueue.CreateXpsDocumentWriter(printQueue);

        // Open the selected document.
        XpsDocument xpsDocument = new XpsDocument(xpsFilePath, FileAccess.Read);

        // Get a fixed document sequence for the selected document.
        System.Windows.Documents.FixedDocumentSequence fixedDocSeq = xpsDocument.GetFixedDocumentSequence();

        // Synchronously, add the XPS document together with a print ticket to the print queue.
        xpsDocumentWriter.Write(fixedDocSeq, printTicket);

    }
    public static string GetBarcodeFormat(BarcodeFormat format)

    {
        switch (format)
        {
            case BarcodeFormat.QR_CODE:
                return "QRCODE";
            case BarcodeFormat.CODE_128:
                return "128";
            case BarcodeFormat.CODE_93:
                return "93";
            case BarcodeFormat.CODE_39:
                return "39";
            case BarcodeFormat.EAN_8:
                return "EAN8";
            case BarcodeFormat.EAN_13:
                return "EAN13";
            case BarcodeFormat.UPC_A:
                return "UPCA";
            case BarcodeFormat.UPC_E:
                return "UPCE";
            case BarcodeFormat.ITF:
                return "ITF";
            case BarcodeFormat.CODABAR:
                return "CODABAR";
            case BarcodeFormat.PDF_417:
                return "PDF417";
            case BarcodeFormat.DATA_MATRIX:
                return "DMATRIX";
            case BarcodeFormat.MAXICODE:
                return "MAXICODE";
            case BarcodeFormat.AZTEC:
                return "AZTEC";
            case BarcodeFormat.RSS_14:
                return "RSS_14";
            default:
                return "UNKNOWN";
        }
        return "TSPL";
    }
    public static void SaveBitmap(SKBitmap bitmap, string filePath)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100); // 100 = max quality
        using var stream = File.OpenWrite(filePath);
        data.SaveTo(stream);
    }
    public static SKColor BrushToSKColor(System.Windows.Media.Brush brush)
    {
        if (brush is SolidColorBrush solidBrush)
        {
            var mediaColor = solidBrush.Color;
            return new SKColor(mediaColor.R, mediaColor.G, mediaColor.B, mediaColor.A);
        }

        // Fallback for unsupported brush types
        return SKColors.Transparent;
    }
    public SKBitmap RenderTextBoxToSKBitmap(TextBox textBox)
    {
        var pixelsPerDip = VisualTreeHelper.GetDpi(textBox).PixelsPerDip;
        // Create a DrawingVisual to draw on
        var formattedText = new FormattedText(
                    textBox.Text,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(textBox.FontFamily, textBox.FontStyle, textBox.FontWeight, textBox.FontStretch),
                    textBox.FontSize,
                    textBox.Foreground, pixelsPerDip);
        var len = textBox.Text.Length;
        var origin = new Point(0, 0);
        var Width = formattedText.WidthIncludingTrailingWhitespace;
        var Height = formattedText.Height;
        var bitmap = new SKBitmap((int)Width, (int)Height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.LightGray);
        SKFont font = new SKFont();
        font.Typeface = SKTypeface.FromFamilyName(textBox.FontFamily.Source);
        font.Size = (float)textBox.FontSize;
        var skColor = BrushToSKColor(textBox.Foreground);
        canvas.DrawText(textBox.Text, 0, 0, font, new SKPaint { Color = skColor });

        return bitmap;
    }
    public SKBitmap RenderTextBoxToBitmap(TextBox textBox)
    {
        var pixelsPerDip = VisualTreeHelper.GetDpi(textBox).PixelsPerDip;
        // Create a DrawingVisual to draw on
        var formattedText = new FormattedText(
                    textBox.Text,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(textBox.FontFamily, textBox.FontStyle, textBox.FontWeight, textBox.FontStretch),
                    textBox.FontSize,
                    textBox.Foreground, pixelsPerDip);
        var len = textBox.Text.Length;
        var origin = new Point(0, 0);
        var Width = formattedText.WidthIncludingTrailingWhitespace;
        var Height = formattedText.Height;

        Rect transformedRect;
        DrawingVisual visual = new DrawingVisual();
        using (DrawingContext dc = visual.RenderOpen())
        {
            if (textBox.RenderTransform != null)
            {
                var angle = (textBox.RenderTransform as RotateTransform).Angle;
                var radians = angle * Math.PI / 180.0;
                var cos = Math.Cos(radians);
                var sin = Math.Sin(radians);
                var transformGroup = new TransformGroup();
                var rotationOrigin = new Point(Width, 0);
                var translate = new TranslateTransform(rotationOrigin.X, rotationOrigin.Y);
                transformGroup.Children.Add(translate);
                var rotate = new RotateTransform(angle, rotationOrigin.X, rotationOrigin.Y);
                transformGroup.Children.Add(rotate);
                var translateBack = new TranslateTransform(-rotationOrigin.X + Height * sin, rotationOrigin.Y);
                transformGroup.Children.Add(translateBack);
                dc.PushTransform(transformGroup);
                var originalRect = new Rect(0, 0, Width, Height);
                transformedRect = transformGroup.TransformBounds(originalRect);
                dc.DrawText(formattedText, origin);
            }
            else
            {
                dc.DrawText(formattedText, origin);
            }
            if (textBox.RenderTransform != null)
            {
                dc.Pop();
            }
        }

        var bounds = visual.ContentBounds;
        DrawingVisual visualFinal = new DrawingVisual();
        using (DrawingContext dc = visualFinal.RenderOpen())
        {
            dc.DrawRectangle(
            textBox.Background, null,
                new Rect(0, 0, bounds.Width, bounds.Height));
            dc.DrawDrawing(visual.Drawing);
        }
        bounds = visualFinal.ContentBounds;
        var renderBitmap = new RenderTargetBitmap(
           (int)bounds.Width, (int)(bounds.Height),
            96, 96, PixelFormats.Pbgra32);
        renderBitmap.Render(visualFinal);
        var bitmap = renderBitmap.ToSKBitmap();
        return bitmap;
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
          Nv.Close();

    }

    private void PrintButtonState(bool enabled)
    {
        printReport.IsEnabled = enabled;
    }
    private async void Button_Click_2(object sender, RoutedEventArgs e)

    {
        try
        {
            PrintButtonState(false);
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.json)|*.json|All files (*.*)|*.*"; // Filter file types

            if (openFileDialog.ShowDialog() == true)
            {

                var jsonContent = File.ReadAllText(openFileDialog.FileName);
                var availableSources = JsonHelper.Serialize<ObservableCollection<KeyValueItem>>(AvailableSources);
                var printer = Properties.Settings.Default.barcodePrinter;
                if (!string.IsNullOrWhiteSpace(printer))
                {
                    var printQueue = GetPrintQueue(printer);
                    var ticket = GetPrintTicket(printQueue);
                    PrintSettings printSettings = null;
                    if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.BarcodePrintSettings))
                    {
                        using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(Properties.Settings.Default.BarcodePrintSettings)))
                        {
                            printSettings = new PrintSettings(ms);

                            var barcodePrintTicket = CreateDeltaPrintTicket(printSettings);
                            if (barcodePrintTicket.PageMediaSize != null)
                            {
                                var result = printQueue.MergeAndValidatePrintTicket(ticket, barcodePrintTicket);
                                ticket = result.ValidatedPrintTicket;
                            }
                            else
                            {
                                ticket.PageMediaSize = new PageMediaSize(printCapabilities.PageImageableArea.ExtentWidth, printCapabilities.PageImageableArea.ExtentHeight);
                            }

                        }
                    }
                    CancellationToken token = new CancellationTokenSource().Token;
                    await DynamicSqliteXpsPipelineFactory.GenerateAsync(
                        ConnectionString,
                        availableSources,
                        jsonContent,
                        printerName: printer,
                        ticket, printSettings,token);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in Button_Click_2");
            MessageBox.Show("An error occurred while processing the file. Please check the log for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            PrintButtonState(true);
        }
    }
}

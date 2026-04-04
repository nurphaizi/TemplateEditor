using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Xml;

namespace TemplateEdit;
public class SerializeCanvasToXAML
{
    public static string SerializeCanvasToXaml(Canvas canvas)
    {
        if (canvas == null)
            throw new ArgumentNullException(nameof(canvas));

        var stringBuilder = new StringBuilder();

        var settings = new XmlWriterSettings
        {
            Indent = true,
            OmitXmlDeclaration = true
        };

        using (var writer = XmlWriter.Create(stringBuilder, settings))
        {
            XamlWriter.Save(canvas, writer);
        }

        return stringBuilder.ToString();
    }
    public void SaveCanvasToXaml(Canvas canvas, string filePath)
    {
        var settings = new XmlWriterSettings()
        {
            Indent = true,
            OmitXmlDeclaration = true,
            DoNotEscapeUriAttributes = true
            
        };

        using (var writer = XmlWriter.Create(filePath, settings))
        {
            XamlWriter.Save(canvas, writer);
        }
    }

    public static Canvas DeserializeCanvasFromXaml(string xaml)
    {
        if (string.IsNullOrWhiteSpace(xaml))
            throw new ArgumentException("XAML content is empty.", nameof(xaml));

        using (var stringReader = new StringReader(xaml))
        using (var xmlReader = XmlReader.Create(stringReader))
        {
            return (Canvas)XamlReader.Load(xmlReader);
        }
    }
}

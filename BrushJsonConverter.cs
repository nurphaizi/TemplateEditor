using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Windows.Media;

namespace TemplateEdit;
public class BrushJsonConverter : JsonConverter<Brush>
{
    public override void WriteJson(JsonWriter writer, Brush value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        // SolidColorBrush → "#RRGGBBAA"
        if (value is SolidColorBrush solid)
        {
            string color = solid.Color.ToString(CultureInfo.InvariantCulture);
            writer.WriteValue(color);
            return;
        }

        // Fallback object format
        JObject obj = new JObject
        {
            ["type"] = value.GetType().Name
        };

        writer.WriteToken(obj.CreateReader());
    }

    public override Brush ReadJson(
        JsonReader reader,
        Type objectType,
        Brush existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        // "#RRGGBBAA"
        if (reader.TokenType == JsonToken.String)
        {
            var color = (Color)ColorConverter.ConvertFromString((string)reader.Value);
            return new SolidColorBrush(color);
        }

        // Object format
        JObject obj = JObject.Load(reader);

        if (obj["type"]?.ToString() == nameof(SolidColorBrush) &&
            obj["color"] != null)
        {
            var color = (Color)ColorConverter.ConvertFromString(obj["color"].ToString());
            return new SolidColorBrush(color);
        }

        throw new JsonSerializationException("Unsupported Brush format");
    }

    // Tolerant parser for brush input saved in different formats.
    // Accepts:
    // - raw color strings: "#RRGGBB", "#AARRGGBB", named colors
    // - quoted JSON string: "\"#FF0000FF\""
    // - object JSON: {"type":"SolidColorBrush","color":"#..."}
    public static Brush Parse(string maybeJsonOrRaw)
    {
        if (string.IsNullOrWhiteSpace(maybeJsonOrRaw))
            return null;

        var s = maybeJsonOrRaw.Trim();

        // Unwrap surrounding JSON string quotes if present
        if (s.Length >= 2 && s[0] == '"' && s[^1] == '"')
        {
            s = s.Substring(1, s.Length - 2).Trim();
        }

        // If looks like an object, try parse as JSON object first
        if (s.StartsWith("{"))
        {
            try
            {
                var obj = JObject.Parse(s);
                if (obj["type"]?.ToString() == nameof(SolidColorBrush) && obj["color"] != null)
                {
                    var color = (Color)ColorConverter.ConvertFromString(obj["color"].ToString());
                    return new SolidColorBrush(color);
                }
            }
            catch
            {
                // Fall through to color parsing
            }
        }

        // Try to parse as a color string (handles "#...", named colors)
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(s);
            return new SolidColorBrush(color);
        }
        catch
        {
            return null;
        }
    }
}

using System.CodeDom;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using ZXing.PDF417.Internal;
public static class FieldTypeConverter
{
    // The primary lookup dictionary. This is fast and efficient.
    private static readonly Dictionary<Type, FieldTypes> TypeMap = new Dictionary<Type, FieldTypes>
    {
        { typeof(bool), FieldTypes.Boolean },
        { typeof(byte), FieldTypes.Byte },
        { typeof(sbyte), FieldTypes.SByte },
        { typeof(short), FieldTypes.Short },
        { typeof(int), FieldTypes.Int },
        { typeof(long), FieldTypes.Long },
        { typeof(float), FieldTypes.Float },
        { typeof(double), FieldTypes.Double },
        { typeof(decimal), FieldTypes.Decimal },
        { typeof(string), FieldTypes.String },
        { typeof(char), FieldTypes.Char },
        { typeof(Guid), FieldTypes.Guid },
        { typeof(DateTime), FieldTypes.DateTime },
        { typeof(DateTimeOffset), FieldTypes.DateTimeOffset },
        { typeof(TimeSpan), FieldTypes.TimeSpan },
        { typeof(byte[]), FieldTypes.ByteArray },
        
        // We'll map System.Uri to FieldTypes.URL
        { typeof(Uri), FieldTypes.URL }
    };
    private static readonly Dictionary<FieldTypes,Type> FieldTypeMap = new Dictionary<FieldTypes, Type>
    {
        { FieldTypes.Boolean,typeof(bool)},
        { FieldTypes.Byte,typeof(byte)},
        { FieldTypes.SByte,typeof(sbyte)},
        { FieldTypes.Short,typeof(short)},
        { FieldTypes.Int,typeof(int)},
        { FieldTypes.Long,typeof(long) },
        { FieldTypes.Float,typeof(float) },
        { FieldTypes.Double,typeof(double)},
        { FieldTypes.Decimal,typeof(decimal)},
        { FieldTypes.String,typeof(string)},
        { FieldTypes.Char,typeof(char)},
        { FieldTypes.Guid,typeof(Guid)},
        { FieldTypes.DateTime,typeof(DateTime)},
        { FieldTypes.DateTimeOffset,typeof(DateTimeOffset)},
        { FieldTypes.TimeSpan,typeof(TimeSpan)},
        { FieldTypes.ByteArray,typeof(byte[])},
        { FieldTypes.URL,typeof(Uri)}
    };

    /// <summary>
    /// Gets the custom FieldTypes enum from a .NET Type.
    /// </summary>
    /// <param name="netType">The .NET Type to convert (e.g., typeof(int)).</param>
    /// <returns>The corresponding FieldTypes enum value.</returns>
    public static FieldTypes GetFieldType(Type netType)
    {
        if (netType == null)
        {
            return FieldTypes.None;
        }

        // Handle Nullable<T> types (e.g., int?, bool?)
        // We get the underlying type (e.g., int) and use that for the lookup.
        Type actualType = Nullable.GetUnderlyingType(netType) ?? netType;

        // 1. Try the fast lookup in our dictionary
        if (TypeMap.TryGetValue(actualType, out FieldTypes fieldType))
        {
            return fieldType;
        }

        // 2. Handle special cases not in the map, like base classes

        // Check if the type is a Stream (e.g., MemoryStream, FileStream)
        if (typeof(Stream).IsAssignableFrom(actualType))
        {
            return FieldTypes.ByteStream;
        }

        // 3. If no match is found, return None
        return FieldTypes.None;
    }

    public static System.Type GetNetType(FieldTypes fieldType)
    {
     return FieldTypeMap.TryGetValue(fieldType,out var type) ? type : null;
    }
}

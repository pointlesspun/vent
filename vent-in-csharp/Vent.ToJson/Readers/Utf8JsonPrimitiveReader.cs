using System.Text;
using System.Text.Json;
using Vent.Util;

namespace Vent.ToJson.Readers
{
    public static class Utf8JsonPrimitiveReader
    {
        public static T ReadPrimitiveFromJson<T>(string jsonText)
        {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(jsonText));

            if (reader.TokenType == JsonTokenType.None)
            {
                reader.Read();
            }

            if (reader.TokenType == JsonTokenType.Null)
            {
                if (typeof(T).IsNullableType())
                {
                    return default;
                }

                throw new JsonException($"Encounted a json null token but the corresponding type {typeof(T)} is not nullable.");
            }

            return (T) ReadPrimitive(ref reader, typeof(T));
        }
    
        public static object ReadPrimitive(this ref Utf8JsonReader reader, Type type)
        {
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Byte => reader.GetByte(),
                TypeCode.Int16 => reader.GetInt16(),
                TypeCode.Int32 => reader.GetInt32(),
                TypeCode.Int64 => reader.GetInt64(),
                TypeCode.UInt16 => reader.GetUInt16(),
                TypeCode.UInt32 => reader.GetUInt32(),
                TypeCode.UInt64 => reader.GetUInt64(),
                TypeCode.Double => reader.GetDouble(),
                TypeCode.Single => reader.GetSingle(),
                TypeCode.String => reader.GetString(),
                TypeCode.Char => reader.GetString()[0],
                TypeCode.Boolean => reader.GetBoolean(),
                _ => throw new NotImplementedException($"type {type.Name} has not backing code "),
            };
        }
    }
}

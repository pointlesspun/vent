using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace Vent.ToJson
{
    public static class JsonUtil
    {
        public static void WritePropertyValue(this JsonWriter writer, string propertyName, object value)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteValue(value);
        }

        public static void WriteObjectValues(this JsonWriter writer, string objectName, object obj, params string[] propertyNames)
        {
            var type = obj.GetType();

            writer.WritePropertyName(objectName);
            writer.WriteStartObject();

            foreach (var property in propertyNames)
            {
                writer.WritePropertyValue(property, type.GetProperty(property).GetValue(obj));
            }

            writer.WriteEndObject();
        }

        public static void WriteVentObject(this JsonWriter writer, object obj)
        {
            writer.WriteStartObject();

            foreach (PropertyInfo property in obj.GetType().GetProperties())
            {
                if (property.CanWrite && property.CanRead)
                {
                    var value = property.GetValue(obj);

                    writer.WritePropertyName(property.Name);
                    WriteVentValue(writer, value);
                }
            }

            writer.WriteEndObject();
        }

        public static void WriteVentValue(this JsonWriter writer, object value)
        {
            if (value == null)
            {
                writer.WriteNull();
            }

            var type = value.GetType();

            if (EntityReflection.IsPrimitiveOrString(type))
            {
                writer.WriteValue(value);
            }
            else if (EntityReflection.IsEntity(type))
            {
                writer.WriteValue(((IEntity)value).Id);
            }
            else if (type.IsArray)
            {
                WriteVentArray(writer, (Array)value);
            }
            else if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    WriteVentList(writer, (IList)value);
                }
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    WriteVentDictionary(writer, (IDictionary)value);
                }
            }
            else
            {
                WriteVentObject(writer, value);
            }
        }

        public static void WriteVentArray(this JsonWriter writer, Array array)
        {
            writer.WriteStartArray();

            for (int i = 0; i < array.Length; i++)
            {
                WriteVentValue(writer, array.GetValue(i));
            }

            writer.WriteEndArray();
        }

        public static void WriteVentList(this JsonWriter writer, IList list)
        {
            writer.WriteStartArray();

            foreach (object element in list)
            {
                WriteVentValue(writer, element);
            }

            writer.WriteEndArray();
        }

        public static void WriteVentDictionary(this JsonWriter writer, IDictionary dictionary)
        {
            writer.WriteStartObject();

            foreach (DictionaryEntry entry in dictionary)
            {
                writer.WritePropertyName(entry.Key.ToString());
                WriteVentValue(writer, entry.Value);
            }

            writer.WriteEndObject();
        }

        public static void WriteVentValue(this Utf8JsonWriter writer, object value)
        {
            if (value == null)
            {
                writer.WriteNullValue();

            }
            else
            {
                var type = value.GetType();

                if (EntityReflection.IsPrimitiveOrString(type))
                {
                    WritePrimitive(writer, value);
                }
                else if (EntityReflection.IsEntity(type))
                {
                    writer.WriteNumberValue(((IEntity)value).Id);
                }
                else if (type.IsArray)
                {
                    WriteVentArray(writer, (Array)value);
                }
                else if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        WriteVentList(writer, (IList)value);
                    }
                    else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                    {
                        WriteVentDictionary(writer, (IDictionary)value);
                    }
                }
                else
                {
                    WriteVentObject(writer, value);
                }
            }
        }

        public static void WriteVentArray(this Utf8JsonWriter writer, Array array)
        {
            writer.WriteStartArray();

            for (int i = 0; i < array.Length; i++)
            {
                WriteVentValue(writer, array.GetValue(i));
            }

            writer.WriteEndArray();
        }

        public static void WriteVentList(this Utf8JsonWriter writer, IList list)
        {
            writer.WriteStartArray();

            foreach (object element in list)
            {
                WriteVentValue(writer, element);
            }

            writer.WriteEndArray();
        }

        public static void WriteVentDictionary(this Utf8JsonWriter writer, IDictionary dictionary)
        {
            writer.WriteStartObject();

            foreach (DictionaryEntry entry in dictionary)
            {
                writer.WritePropertyName(entry.Key.ToString());
                WriteVentValue(writer, entry.Value);
            }

            writer.WriteEndObject();
        }

        public static void WriteVentObject(this Utf8JsonWriter writer, object obj)
        {
            writer.WriteStartObject();

            foreach (PropertyInfo property in obj.GetType().GetProperties())
            {
                if (property.CanWrite && property.CanRead)
                {
                    var value = property.GetValue(obj);

                    writer.WritePropertyName(property.Name);
                    WriteVentValue(writer, value);
                }
            }

            writer.WriteEndObject();
        }

        public static void WritePrimitive(this Utf8JsonWriter writer, object value)
        {
            switch (Type.GetTypeCode(value.GetType()))
            {
                case TypeCode.Byte:
                    writer.WriteNumberValue((byte)value);   
                    break;
                case TypeCode.Int16:
                    writer.WriteNumberValue((short)value);
                    break;
                case TypeCode.Int32:
                    writer.WriteNumberValue((int)value);
                    break;
                case TypeCode.Int64:
                    writer.WriteNumberValue((long)value);
                    break;
                case TypeCode.UInt16:
                    writer.WriteNumberValue((ushort)value);
                    break;
                case TypeCode.UInt32:
                    writer.WriteNumberValue((uint)value);
                    break;
                case TypeCode.UInt64:
                    writer.WriteNumberValue((ulong)value);
                    break;
                case TypeCode.Double:
                    writer.WriteNumberValue((double)value);
                    break;
                case TypeCode.Single:
                    writer.WriteNumberValue((float)value);
                    break;
                case TypeCode.String:
                    writer.WriteStringValue((string)value);
                    break;
                case TypeCode.Char:
                    writer.WriteStringValue(value.ToString());
                    break;
                case TypeCode.Boolean:
                    writer.WriteBooleanValue((bool)value);
                    break;
                default:
                    throw new NotImplementedException($"type {value.GetType()} has not backing code ");
            }
        }

        public static object ReadPrimitive(this Utf8JsonReader reader, Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                    return reader.GetByte();
                case TypeCode.Int16:
                    return reader.GetInt16();
                case TypeCode.Int32:
                    return reader.GetInt32();
                case TypeCode.Int64:
                    return reader.GetInt64();
                case TypeCode.UInt16:
                    return reader.GetUInt16();
                case TypeCode.UInt32:
                    return reader.GetUInt32();
                case TypeCode.UInt64:
                    return reader.GetUInt64();
                case TypeCode.Double:
                    return reader.GetDouble();
                case TypeCode.Single:
                    return reader.GetSingle();
                case TypeCode.String:
                    return reader.GetString();
                case TypeCode.Char:
                    return reader.GetString()[0];
                case TypeCode.Boolean:
                    return reader.GetBoolean();
                default:
                    throw new NotImplementedException($"type {type.Name} has not backing code ");
            }
        }

        public static T ReadProperty<T>(ref Utf8JsonReader reader, string expectedPropertyName)
        {
            if (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals(expectedPropertyName))
                    {
                        if (reader.Read())
                        {
                            return (T)ReadPrimitive(reader, typeof(T));
                        }
                    }
                    else
                    {
                        throw new System.Text.Json.JsonException($"expected  name {expectedPropertyName} but found {reader.GetString()}");
                    }

                    throw new System.Text.Json.JsonException($"expected {typeof(T)} but found no more tokens");
                }

                throw new System.Text.Json.JsonException($"expected PropertyName but found {reader.TokenType}");
            }

            throw new System.Text.Json.JsonException($"expected PropertyName but found no more tokens");
        }

        public delegate void JsonArrayReader(ref Utf8JsonReader reader);

        public static void ParseJsonArray(ref Utf8JsonReader reader, JsonArrayReader arrayElementHandler)
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                while (reader.TokenType != JsonTokenType.EndArray)
                {
                    arrayElementHandler(ref reader);
                }
            }
            else
            {
                throw new System.Text.Json.JsonException($"expected JsonTokenType.StartArray but found {reader.TokenType}.");
            }
        }
    }
}


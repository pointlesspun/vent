using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Vent.ToJson
{
    public enum EntitySerialization
    {
        AsReference,
        AsValue
    };

    public static class Utf8JsonUtil
    {
        public static void WriteVentValue(
            this Utf8JsonWriter writer, 
            object value, 
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
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
                else if (EntityReflection.IsEntity(type) && entitySerialization == EntitySerialization.AsReference)
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

        public static void WriteVentArray(
            this Utf8JsonWriter writer, 
            Array array, 
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            writer.WriteStartArray();

            for (int i = 0; i < array.Length; i++)
            {
                WriteVentValue(writer, array.GetValue(i), entitySerialization);
            }

            writer.WriteEndArray();
        }

        public static void WriteVentList(
            this Utf8JsonWriter writer, 
            IList list,
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            writer.WriteStartArray();

            foreach (object element in list)
            {
                WriteVentValue(writer, element, entitySerialization);
            }

            writer.WriteEndArray();
        }

        public static void WriteVentDictionary(
            this Utf8JsonWriter writer, 
            IDictionary dictionary,
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            writer.WriteStartObject();

            foreach (DictionaryEntry entry in dictionary)
            {
                writer.WritePropertyName(entry.Key.ToString());
                WriteVentValue(writer, entry.Value, entitySerialization);
            }

            writer.WriteEndObject();
        }

        public static void WriteVentObject(this Utf8JsonWriter writer, object obj)
        {
            if (obj == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStartObject();

                writer.WriteString(SharedJsonTags.EntityTypeTag, GetClassName(obj.GetType()));

                foreach (PropertyInfo propertyInfo in obj.GetType().GetProperties())
                {
                    if (propertyInfo.CanWrite && propertyInfo.CanRead)
                    {
                        var value = propertyInfo.GetValue(obj);
                        var entitySerialization = GetEntitySerialization(propertyInfo);

                        writer.WritePropertyName(propertyInfo.Name);
                        WriteVentValue(writer, value, entitySerialization);
                    }
                }
            }

            writer.WriteEndObject();
        }

        public static string GetClassName(Type type)
        {
            var stringBuilder = new StringBuilder();


            var genericArgs = type.GetGenericArguments();
            if (genericArgs != null && genericArgs.Length > 0)
            {
                stringBuilder.Append(type.Namespace + "." + type.Name.Substring(0, type.Name.Length - 2));
                stringBuilder.Append('<');
                stringBuilder.Append(string.Join(",", genericArgs.Select(arg => GetClassName(arg))));
                stringBuilder.Append('>');
            }
            else
            {
                stringBuilder.Append(type.Namespace + "." + type.Name);
            }

            return stringBuilder.ToString();
        }


        public static EntitySerialization GetEntitySerialization(this PropertyInfo propertyInfo) =>
                Attribute.IsDefined(propertyInfo, typeof(SerializeAsValueAttribute))
                            ? EntitySerialization.AsValue
                            : EntitySerialization.AsReference;

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
                        throw new JsonException($"expected  name {expectedPropertyName} but found {reader.GetString()}");
                    }

                    throw new JsonException($"expected {typeof(T)} but found no more tokens");
                }

                throw new JsonException($"expected PropertyName but found {reader.TokenType}");
            }

            throw new JsonException($"expected PropertyName but found no more tokens");
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
                throw new JsonException($"expected JsonTokenType.StartArray but found {reader.TokenType}.");
            }
        }
        public static void ReadAnyToken(this ref Utf8JsonReader reader)
        {
            if (!reader.Read())
            {
                if (reader.IsFinalBlock)
                {
                    throw new JsonException($"expected more tokens, but encountered the final block.");
                }
            }
        }

        public static void ReadToken(this ref Utf8JsonReader reader, JsonTokenType expectedToken)
        {
            ReadAnyToken(ref reader);

            if (reader.TokenType != expectedToken)
            {
                throw new JsonException($"expected {expectedToken} but found {reader.TokenType}.");
            }
        }

        public static string ReadString(this ref Utf8JsonReader reader)
        {
            ReadAnyToken(ref reader);

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"expected {JsonTokenType.String} but found {reader.TokenType}.");
            }

            return reader.GetString();
        }

        public static void ReadPropertyName(this ref Utf8JsonReader reader, string propertyName)
        {
            ReadAnyToken(ref reader);

            if (reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals(propertyName))
            {
                if (reader.TokenType != JsonTokenType.EndObject)
                {
                    throw new JsonException($"expected property name {propertyName} but found {reader.TokenType}.");
                }
            }
        }
    }
}


/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using System.Collections;
using System.Reflection;

using System.Text.Encodings.Web;
using System.Text;
using System.Text.Json;

using Vent.Util;
using Vent.Registry;
using Vent.ToJson.ClassResolver;

namespace Vent.ToJson.Writers
{
    public static class Utf8JsonWriterExtensions
    {

        /// <summary>
        /// Convenience method to make the most often used invocation a little easier to read
        /// </summary>
        /// <param name="registry"></param>
        /// <returns></returns>
        public static string WriteRegistryToJson(EntityRegistry registry)
        {
            return WriteObjectToJsonString(registry, EntitySerialization.AsValue);
        }

        public static string WriteObjectToJsonString(object obj, EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            var options = new JsonWriterOptions
            {
                Indented = true,
                // don't want to see escaped characters in the tests
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var memoryStream = new MemoryStream();
            var writer = new Utf8JsonWriter(memoryStream, options);
            writer.WriteValue(obj, entitySerialization);
            writer.Flush();
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }

        public static void WriteValue(
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

                if (Reflection.IsPrimitiveOrString(type))
                {
                    writer.WritePrimitive(value);
                }
                else if (Reflection.IsEntity(type) && entitySerialization == EntitySerialization.AsReference)
                {
                    writer.WriteNumberValue(((IEntity)value).Id);
                }
                else if (type.IsArray)
                {
                    writer.WriteArray((Array)value);
                }
                else if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    if (typeof(EntityRegistry).IsAssignableFrom(type))
                    {
                        writer.WriteObject(value);
                    }
                    else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        writer.WriteList((IList)value, entitySerialization);
                    }
                    else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                    {
                        writer.WriteDictionary((IDictionary)value, entitySerialization);
                    }
                }
                else
                {
                    writer.WriteObject(value);
                }
            }
        }

        public static void WriteArray(
            this Utf8JsonWriter writer,
            Array array,
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            writer.WriteStartArray();

            for (int i = 0; i < array.Length; i++)
            {
                writer.WriteValue(array.GetValue(i), entitySerialization);
            }

            writer.WriteEndArray();
        }

        public static void WriteList(
            this Utf8JsonWriter writer,
            IList list,
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            writer.WriteStartArray();

            foreach (object element in list)
            {
                writer.WriteValue(element, entitySerialization);
            }

            writer.WriteEndArray();
        }

        public static void WriteDictionary(
            this Utf8JsonWriter writer,
            IDictionary dictionary,
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            var keyType = dictionary.GetType().GetGenericArguments()[0];

            Contract.Requires<EntitySerializationException>(IsValidDictionaryKey(keyType), $"Cannot serialize dictionary keys of type {keyType}");

            var keyConverter = GetKeyConverter(keyType);

            writer.WriteStartObject();

            foreach (DictionaryEntry entry in dictionary)
            {
                writer.WritePropertyName(keyConverter(entry.Key));
                writer.WriteValue(entry.Value, entitySerialization);
            }

            writer.WriteEndObject();
        }

        private static Func<object, string> GetKeyConverter(Type keyType)
        {
            if (keyType == typeof(string) || keyType.IsPrimitive)
            {
                return (str) => str.ToString();
            }
            else if (keyType == typeof(DateTime))
            {
                return (dateTime) => ((DateTime)dateTime).Ticks.ToString();
            }

            throw new NotImplementedException($"Cannot convert dictionary key from {keyType.Name}.");
        }

        public static bool IsValidDictionaryKey(Type keyType)
        {
            return keyType != null
                && (Reflection.IsPrimitiveOrString(keyType)
                || Reflection.IsEntity(keyType)
                || keyType == typeof(DateTime));
        }

        public static void WriteObject(this Utf8JsonWriter writer, object obj)
        {
            if (obj == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStartObject();

                if (obj is IEntity)
                {
                    writer.WriteString(SharedJsonTags.EntityTypeTag, obj.GetType().ToVentClassName());
                }

                if (obj is ICustomJsonSerializable customSerializable)
                {
                    customSerializable.Write(writer);
                }
                else
                {
                    foreach (PropertyInfo propertyInfo in obj.GetType().GetProperties())
                    {
                        if (propertyInfo.CanWrite && propertyInfo.CanRead)
                        {
                            var value = propertyInfo.GetValue(obj);
                            var entitySerialization = propertyInfo.GetEntitySerialization();

                            writer.WritePropertyName(propertyInfo.Name);
                            writer.WriteValue(value, entitySerialization);
                        }
                    }
                }
            }

            writer.WriteEndObject();
        }

        public static void WriteProperty(
            this Utf8JsonWriter writer,
            string name,
            object value,
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            writer.WritePropertyName(name);
            writer.WriteValue(value, entitySerialization);
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
    }
}

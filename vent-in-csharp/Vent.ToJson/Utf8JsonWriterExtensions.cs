using System.Collections;
using System.Reflection;
using System.Text.Json;

namespace Vent.ToJson
{
    public static class Utf8JsonWriterExtensions
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

                writer.WriteString(SharedJsonTags.EntityTypeTag, obj.GetType().ToVentClassName());

                foreach (PropertyInfo propertyInfo in obj.GetType().GetProperties())
                {
                    if (propertyInfo.CanWrite && propertyInfo.CanRead)
                    {
                        var value = propertyInfo.GetValue(obj);
                        var entitySerialization = propertyInfo.GetEntitySerialization();

                        writer.WritePropertyName(propertyInfo.Name);
                        WriteVentValue(writer, value, entitySerialization);
                    }
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
    }
}

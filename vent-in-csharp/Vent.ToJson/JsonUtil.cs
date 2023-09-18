using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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
    }
}

/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using System.Text.Json;
using Vent.Registry;
using Vent.Util;

namespace Vent.ToJson.Readers
{
    public class Utf8JsonObjectReader<T> : AbstractUtf8JsonReader<T>
    {
        public override object ReadValue(ref Utf8JsonReader reader,
            JsonReaderContext context,
            EntitySerialization _)
        {
            return Utf8JsonObjectReaderExtensions.ReadObject(ref reader, context, typeof(T));
        }
    }

    public static class Utf8JsonObjectReaderExtensions
    {
        
        /// <summary>
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="contextStack"></param>
        /// <returns></returns>
        public static object ReadObject(this ref Utf8JsonReader reader,
            JsonReaderContext context,
            Type objectType)
        {
            // early exit in case of a null
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            // if there is a start of the object read it
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                var obj = Activator.CreateInstance(objectType);
                ReadObjectProperties(ref reader, context, obj);
                return obj;
            }
            else
            {
                throw new JsonException($"Expected start of an object, found {reader.TokenType}.");
            }
        }

        public static void ReadObjectProperties(
                this ref Utf8JsonReader reader,
                JsonReaderContext context,
                object obj)
        {
            Contract.NotNull(context);
            Contract.NotNull(obj);

            if (obj is ICustomJsonSerializable customSerializable)
            {
                customSerializable.Read(ref reader, context);
                // consume the end of object token
                reader.ReadAnyToken();
            }
            else
            {
                while (ReadObjectProperty(ref reader, context, obj));
            }
        }

        public static bool ReadObjectProperty(
                this ref Utf8JsonReader reader,
                JsonReaderContext context,
                object obj)
        {
            var type = obj.GetType();

            if (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                if (propertyName == SharedJsonTags.EntityTypeTag)
                {
                    throw new EntitySerializationException($"Trying to serialize entity of type {type.ToVentClassName()} as an object but it's an entity. Use ReadEntity instead of ReadObject.");
                }

                var info = type.GetProperty(propertyName);

                if (info != null)
                {
                    reader.ReadAnyToken();
                    {
                        var value = reader.ReadVentValue(info.PropertyType, context, info.GetEntitySerialization());

                        if (value is ForwardEntityReference reference)
                        {
                            reference.Key = info;
                            context.Top.AddReference(obj, reference);
                        }
                        else
                        {
                            info.SetValue(obj, value);
                        }
                    }
                }
                else
                {
                    throw new NotImplementedException($"Object of type {type} does not have a property called {propertyName}");
                }

                return true;
            }

            return false;
        }
    }
}

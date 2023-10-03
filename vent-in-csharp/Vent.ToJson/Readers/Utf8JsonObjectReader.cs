using System.Text;
using System.Text.Json;

namespace Vent.ToJson.Readers
{
    // xxx move to AbstractUtf8JsonReader, see Utf8JsonArrayReader
    public static class Utf8JsonObjectReader
    {
        public static T ReadObjectFromJson<T>(
            string jsonText,
            JsonReaderContext context = null
        )
        {
            var objectReader = new Utf8JsonReader(Encoding.UTF8.GetBytes(jsonText));
            return ReadObject<T>(ref objectReader, context);
        }

        public static T ReadObject<T>(
            this ref Utf8JsonReader reader,
            JsonReaderContext context = null
        )
        {
            // create a new context if none was provided
            context ??= new JsonReaderContext(new EntityRegistry(), ClassLookup.CreateDefault());

            if (reader.TokenType == JsonTokenType.None)
            {
                reader.Read();
            }

            return (T)ReadObject(ref reader, context, typeof(T));
        }

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

                        if (value is ForwardReference reference)
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

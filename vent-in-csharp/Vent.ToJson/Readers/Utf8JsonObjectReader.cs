using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Vent.ToJson.Readers
{
    public static class Utf8JsonObjectReader
    {
        /// <summary>
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="contextStack"></param>
        /// <returns></returns>
        public static object ReadVentObject(this ref Utf8JsonReader reader,
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
                ReadVentObjectProperties(ref reader, context, obj);
                return obj;
            }
            else
            {
                throw new JsonException($"Expected start of an object, found {reader.TokenType}.");
            }
        }

        public static void ReadVentObjectProperties(
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
                while (ReadVentObjectProperty(ref reader, context, obj)) ;
            }
        }

        public static bool ReadVentObjectProperty(
                this ref Utf8JsonReader reader,
                JsonReaderContext context,
                object obj)
        {
            var type = obj.GetType();

            if (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

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

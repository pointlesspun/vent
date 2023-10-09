using System.Collections;
using System.Text.Json;
using Vent.Registry;
using Vent.Util;

namespace Vent.ToJson.Readers
{
    public static class Utf8JsonReaderExtensions
    {
        public static T ReadVentValue<T>(this ref Utf8JsonReader reader,
            JsonReaderContext context = null,
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            return (T) ReadVentValue(ref reader, typeof(T), context, entitySerialization);  
        }

        /// <summary>
        /// Read a value supported by Vent.ToJson, this includes: primitives, strings, arrays (todo),
        /// lists, dictionaries, entities and objects. Any object outside this set will cause
        /// an exception to be thrown. The reader is expected to contain valid json or the reader
        /// will throw an exception.
        /// 
        /// </summary>
        /// <param name="reader">Json reader containing valid data</param>
        /// <param name="valueType">Type to convert to</param>
        /// <param name="context">Context holding the current registry and class lookup</param>
        /// <param name="entitySerialization">If set to 'AsReference' entities will be read as references
        /// and resolved against the current registry. If set to 'AsValue' the entity will be created
        /// from the current parameters.</param>
        /// <returns>The parsed object or null if the current reader value is null</returns>
        /// <exception cref="NotImplementedException"></exception>
        public static object ReadVentValue(
            this ref Utf8JsonReader reader,
            Type valueType,
            JsonReaderContext context = null,
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            // create a new context if none was provided
            context ??= new JsonReaderContext(new EntityRegistry(), ClassLookup.CreateDefault());

            // check if the reader has read something. if not read the first token
            if (reader.TokenType == JsonTokenType.None)
            {
                reader.ReadAnyToken();
            }

            if (reader.TokenType == JsonTokenType.Null || reader.TokenType == JsonTokenType.None)
            {
                return null;
            }
            else if (Reflection.IsEntity(valueType))
            {
                return reader.ReadEntity(context, valueType, entitySerialization);
            }
            else if (Reflection.IsPrimitiveOrString(valueType))
            {
                return reader.ReadPrimitive(valueType);
            }
            else if (valueType.IsArray)
            {
                return reader.ReadArray(context, valueType, entitySerialization);
            }
            else if (valueType == typeof(DateTime))
            {
                return reader.ReadDateTime();
            }
            else if (typeof(IEnumerable).IsAssignableFrom(valueType) && valueType.IsGenericType)
            {
                if (valueType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    return reader.ReadList(context, valueType.GetGenericArguments()[0], entitySerialization);
                }
                else if (valueType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    return reader.ReadDictionary(context, valueType, entitySerialization);
                }
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                return reader.ReadObject(context, valueType);
            }

            throw new NotImplementedException($"Cannot parse {valueType} to a value");
        }

        public static T ReadPrimitiveProperty<T>(this ref Utf8JsonReader reader, string propertyName)
        {
            ReadPropertyName(ref reader, propertyName);
            ReadAnyToken(ref reader);
            return (T) reader.ReadPrimitive(typeof(T));
        }

        public delegate void JsonArrayReader(ref Utf8JsonReader reader);


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


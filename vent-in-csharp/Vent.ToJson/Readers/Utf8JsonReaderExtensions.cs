using System.Collections;
using System.Text.Json;

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
            else if (EntityReflection.IsEntity(valueType))
            {
                return reader.ReadEntity(context, entitySerialization);
            }
            else if (EntityReflection.IsPrimitiveOrString(valueType))
            {
                return reader.ReadPrimitive(valueType);
            }
            else if (valueType.IsArray)
            {
                return reader.ReadArray(context, valueType, entitySerialization);
            }
            else if (valueType == typeof(DateTime))
            {
                return ReadDateTime(ref reader);
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

        

        /// <summary>
        /// Read the date time from the current token in the reader. If the token
        /// is a number, it will be assumed to represent ticks and the date time
        /// returned will be based on these ticks. Otherwise DateTime.Parse
        /// will be used.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static DateTime ReadDateTime(this ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                // parse the datetime as if it were ticks
                return new DateTime(reader.GetInt64());
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                return DateTime.Parse(reader.GetString());
            }
            
            throw new JsonException($"JsonReader can't covert {reader.TokenType} to a DateTime.");
        }


        public static T ReadPrimitiveProperty<T>(this ref Utf8JsonReader reader, string propertyName)
        {
            ReadPropertyName(ref reader, propertyName);
            ReadAnyToken(ref reader);
            return reader.ReadPrimitive<T>();
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
                            return reader.ReadPrimitive<T>();
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


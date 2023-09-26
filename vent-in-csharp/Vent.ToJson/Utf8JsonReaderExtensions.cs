using System.Collections;
using System.Text.Json;

namespace Vent.ToJson
{
    public static class Utf8JsonReaderExtensions
    {       
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

        /// <summary>
        /// Creates an object from json based assuming a vent specific convention, ie:
        ///  * Expects the next token in the reader to be null or a start object. If null is found null will be returned.
        ///  * If the token is a start object, ParseVentObjectProperties is called
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="registry"></param>
        /// <param name="forwardReferences"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static object ReadNullOrVentObject(this ref Utf8JsonReader reader, JsonReaderContext context)
        {
            reader.ReadAnyToken();

            return reader.TokenType switch
            {
                JsonTokenType.Null => null,
                JsonTokenType.StartObject => ReadVentObject(ref reader, context),
                _ => throw new NotImplementedException($"Unexpected token {reader.TokenType} encountered"),
            };
        }

        public static object ReadVentObject(ref Utf8JsonReader reader, JsonReaderContext context)
        {
            reader.ReadPropertyName(SharedJsonTags.EntityTypeTag);
            {
                var obj = CreateInstanceFromTypeName(ref reader, context);
                ReadVentObjectProperties(ref reader, context, obj);
                return obj;
            }
        }

        public static object CreateInstanceFromTypeName(ref Utf8JsonReader reader, JsonReaderContext context)
        {
            var className = reader.ReadString();

            if (className != null)
            {
                return className.CreateInstance(context.ClassLookup);
            }
            else
            {
                throw new JsonException($"Cannot instantiate entity with a null {className}");
            }
        }

        public static void ReadVentObjectProperties(
                this ref Utf8JsonReader reader,
                JsonReaderContext context,
                object obj)
        {
            Contract.NotNull(context);
            Contract.NotNull(obj);

            var type = obj.GetType();
            List<ForwardReference> objectReferences = null;

            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                var info = type.GetProperty(propertyName);

                if (info != null)
                {
                    reader.ReadAnyToken();
                    {
                        var value = reader.ReadVentValue(context, info.PropertyType, info.GetEntitySerialization());

                        if (value is ForwardReference reference)
                        {
                            reference.Key = info;
                            objectReferences ??= context.AddReferenceList(obj);
                            objectReferences.Add(reference);
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
            }
        }


        public static object ReadVentValue(
            this ref Utf8JsonReader reader,
            JsonReaderContext context,
            Type valueType,
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }
            else if (EntityReflection.IsEntity(valueType) && entitySerialization == EntitySerialization.AsReference)
            {
                var key = reader.GetInt32();

                return context.Registry.ContainsKey(key) ? context.Registry[key] : new ForwardReference(key);
            }
            else if (EntityReflection.IsPrimitiveOrString(valueType))
            {
                return ReadPrimitive(reader, valueType);
            }
            else if (typeof(IEnumerable).IsAssignableFrom(valueType) && valueType.IsGenericType)
            {
                if (valueType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    return ReadList(ref reader, context, valueType, entitySerialization);
                }
                else if (valueType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    return ReadDictionary(ref reader, context, valueType, entitySerialization);
                }
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                return ReadVentObject(ref reader, context);
            }

            throw new NotImplementedException($"Cannot parse {valueType} to a value");
        }

        public static IList ReadList(this ref Utf8JsonReader reader,
            JsonReaderContext context,
            Type type,
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            var elementType = type.GetGenericArguments()[0];
            var listType = typeof(List<>).MakeGenericType(elementType);

            if (typeof(IEntity).IsAssignableFrom(elementType) && entitySerialization == EntitySerialization.AsReference)
            {
                return ReadEntityList(ref reader, context, listType);
            }
            else
            {
                return ReadValueList(ref reader, context, listType, elementType, entitySerialization);
            }
        }

        public static IList ReadEntityList(this ref Utf8JsonReader reader,
            JsonReaderContext context,
            Type listType)
        {
            List<ForwardReference> listEntityReferences = null;
            var listValue = (IList)Activator.CreateInstance(listType);

            void ParseEntityListElement(ref Utf8JsonReader reader)
            {
                reader.ReadAnyToken();

                if (reader.TokenType != JsonTokenType.EndArray)
                {
                    var entityId = reader.GetInt32();
                    if (context.Registry.ContainsKey(entityId))
                    {
                        listValue.Add(context.Registry[entityId]);
                    }
                    else
                    {
                        listEntityReferences ??= context.AddReferenceList(listValue);
                        listEntityReferences.Add(new ForwardReference(entityId, listValue.Count));

                        listValue.Add(null);
                    }
                }
            }

            ParseJsonArray(ref reader, ParseEntityListElement);
            
            return listValue;
        }

        public static IList ReadValueList(this ref Utf8JsonReader reader,
         JsonReaderContext context,
         Type listType, Type listElementType,
         EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            var listValue = (IList)Activator.CreateInstance(listType);

            void ParseListElement(ref Utf8JsonReader reader)
            {
                reader.ReadAnyToken();

                if (reader.TokenType != JsonTokenType.EndArray)
                {
                    listValue.Add(ReadVentValue(ref reader, context, listElementType, entitySerialization));
                }
            }

            ParseJsonArray(ref reader, ParseListElement);

            return listValue;
        }

        public static IDictionary ReadDictionary(this ref Utf8JsonReader reader,
            JsonReaderContext context,
            Type type,
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            var keyConverter = GetKeyConverter(type.GetGenericArguments()[0]);
            var valueType = type.GetGenericArguments()[1];
            var dictionary = (IDictionary)Activator.CreateInstance(type);
            List<ForwardReference> objectReferences = null;

            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                var key = keyConverter(reader.GetString());

                reader.ReadAnyToken();
                {
                    var value = ReadVentValue(ref reader, context, valueType, entitySerialization);

                    if (value is ForwardReference reference)
                    {
                        reference.Key = key;
                        objectReferences ??= context.AddReferenceList(dictionary);
                        objectReferences.Add(reference);
                    }
                    else
                    {
                        dictionary[key] = value;
                    }
                }
            }

            return dictionary;
        }

        private static Func<string, object> GetKeyConverter(Type keyType)
        {
            if (keyType == typeof(string))
            {
                return (str) => str;
            }
            else if (keyType.IsPrimitive)
            {
                return str => Convert.ChangeType(str, keyType);
            }

            throw new NotImplementedException($"Cannot convert key from {keyType.Name}");
        }
    }
}


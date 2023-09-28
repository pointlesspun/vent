using System.Collections;
using System.Reflection.PortableExecutable;
using System.Runtime.Serialization;
using System.Text.Json;

namespace Vent.ToJson
{
    public static class Utf8JsonReaderExtensions
    {
        public static T ReadPrimitiveProperty<T>(this ref Utf8JsonReader reader, string propertyName)
        {
            ReadPropertyName(ref reader, propertyName);
            ReadAnyToken(ref reader);
            return (T)ParseCurrentTokenAsPrimitive(reader, typeof(T));
        }

        public static T ReadPrimitive<T>(this ref Utf8JsonReader reader)
        {
            ReadAnyToken(ref reader);
            return (T) ParseCurrentTokenAsPrimitive(reader, typeof(T));
        }

        public static object ParseCurrentTokenAsPrimitive(Utf8JsonReader reader, Type type)
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
                            return (T)ParseCurrentTokenAsPrimitive(reader, typeof(T));
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
        public static object ReadNullOrVentObject(
            this ref Utf8JsonReader reader
            , List<JsonReaderContext> contextStack)
        {
            reader.ReadAnyToken();

            return reader.TokenType switch
            {
                JsonTokenType.Null => null,
                JsonTokenType.StartObject => ReadVentObject(ref reader, contextStack),
                _ => throw new NotImplementedException($"Unexpected token {reader.TokenType} encountered"),
            };
        }

        public static object ReadVentObject(ref Utf8JsonReader reader, List<JsonReaderContext> contextStack)
        {
            reader.ReadPropertyName(SharedJsonTags.EntityTypeTag);
            {
                var obj = CreateInstanceFromTypeName(ref reader, contextStack[0].ClassLookup);
                ReadVentObjectProperties(ref reader, contextStack, obj);
                return obj;
            }
        }

        public static object CreateInstanceFromTypeName(ref Utf8JsonReader reader, Dictionary<string, Type> classLookup)
        {
            var className = reader.ReadString();

            if (className != null)
            {
                return className.CreateInstance(classLookup);
            }
            else
            {
                throw new JsonException($"Cannot instantiate entity with a null {className}");
            }
        }

        public static void ReadVentObjectProperties(
                this ref Utf8JsonReader reader,
                List<JsonReaderContext> contextStack,
                object obj)
        {
            Contract.NotNull(contextStack);
            Contract.NotNull(obj);

            List<ForwardReference> objectReferences = null;

            if (obj is ICustomJsonSerializable customSerializable)
            {
                customSerializable.Read(ref reader, contextStack, objectReferences);
                // consume the end of object token
                reader.ReadAnyToken();
            }
            else
            {
                while (ReadVentObjectProperty(ref reader, contextStack, ref objectReferences, obj)) ;
            }
        }

        public static bool ReadVentObjectProperty(
                this ref Utf8JsonReader reader,
                List<JsonReaderContext> contextStack,
                ref List<ForwardReference> objectReferences,
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
                        var value = reader.ReadVentValue(contextStack, info.PropertyType, info.GetEntitySerialization());

                        if (value is ForwardReference reference)
                        {
                            reference.Key = info;
                            objectReferences ??= contextStack[0].AddObjectReferenceList(obj);
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

                return true;
            }

            return false;
        }


        public static object ReadVentValue(
            this ref Utf8JsonReader reader,
            List<JsonReaderContext> contextStack,
            Type valueType,
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }
            else if (EntityReflection.IsEntity(valueType))
            {
                return ReadEntity(ref reader, contextStack, entitySerialization);
            }
            else if (EntityReflection.IsPrimitiveOrString(valueType))
            {
                return ParseCurrentTokenAsPrimitive(reader, valueType);
            }
            else if (typeof(IEnumerable).IsAssignableFrom(valueType) && valueType.IsGenericType)
            {
                if (valueType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    return ReadList(ref reader, contextStack, valueType, entitySerialization);
                }
                else if (valueType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    return ReadDictionary(ref reader, contextStack, valueType, entitySerialization);
                }
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                return ReadVentObject(ref reader, contextStack);
            }

            throw new NotImplementedException($"Cannot parse {valueType} to a value");
        }

        public static object ReadEntity(this ref Utf8JsonReader reader,
            List<JsonReaderContext> contextStack,
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (entitySerialization == EntitySerialization.AsReference)
            {
                var key = reader.GetInt32();

                return contextStack[0].Registry.ContainsKey(key)
                        ? contextStack[0].Registry[key]
                        : new ForwardReference(contextStack[0].Registry, key);
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                reader.ReadPropertyName(SharedJsonTags.EntityTypeTag);
                    
                var entity = (IEntity) CreateInstanceFromTypeName(ref reader, contextStack[0].ClassLookup);

                if (entity is EntityRegistry registry)
                {
                    contextStack.Insert(0, new JsonReaderContext(registry, contextStack[0].ClassLookup));
                    ReadVentObjectProperties(ref reader, contextStack, entity);
                    TypeNameNode.ResolveForwardReferences(contextStack[0].ForwardReferenceLookup);
                    contextStack.RemoveAt(0);
                }
                else
                {
                    ReadVentObjectProperties(ref reader, contextStack, entity);
                }

                return entity;
            }

            throw new NotImplementedException($"Cannot parse {reader.TokenType} to an serialization of an entity");
        }

        public static IList ReadList(this ref Utf8JsonReader reader,
            List<JsonReaderContext> contextStack,
            Type type,
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            var elementType = type.GetGenericArguments()[0];
            var listType = typeof(List<>).MakeGenericType(elementType);

            if (typeof(IEntity).IsAssignableFrom(elementType) && entitySerialization == EntitySerialization.AsReference)
            {
                return ReadEntityList(ref reader, contextStack, listType);
            }
            else
            {
                return ReadValueList(ref reader, contextStack, listType, elementType, entitySerialization);
            }
        }

        public static IList ReadEntityList(this ref Utf8JsonReader reader,
            List<JsonReaderContext> contextStack,
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
                    if (contextStack[0].Registry.ContainsKey(entityId))
                    {
                        listValue.Add(contextStack[0].Registry[entityId]);
                    }
                    else
                    {
                        listEntityReferences ??= contextStack[0].AddObjectReferenceList(listValue);
                        listEntityReferences.Add(new ForwardReference(contextStack[0].Registry, entityId, listValue.Count));

                        listValue.Add(null);
                    }
                }
            }

            ParseJsonArray(ref reader, ParseEntityListElement);
            
            return listValue;
        }

        public static IList ReadValueList(this ref Utf8JsonReader reader,
         List<JsonReaderContext> contextStack,
         Type listType, Type listElementType,
         EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            var listValue = (IList)Activator.CreateInstance(listType);

            void ParseListElement(ref Utf8JsonReader reader)
            {
                reader.ReadAnyToken();

                if (reader.TokenType != JsonTokenType.EndArray)
                {
                    listValue.Add(ReadVentValue(ref reader, contextStack, listElementType, entitySerialization));
                }
            }

            ParseJsonArray(ref reader, ParseListElement);

            return listValue;
        }

        public static IDictionary ReadDictionary(this ref Utf8JsonReader reader,
            List<JsonReaderContext> contextStack,
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
                    var value = ReadVentValue(ref reader, contextStack, valueType, entitySerialization);

                    if (value is ForwardReference reference)
                    {
                        reference.Key = key;
                        objectReferences ??= contextStack[0].AddObjectReferenceList(dictionary);
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


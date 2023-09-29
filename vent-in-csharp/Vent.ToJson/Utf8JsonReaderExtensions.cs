using System.Collections;
using System.Text.Json;

namespace Vent.ToJson
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
        /// <param name="reader"></param>
        /// <param name="valueType"></param>
        /// <param name="context"></param>
        /// <param name="entitySerialization"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static object ReadVentValue(
            this ref Utf8JsonReader reader,
            Type valueType,
            JsonReaderContext context = null,
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            // create a new context if none was provided
            context ??= new JsonReaderContext(new EntityRegistry(), ClassLookup.CreateDefault());

            // xxx todo add array
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
                return ReadEntity(ref reader, context, entitySerialization);
            }
            else if (EntityReflection.IsPrimitiveOrString(valueType))
            {
                return ReadPrimitive(reader, valueType);
            }
            else if (valueType.IsArray)
            {
                // xxx to do
            }
            else if (valueType == typeof(DateTime))
            {
                return ParseDateTime(ref reader);
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

        public static DateTime ParseDateTime(this ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                // parse the datetime as if it were ticks
                return new DateTime(reader.GetInt64());
            }
            
            return DateTime.Parse(reader.GetString());
        }

        /// <summary>
        /// Read an object using the Vent object convention. This convention expects the format to be
        ///
        /// "{
        ///     "__entityType": "className",
        ///     ... vent compatible properties
        /// }"
        /// 
        /// The "className" needs to be declared in the JsonReaderContext.ClassLookup.
        /// 
        /// If this call is succesfull, the reader will be at the token after the closing bracket.
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="contextStack"></param>
        /// <returns></returns>
        public static object ReadVentObject(this ref Utf8JsonReader reader, JsonReaderContext context)
        {
            // early exit in case of a null
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            // if there is a start of the object read it
            if (reader.TokenType == JsonTokenType.StartObject) 
            {
                reader.Read();
            }

            reader.ReadPropertyName(SharedJsonTags.EntityTypeTag);
            {
                var obj = CreateInstanceFromTypeName(ref reader, context.ClassLookup);
                ReadVentObjectProperties(ref reader, context, obj);
                return obj;
            }
        }

        public static T ReadPrimitiveProperty<T>(this ref Utf8JsonReader reader, string propertyName)
        {
            ReadPropertyName(ref reader, propertyName);
            ReadAnyToken(ref reader);
            return (T)ReadPrimitive(reader, typeof(T));
        }

        public static T ReadPrimitive<T>(this ref Utf8JsonReader reader)
        {
            ReadAnyToken(ref reader);
            return (T) ReadPrimitive(reader, typeof(T));
        }

        public static object ReadPrimitive(Utf8JsonReader reader, Type type)
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

        public static object ReadEntity(this ref Utf8JsonReader reader,
            JsonReaderContext context,
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (entitySerialization == EntitySerialization.AsReference)
            {
                var key = reader.GetInt32();

                return context.TopRegistry.ContainsKey(key)
                        ? context.TopRegistry[key]
                        : new ForwardReference(context.TopRegistry, key);
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                reader.ReadPropertyName(SharedJsonTags.EntityTypeTag);
                    
                var entity = (IEntity) CreateInstanceFromTypeName(ref reader, context.ClassLookup);

                if (entity is EntityRegistry registry)
                {
                    context.Push(registry);
                    
                    ReadVentObjectProperties(ref reader, context, entity);

                    // are there any references to resolve ?
                    if (context.Top.ForwardReferenceLookup != null)
                    {
                        TypeNameNode.ResolveForwardReferences(context.TopLookup);
                    }

                    context.Pop();
                }
                else
                {
                    ReadVentObjectProperties(ref reader, context, entity);
                }

                return entity;
            }

            throw new NotImplementedException($"Cannot parse {reader.TokenType} to an serialization of an entity");
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
            var listValue = (IList)Activator.CreateInstance(listType);

            void ParseEntityListElement(ref Utf8JsonReader reader)
            {
                reader.ReadAnyToken();

                if (reader.TokenType != JsonTokenType.EndArray)
                {
                    // xxx test if entity points to -1
                    var entityId = reader.GetInt32();
                    if (context.TopRegistry.ContainsKey(entityId))
                    {
                        listValue.Add(context.TopRegistry[entityId]);
                    }
                    else
                    {
                        context.Top.AddReference(listValue,
                            new ForwardReference(context.TopRegistry, entityId, listValue.Count));
                        // add a null value, this will be replaced when the forward references are
                        // resolved with the actual entity
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
                    listValue.Add(ReadVentValue(ref reader, listElementType, context, entitySerialization));
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

            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                var key = keyConverter(reader.GetString());

                reader.ReadAnyToken();
                {
                    var value = ReadVentValue(ref reader, valueType, context, entitySerialization);

                    if (value is ForwardReference reference)
                    {
                        reference.Key = key;
                        context.Top.AddReference(dictionary, reference);
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
            // xxx add timestamp

            throw new NotImplementedException($"Cannot convert key from {keyType.Name}");
        }
    }
}


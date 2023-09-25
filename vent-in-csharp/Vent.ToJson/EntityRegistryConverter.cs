using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vent.ToJson
{
    public class EntityRegistryConverter : JsonConverter<EntityRegistry>
    {
        private readonly Dictionary<string, Type> _classLookup;
                
        private class ForwardReference
        {
            public int EntityId { get; set; }

            public object Key { get; set; }

            public ForwardReference()
            {
            }

            public ForwardReference(int entityKey)
            {
                this.EntityId  = entityKey;
            }

            public void ResolveEntity(EntityRegistry registry, object target)
            {
                if (target is IList list)
                {
                    list[(int)Key] = registry[EntityId];
                }
                else if (target is IDictionary dictionary)
                {
                    dictionary[Key] = registry[EntityId];
                }
                else if (Key is PropertyInfo propertyInfo)
                {
                    propertyInfo.SetValue(target, registry[EntityId]);
                }
            }
        }

        public EntityRegistryConverter()
        {
        }

        public EntityRegistryConverter(Dictionary<string, Type> classLookup)
        {
            _classLookup = classLookup;
        }
                
        public override EntityRegistry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var registry = new EntityRegistry
            {
                Id = Utf8JsonReaderExtensions.ReadProperty<int>(ref reader, nameof(EntityRegistry.Id)),
                NextEntityId = Utf8JsonReaderExtensions.ReadProperty<int>(ref reader, nameof(EntityRegistry.NextEntityId)),
                MaxEntitySlots = Utf8JsonReaderExtensions.ReadProperty<int>(ref reader, nameof(EntityRegistry.MaxEntitySlots))
            };

            ReadEntityInstances(ref reader, registry);
            return registry;
       }
    
        public override void Write(Utf8JsonWriter writer, EntityRegistry registry, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteNumber(nameof(EntityRegistry.Id), registry.Id);
            writer.WriteNumber(nameof(EntityRegistry.NextEntityId), registry.NextEntityId);
            writer.WriteNumber(nameof(EntityRegistry.MaxEntitySlots), registry.MaxEntitySlots);

            writer.WritePropertyName(SharedJsonTags.EntityInstancesTag);
            {
                writer.WriteStartObject();
                {
                    foreach (KeyValuePair<int, IEntity> kvp in registry)
                    {
                        writer.WritePropertyName(kvp.Key.ToString());
                        writer.WriteVentObject(kvp.Value);
                    }
                }
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        }
                
        private static void ResolveForwardReferences(EntityRegistry registry, 
            Dictionary<object, List<ForwardReference>> forwardReferences)
        {
            foreach (var forwardReferenceList in forwardReferences)
            {
                foreach (var forwardReference in forwardReferenceList.Value)
                {
                    forwardReference.ResolveEntity(registry, forwardReferenceList.Key);
                }
            }
        }

        private void ReadEntityInstances(ref Utf8JsonReader reader, EntityRegistry registry)
        {
            reader.ReadPropertyName(SharedJsonTags.EntityInstancesTag);
            {
                reader.ReadToken(JsonTokenType.StartObject);
                {
                    var forwardReferences = new Dictionary<object, List<ForwardReference>>();

                    while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var key = int.Parse(reader.GetString());
                        var entity = ParseNullOrVentObject(ref reader, registry, forwardReferences) as IEntity;

                        registry.SetSlot(key, entity);                      
                    }

                    ResolveForwardReferences(registry, forwardReferences);
                }
                reader.ReadToken(JsonTokenType.EndObject);
            }
        }

        private object CreateInstanceFromTypeName(ref Utf8JsonReader reader)
        {
            var className = reader.ReadString();
            
            if (className != null)
            {
                return className.CreateInstance(_classLookup);
            }
            else
            {
                throw new JsonException($"Cannot instantiate entity with a null {className}");
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
        private object ParseNullOrVentObject(
            ref Utf8JsonReader reader, 
            EntityRegistry registry, 
            Dictionary<object, List<ForwardReference>> forwardReferences)
        {
            reader.ReadAnyToken();

            return reader.TokenType switch
            {
                JsonTokenType.Null => null,
                JsonTokenType.StartObject => ParseVentObject(ref reader, registry, forwardReferences),
                _ => throw new NotImplementedException($"Unexpected token {reader.TokenType} encountered"),
            };
        }

        private object ParseVentObject(ref Utf8JsonReader reader,
            EntityRegistry registry,
            Dictionary<object, List<ForwardReference>> forwardReferences)
        {
            reader.ReadPropertyName(SharedJsonTags.EntityTypeTag);
            {
                var obj = CreateInstanceFromTypeName(ref reader);
                ParseObjectProperties(ref reader, registry, forwardReferences, obj);
                return obj;
            }
        }

        private void ParseObjectProperties(
                ref Utf8JsonReader reader,
                EntityRegistry registry,
                Dictionary<object, List<ForwardReference>> references,
                object obj)
        {
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
                        var value = ParseValue(ref reader, registry, references, info.PropertyType, info.GetEntitySerialization());

                        if (value is ForwardReference reference)
                        {
                            reference.Key = info;

                            if (objectReferences == null)
                            {
                                objectReferences = new List<ForwardReference>();
                                references[obj] = objectReferences;
                            }

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

        private object ParseValue(
            ref Utf8JsonReader reader, 
            EntityRegistry registry,
            Dictionary<object, List<ForwardReference>> references,
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

                return registry.ContainsKey(key)
                    ? registry[key]
                    : new ForwardReference(key);
            }
            else if (EntityReflection.IsPrimitiveOrString(valueType))
            {
                return Utf8JsonReaderExtensions.ReadPrimitive(reader, valueType);
            }
            else if (typeof(IEnumerable).IsAssignableFrom(valueType) && valueType.IsGenericType)
            {
                if (valueType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    return ReadList(ref reader, registry, references, valueType, entitySerialization);
                }
                else if (valueType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    return ReadDictionary(ref reader, registry, references, valueType, entitySerialization);
                }
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                return ParseVentObject(ref reader, registry, references);
            }

            throw new NotImplementedException($"Cannot parse {valueType} to a value");
        }
        
        private IList ReadList(ref Utf8JsonReader reader,
            EntityRegistry registry,
            Dictionary<object, List<ForwardReference>> references,
            Type type,
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            var elementType = type.GetGenericArguments()[0];
            var listType = typeof(List<>).MakeGenericType(elementType);
            
            if (typeof(IEntity).IsAssignableFrom(elementType) && entitySerialization == EntitySerialization.AsReference)
            {
                return ReadEntityList(ref reader, registry, references, listType);
            }
            else
            {
                return ReadValueList(ref reader, registry, references, listType, elementType, entitySerialization);
            }            
        }

        private IList ReadEntityList(ref Utf8JsonReader reader,
            EntityRegistry registry,
            Dictionary<object, List<ForwardReference>> references,
            Type listType)
        {
            List<ForwardReference> listEntityReferences = null;
            var listValue = (IList)Activator.CreateInstance(listType);

            void ParseEntityListElement(ref Utf8JsonReader reader)
            {
                reader.ReadAnyToken();

                if (reader.TokenType != JsonTokenType.EndArray)
                {
                    var key = reader.GetInt32();
                    if (registry.ContainsKey(key))
                    {
                        listValue.Add(registry[key]);
                    }
                    else
                    {
                        if (listEntityReferences == null)
                        {
                            listEntityReferences = new List<ForwardReference>();
                            references[listValue] = listEntityReferences;
                        }

                        listEntityReferences.Add(new ForwardReference()
                        {
                            EntityId = key,
                            Key = listValue.Count
                        });

                        listValue.Add(null);
                    }
                }
            }

            Utf8JsonReaderExtensions.ParseJsonArray(ref reader, ParseEntityListElement);
            return listValue;
        }

        private IList ReadValueList(ref Utf8JsonReader reader,
         EntityRegistry registry,
         Dictionary<object, List<ForwardReference>> references,
         Type listType, Type listElementType,
         EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            var listValue = (IList)Activator.CreateInstance(listType);

            void ParseListElement(ref Utf8JsonReader reader)
            {
                reader.ReadAnyToken();

                if (reader.TokenType != JsonTokenType.EndArray)
                {
                    listValue.Add(ParseValue(ref reader, registry, references, listElementType, entitySerialization));
                }
            }

            Utf8JsonReaderExtensions.ParseJsonArray(ref reader, ParseListElement);

            return listValue;
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

        private IDictionary ReadDictionary(ref Utf8JsonReader reader,
            EntityRegistry registry,
            Dictionary<object, List<ForwardReference>> references,
            Type type,
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            var keyConverter = GetKeyConverter(type.GetGenericArguments()[0]);
            var valueType = type.GetGenericArguments()[1];
            var dictionary = (IDictionary) Activator.CreateInstance(type);
            List<ForwardReference> objectReferences = null;

            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                var key = keyConverter(reader.GetString());

                reader.ReadAnyToken();
                {
                    var value = ParseValue(ref reader, registry, references, valueType, entitySerialization);

                    if (value is ForwardReference reference)
                    {
                        reference.Key = key;

                        if (objectReferences == null)
                        {
                            objectReferences = new List<ForwardReference>();
                            references[dictionary] = objectReferences;
                        }

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
    }
}

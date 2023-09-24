using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vent.ToJson
{
    public class EntityRegistryConverter : JsonConverter<EntityRegistry>
    {
        private readonly Dictionary<string, Type> _classLookup = new();
                
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
                else if (Key is PropertyInfo propertyInfo)
                {
                    propertyInfo.SetValue(target, registry[EntityId]);
                }
            }
        }

        public EntityRegistryConverter()
        {
        }

        public EntityRegistryConverter(params Assembly[] assemblies)
        {
            RegisterEntityClasses(assemblies);
        }


        public EntityRegistryConverter RegisterEntityClasses() =>
            RegisterEntityClasses(AppDomain.CurrentDomain.GetAssemblies());

        public EntityRegistryConverter RegisterEntityClasses(params Assembly[] assemblies)
        {
            var entityType = typeof(IEntity);

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes().Where(p =>
                    p.IsClass && !p.IsAbstract
                    && entityType.IsAssignableFrom(p)))
                {
                    _classLookup[type.FullName] = type;
                }
            }

            return this;
        }

        public override EntityRegistry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var registry = new EntityRegistry
            {
                Id = Utf8JsonUtil.ReadProperty<int>(ref reader, nameof(EntityRegistry.Id)),
                NextEntityId = Utf8JsonUtil.ReadProperty<int>(ref reader, nameof(EntityRegistry.NextEntityId)),
                MaxEntitySlots = Utf8JsonUtil.ReadProperty<int>(ref reader, nameof(EntityRegistry.MaxEntitySlots))
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
                writer.WriteStartObject();

                foreach (KeyValuePair<int, IEntity> kvp in registry)
                {
                    writer.WritePropertyName(kvp.Key.ToString());

                    Utf8JsonUtil.WriteVentObject(writer, kvp.Value);
                }

                writer.WriteEndObject();
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
                        var entity = CreateObject(ref reader, registry, forwardReferences) as IEntity;

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
            
            if (className != null && _classLookup.TryGetValue(className, out var type))
            {
                var genericArgsIndex = className.IndexOf("<");

                if (genericArgsIndex >= 0)
                {
                    var genericArgs = ParseUtil.ParseGenericArgs(className.Substring(genericArgsIndex));
                    //var genericType = type.MakeGenericType(genericArgs);
                    return null; // (IEntity)Activator.CreateInstance(genericType);
                }
                else
                {
                    return (IEntity)Activator.CreateInstance(type);
                }
            }
            else
            {
                throw new JsonException($"Cannot instantiate entity of class {className}, it was not found in the classRegistry");
            }
        }

        

        private object CreateObject(
            ref Utf8JsonReader reader, 
            EntityRegistry registry, 
            Dictionary<object, List<ForwardReference>> forwardReferences)
        {
            reader.ReadAnyToken();

            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return null;
                case JsonTokenType.StartObject:
                    reader.ReadPropertyName(SharedJsonTags.EntityTypeTag);
                    {
                        var obj = CreateInstanceFromTypeName(ref reader);
                       
                        ParseObjectProperties(ref reader, registry, forwardReferences, obj);

                        return obj;
                    }
                   
                default:
                    throw new NotImplementedException($"Unexpected token {reader.TokenType} encountered");
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
            EntitySerialization entitySerialization)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }
            else if (EntityReflection.IsEntity(valueType))
            {
                var key = reader.GetInt32();

                return registry.ContainsKey(key)
                    ? registry[key]
                    : new ForwardReference(key);
            }
            else if (EntityReflection.IsPrimitiveOrString(valueType))
            {
                return Utf8JsonUtil.ReadPrimitive(reader, valueType);
            }
            else if (typeof(IEnumerable).IsAssignableFrom(valueType))
            {
                if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    return ParseList(ref reader, registry, references, valueType, entitySerialization);
                }
            }
            else
            {
                return CreateObject(ref reader, registry, references);
            }

            throw new NotImplementedException($"Cannot parse {valueType} to a value");
        }

        
        private IList ParseList(ref Utf8JsonReader reader,
            EntityRegistry registry,
            Dictionary<object, List<ForwardReference>> references,
            Type type,
            EntitySerialization entitySerialization)
        {
            var elementType = type.GetGenericArguments()[0];
            var listType = typeof(List<>).MakeGenericType(elementType);
            var listValue = (IList)Activator.CreateInstance(listType);
            
            List<ForwardReference> listEntityReferences = null;

            if (typeof(IEntity).IsAssignableFrom(elementType) && entitySerialization == EntitySerialization.AsReference)
            {
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

                Utf8JsonUtil.ParseJsonArray(ref reader, ParseEntityListElement);
            }
            else
            {
                void ParseListElement(ref Utf8JsonReader reader)
                {
                    reader.ReadAnyToken();

                    if (reader.TokenType != JsonTokenType.EndArray)
                    {
                        listValue.Add(ParseValue(ref reader, registry, references, elementType, entitySerialization));
                    }
                }

                Utf8JsonUtil.ParseJsonArray(ref reader, ParseListElement);
            }

            return listValue;
        }
    }
}

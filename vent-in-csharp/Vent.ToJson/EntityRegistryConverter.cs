
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Formats.Asn1.AsnWriter;

namespace Vent.ToJson
{
    public class EntityRegistryConverter : JsonConverter<EntityRegistry>
    {
        private const string EntityInstancesTag = "EntityRegistry";
        private const string EntityTypeTag = "__entityType";

        private readonly Dictionary<string, Type> _classLookup = new();

        private class ForwardReference
        {
            public int entityKey;
            public PropertyInfo propertyInfo;
            public object target;

            public void Apply(EntityRegistry registry)
            {
                propertyInfo.SetValue(target, registry[entityKey]);
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
                Id = JsonUtil.ReadProperty<int>(ref reader, nameof(EntityRegistry.Id)),
                NextEntityId = JsonUtil.ReadProperty<int>(ref reader, nameof(EntityRegistry.NextEntityId)),
                MaxEntitySlots = JsonUtil.ReadProperty<int>(ref reader, nameof(EntityRegistry.MaxEntitySlots))
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

            writer.WritePropertyName(EntityInstancesTag);
                writer.WriteStartObject();

                foreach (KeyValuePair<int, IEntity> kvp in registry)
                {
                    writer.WritePropertyName(kvp.Key.ToString());

                    if (kvp.Value != null)
                    {
                        writer.WriteStartObject();

                        writer.WriteString(EntityTypeTag, kvp.Value.GetType().FullName);

                        WriteObjectProperties(writer, kvp.Value);

                        writer.WriteEndObject();
                    }
                    else
                    {
                        writer.WriteNullValue();
                    }
                }

                writer.WriteEndObject();
            writer.WriteEndObject();
        }

        private static void WriteObjectProperties(Utf8JsonWriter writer, object obj)
        {
            foreach (PropertyInfo property in obj.GetType().GetProperties())
            {
                if (property.CanWrite && property.CanRead)
                {
                    var value = property.GetValue(obj);

                    writer.WritePropertyName(property.Name);
                    writer.WriteVentValue(value);
                }
            }
        }

        private void ReadEntityInstances(ref Utf8JsonReader reader, EntityRegistry registry)
        {
            if (reader.Read() 
                && reader.TokenType == JsonTokenType.PropertyName 
                && reader.ValueTextEquals(EntityInstancesTag))
            {
                if (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
                {
                    var forwardReferences = new List<ForwardReference>();

                    while (ReadInstance(ref reader, registry, forwardReferences)) ;

                    if (!reader.Read())
                    {
                        throw new JsonException($"expected JsonTokenType.EndObject but found no more tokens.");
                    }

                    if (reader.TokenType != JsonTokenType.EndObject)
                    {
                        throw new JsonException($"expected JsonTokenType.EndObject but found {reader.TokenType}.");
                    }

                    foreach (var forwardReference in forwardReferences)
                    {
                        forwardReference.Apply(registry);
                    }
                }
            }
            else
            {
                if (reader.IsFinalBlock)
                {
                    throw new JsonException($"expected Property with name {EntityInstancesTag} but found no more tokens.");
                }
                else
                {
                    throw new JsonException($"expected Property with name {EntityInstancesTag} but found {reader.TokenType}.");
                }
            }
        }

        private bool ReadInstance(ref Utf8JsonReader reader, EntityRegistry registry, List<ForwardReference> references)
        {
            if (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                var key = int.Parse(reader.GetString());

                if (reader.Read())
                {
                    switch (reader.TokenType)
                    {
                        case JsonTokenType.Null:
                            registry.SetSlotToNull(key);
                            break;
                        case JsonTokenType.StartObject:
                            var entity = CreateEntityInstance(ref reader);
                            registry.SetSlot(key, entity);
                            ReadEntityProperties(ref reader, registry, entity, references);
                            break;
                        default:
                            throw new NotImplementedException($"Unexpected token {reader.TokenType} encountered after key {key} while reading an entity instance");

                    }

                   
                    if (reader.TokenType != JsonTokenType.EndObject)
                    {
                        throw new JsonException($"expected JsonTokenType.EndObject but found {reader.TokenType}");
                    }

                    return true;
                }
            }

            return false;
        }

        private IEntity CreateEntityInstance(ref Utf8JsonReader reader)
        {
            // read and create entity type this must be the first property
            if (reader.Read() && reader.TokenType == JsonTokenType.PropertyName
                    && reader.ValueTextEquals(EntityTypeTag))
            {
                if (reader.Read())
                {
                    var className = reader.GetString();

                    if (className != null && _classLookup.TryGetValue(className, out var type))
                    {
                        return (IEntity)Activator.CreateInstance(type);
                    }
                    else
                    {
                        throw new JsonException($"Cannot instantiate entity of class {className}, it was not found in the classRegistry");
                    }
                }
                else
                {
                    throw new JsonException($"expected EntityType but found no more tokens");
                }
            }
            else
            {
                throw new InvalidDataException($"First property of Json should be a valid property named {EntityTypeTag}.");
            }
        }

        private void ReadEntityProperties(
                ref Utf8JsonReader reader, 
                EntityRegistry registry, 
                IEntity entity, 
                List<ForwardReference> references)
        {
            var entityType = entity.GetType();
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName) 
            { 
                var propertyName = reader.GetString();  

                var info = entityType.GetProperty(propertyName);

                if (info != null)
                {
                    var type = info.PropertyType;

                    var value = ParseValue(ref reader, registry, type);

                    if (value is ForwardReference reference)
                    {
                        reference.propertyInfo = info;
                        reference.target = entity;
                        references.Add(reference);
                    }
                    else
                    {
                        info.SetValue(entity, value);
                    }
                }
                else
                {
                    throw new NotImplementedException($"Entity of type {entityType} does not have a property called {propertyName}");
                }

            }
        }


        private object ParseValue(ref Utf8JsonReader reader, EntityRegistry registry, Type expectedType)
        {
            if (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    return null;
                }

                if (EntityReflection.IsPrimitiveOrString(expectedType))
                {
                    return JsonUtil.ReadPrimitive(reader, expectedType);
                }
                else if (EntityReflection.IsEntity(expectedType))
                {
                    var key = reader.GetInt32();
                    if (registry.ContainsKey(key))
                    {
                        return registry[key];
                    }

                    return new ForwardReference()
                    {
                        entityKey = key,
                    };
                    
                }
                else
                {
                    throw new NotImplementedException($"Cannot parse {expectedType} to a value");
                }
            }

            throw new JsonException($"expected Value but found no more tokens");
        }
    }
}

using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vent.ToJson
{
    public class EntityRegistryConverter : JsonConverter<EntityRegistry>
    {
        private readonly Dictionary<string, Type> _classLookup;

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
                

        private void ReadEntityInstances(ref Utf8JsonReader reader, EntityRegistry registry)
        {
            var contextStack = new List<JsonReaderContext>() {
                new JsonReaderContext()
                {
                    ClassLookup = _classLookup,
                    Registry = registry
                }
            };

            reader.ReadPropertyName(SharedJsonTags.EntityInstancesTag);
            {
                reader.ReadToken(JsonTokenType.StartObject);
                {                   
                    while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var key = int.Parse(reader.GetString());
                        reader.ReadAnyToken();
                        var entity = reader.ReadEntity(contextStack, EntitySerialization.AsValue) as IEntity;
                        registry.SetSlot(key, entity);                      
                    }

                    TypeNameNode.ResolveForwardReferences(contextStack[0].ForwardReferenceLookup);
                }
                reader.ReadToken(JsonTokenType.EndObject);
            }
        }       
    }
}

using Microsoft.Win32;
using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vent.ToJson.Readers;

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
            return (EntityRegistry) reader.ReadEntity(new JsonReaderContext()
            {
                ClassLookup = _classLookup
            }, 
            typeof(EntityRegistry),
            EntitySerialization.AsValue);
        }
    
        public override void Write(Utf8JsonWriter writer, EntityRegistry registry, JsonSerializerOptions options)
        {
            writer.WriteVentObject(registry);
        }
    }
}

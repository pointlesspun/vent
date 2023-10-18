﻿/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using System.Text.Json;
using System.Text.Json.Serialization;

using Vent.Registry;
using Vent.ToJson.Writers;

namespace Vent.ToJson.Readers
{
    /// <summary>
    /// Wrapper for the System.Text.Json.JsonConverter for the function reader.ReadEntity
    /// </summary>
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
            return (EntityRegistry)reader.ReadEntity(new JsonReaderContext()
            {
                ClassLookup = _classLookup
            },
            typeof(EntityRegistry),
            EntitySerialization.AsValue);
        }

        public override void Write(Utf8JsonWriter writer, EntityRegistry registry, JsonSerializerOptions options)
        {
            writer.WriteObject(registry);
        }
    }
}
using System.Text.Json;

namespace Vent.ToJson.Readers
{
    public static class Utf8JsonEntityReader
    {
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

                var entity = (IEntity)CreateInstanceFromTypeName(ref reader, context.ClassLookup);

                if (entity is EntityRegistry registry)
                {
                    context.Push(registry);

                    reader.ReadObjectProperties(context, entity);

                    // are there any references to resolve ?
                    if (context.Top.ForwardReferenceLookup != null)
                    {
                        TypeNameNode.ResolveForwardReferences(context.TopLookup);
                    }

                    context.Pop();
                }
                else
                {
                    reader.ReadObjectProperties(context, entity);
                }

                return entity;
            }

            throw new NotImplementedException($"Cannot parse {reader.TokenType} to an serialization of an entity");
        }

        private static object CreateInstanceFromTypeName(ref Utf8JsonReader reader, Dictionary<string, Type> classLookup)
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
    }
}

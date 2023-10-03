using System.Text.Json;

namespace Vent.ToJson.Readers
{
    public class Utf8JsonEntityReader : AbstractUtf8JsonReader<object> 
    {
        public override object ReadValue(ref Utf8JsonReader reader,
            JsonReaderContext context,
            Type type,
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            return Utf8JsonEntityReaderExtensions.ReadEntity(ref reader, context, entitySerialization);
        }
    }

    public static class Utf8JsonEntityReaderExtensions
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

                var entity = (IEntity)reader.ReadString().CreateInstance(context.ClassLookup);

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
    }
}

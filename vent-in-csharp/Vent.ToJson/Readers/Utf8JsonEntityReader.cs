using System.Text;
using System.Text.Json;

namespace Vent.ToJson.Readers
{
    public static class Utf8JsonEntityReader
    {
        public static T ReadEntityFromJson<T>(
            string jsonText,
            JsonReaderContext context = null,
            EntitySerialization entitySerialization = EntitySerialization.AsReference
        )
        {
            var objectReader = new Utf8JsonReader(Encoding.UTF8.GetBytes(jsonText));
            return ReadEntity<T>(ref objectReader, context, entitySerialization);
        }

        public static T ReadEntity<T>(
            this ref Utf8JsonReader reader,
            JsonReaderContext context = null,
            EntitySerialization entitySerialization = EntitySerialization.AsReference
        )
        {
            // create a new context if none was provided
            context ??= new JsonReaderContext(new EntityRegistry(), ClassLookup.CreateDefault());

            if (reader.TokenType == JsonTokenType.None)
            {
                reader.Read();
            }

            return (T)ReadEntity(ref reader, context, entitySerialization);
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

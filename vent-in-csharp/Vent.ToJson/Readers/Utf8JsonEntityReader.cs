using System.Reflection;
using System.Text;
using System.Text.Json;
using Vent.Registry;
using Vent.ToJson.ClassResolver;
using Vent.Util;

namespace Vent.ToJson.Readers
{
    /// <summary>
    /// Read an entity. Note that we
    /// </summary>
    public class Utf8JsonEntityReader : AbstractUtf8JsonReader<IEntity>
    {
        /// <summary>
        /// Convenience method to read a single entity 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonText"></param>
        /// <param name="context"></param>
        /// <param name="entitySerialization"></param>
        /// <returns></returns>
        public static T ReadEntityFromJson<T>(string jsonText,
            JsonReaderContext context = null,
            EntitySerialization entitySerialization = EntitySerialization.AsValue) where T: IEntity
        {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(jsonText));

            // create a new context if none was provided
            context ??= new JsonReaderContext(new EntityRegistry(), ClassLookup.CreateDefault(Assembly.GetCallingAssembly()));

            if (reader.TokenType == JsonTokenType.None)
            {
                reader.Read();
            }

            if (reader.TokenType == JsonTokenType.Null)
            {
                if (typeof(T).IsNullableType())
                {
                    return default;
                }

                throw new JsonException($"Encounted a json null token but the corresponding type {typeof(T)} is not nullable.");
            }

            return (T) Utf8JsonEntityReaderExtensions.ReadEntity(ref reader, context, typeof(T), entitySerialization);
        }

        public override object ReadValue(ref Utf8JsonReader reader,
            JsonReaderContext context,
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            return Utf8JsonEntityReaderExtensions.ReadEntity(ref reader, context, typeof(IEntity), entitySerialization);
        }
    }

    public static class Utf8JsonEntityReaderExtensions
    {
        public static IEntity ReadEntity(this ref Utf8JsonReader reader,
            JsonReaderContext context,
            Type entityType,
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (entitySerialization == EntitySerialization.AsReference)
            {
                if (typeof(EntityRegistry).IsAssignableFrom(entityType))
                {
                    // entities can only refer to their own registry
                    return context.TopRegistry;
                }
                else
                {
                    var key = reader.GetInt32();

                    if (key == -1)
                    {
                        return null;
                    }

                    return context.TopRegistry.ContainsKey(key)
                            ? context.TopRegistry[key]
                            : new ForwardEntityReference(context.TopRegistry, key);
                }
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

using System.Collections;
using System.Text.Json;

namespace Vent.ToJson.Readers
{
    public static class Utf8JsonListReader
    {
        public static List<T> ReadList<T>(
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

            return (List<T>) ReadList(ref reader, context, typeof(T), entitySerialization);
        }

        public static IList ReadList(
            this ref Utf8JsonReader reader,
            JsonReaderContext context,
            Type listElementType,
            EntitySerialization entitySerialization
        )
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }
            
            var listType = typeof(List<>).MakeGenericType(listElementType);

            if (typeof(IEntity).IsAssignableFrom(listElementType) 
                && entitySerialization == EntitySerialization.AsReference)
            {
                return ReadEntityList(ref reader, context, listType);
            }
            else
            {
                return ReadValueList(ref reader, context, listType, listElementType, entitySerialization);
            }
        }

        public static IList ReadEntityList(
            this ref Utf8JsonReader reader,
            JsonReaderContext context,
            Type listType
        )
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                var listValue = (IList)Activator.CreateInstance(listType);

                while (reader.TokenType != JsonTokenType.EndArray)
                {   
                    reader.ReadAnyToken();

                    if (reader.TokenType != JsonTokenType.EndArray)
                    {
                        // xxx test if entity points to -1
                        var entityId = reader.GetInt32();
                        if (context.TopRegistry.ContainsKey(entityId))
                        {
                            listValue.Add(context.TopRegistry[entityId]);
                        }
                        else
                        {
                            context.Top.AddReference(listValue,
                                new ForwardReference(context.TopRegistry, entityId, listValue.Count));
                            // add a null value, this will be replaced when the forward references are
                            // resolved with the actual entity
                            listValue.Add(null);
                        }
                    }
                }
                
                return listValue;
            }
            else
            {
                throw new JsonException($"expected JsonTokenType.StartArray but found {reader.TokenType}.");
            }
        }

        public static IList ReadValueList(
            this ref Utf8JsonReader reader,
            JsonReaderContext context,
            Type listType, 
            Type listElementType,
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                var listValue = (IList)Activator.CreateInstance(listType);
                var skipNextRead = false;

                while (reader.TokenType != JsonTokenType.EndArray)
                {
                    if (skipNextRead)
                    {
                        skipNextRead = false;
                    }
                    else
                    { 
                        reader.ReadAnyToken();
                    }

                    if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        listValue.Add(reader.ReadVentValue(listElementType, context, entitySerialization));
                        skipNextRead = true;
                        reader.ReadAnyToken();
                    }
                    else if (reader.TokenType != JsonTokenType.EndArray)
                    {
                        listValue.Add(reader.ReadVentValue(listElementType, context, entitySerialization));
                    }                    
                }

                return listValue;
            }
            else
            {
                throw new JsonException($"expected JsonTokenType.StartArray but found {reader.TokenType}.");
            }
        }
    }
}

using System.Collections;
using System.Text;
using System.Text.Json;

namespace Vent.ToJson.Readers
{
    public static class Utf8JsonListReader
    {
        public static List<T> ReadListFromJson<T>(
            string jsonText,
            JsonReaderContext context = null,
            EntitySerialization entitySerialization = EntitySerialization.AsReference
        )
        {
            var listReader = new Utf8JsonReader(Encoding.UTF8.GetBytes(jsonText));
            return ReadList<T>(ref listReader, context, entitySerialization);
        }

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
            if (reader.TokenType != JsonTokenType.Null)
            {
                return ReadValueList(ref reader, context,
                    typeof(List<>).MakeGenericType(listElementType), listElementType, entitySerialization);
            }
            
            return null;
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
                        AddValueToList(context, listValue, 
                            reader.ReadVentValue(listElementType, context, entitySerialization));
                        skipNextRead = true;
                        reader.ReadAnyToken();
                    }
                    else if (reader.TokenType != JsonTokenType.EndArray)
                    {
                        AddValueToList(context, listValue, 
                            reader.ReadVentValue(listElementType, context, entitySerialization));
                    }                    
                }

                return listValue;
            }
            else
            {
                throw new JsonException($"expected JsonTokenType.StartArray but found {reader.TokenType}.");
            }
        }

        private static void AddValueToList(JsonReaderContext context, IList list, object value)
        {
            if (value is ForwardReference forwardReference)
            {
                forwardReference.Key = list.Count;
                context.Top.AddReference(list, forwardReference);
                list.Add(null);
            }
            else
            {
                list.Add(value);
            }
        }
    }
}

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

            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
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
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                var listValue = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(listElementType));

                reader.ReadAnyToken();

                while (reader.TokenType != JsonTokenType.EndArray)
                {
                    AddValueToList(context, listValue,
                        reader.ReadVentValue(listElementType, context, entitySerialization));

                    reader.ReadAnyToken();
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

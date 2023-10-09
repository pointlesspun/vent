using System.Collections;
using System.Text.Json;
using Vent.Registry;

namespace Vent.ToJson.Readers
{
    public class Utf8JsonListReader<T> : AbstractUtf8JsonReader<List<T>>
    {
        public override object ReadValue(ref Utf8JsonReader reader,
            JsonReaderContext context,
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            return Utf8JsonListReaderExtensions.ReadList(ref reader, context, typeof(T), entitySerialization);
        }
    }

    public static class Utf8JsonListReaderExtensions
    {
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
            if (value is ForwardEntityReference forwardReference)
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

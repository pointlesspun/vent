using System.Collections;
using System.Text;
using System.Text.Json;

namespace Vent.ToJson.Readers
{
    public static class Utf8JsonDictionaryReader
    {
        public static Dictionary<TKey, TValue> ReadDictionaryFromJson<TKey, TValue>(
            string jsonText,
            JsonReaderContext context = null,
            EntitySerialization entitySerialization = EntitySerialization.AsReference
        )
        {
            var listReader = new Utf8JsonReader(Encoding.UTF8.GetBytes(jsonText));
            return ReadDictionary<TKey, TValue>(ref listReader, context, entitySerialization);
        }

        public static Dictionary<TKey,TValue> ReadDictionary<TKey, TValue>(
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

            return (Dictionary<TKey, TValue>) 
                        ReadDictionary(ref reader, context, typeof(Dictionary<TKey, TValue>), entitySerialization);
        }

        public static IDictionary ReadDictionary(this ref Utf8JsonReader reader,
            JsonReaderContext context,
            Type type,
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            var keyConverter = GetKeyConverter(type.GetGenericArguments()[0]);
            var valueType = type.GetGenericArguments()[1];
            var dictionary = (IDictionary)Activator.CreateInstance(type);

            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                var key = keyConverter(reader.GetString());

                reader.ReadAnyToken();
                {
                    var value = reader.ReadVentValue(valueType, context, entitySerialization);

                    if (value is ForwardReference reference)
                    {
                        reference.Key = key;
                        context.Top.AddReference(dictionary, reference);
                    }
                    else
                    {
                        dictionary[key] = value;
                    }
                }
            }

            return dictionary;
        }

        private static Func<string, object> GetKeyConverter(Type keyType)
        {
            if (keyType == typeof(string))
            {
                return (str) => str;
            }
            else if (keyType.IsPrimitive)
            {
                return str => Convert.ChangeType(str, keyType);
            }
            else if (keyType == typeof(DateTime))
            {
                return (str) =>
                {
                    if (long.TryParse(str, out var ticks))
                    {
                        return new DateTime(ticks);
                    }

                    return DateTime.Parse(str);
                };
            }

            throw new NotImplementedException($"Cannot convert key from {keyType.Name}");
        }
    }
}

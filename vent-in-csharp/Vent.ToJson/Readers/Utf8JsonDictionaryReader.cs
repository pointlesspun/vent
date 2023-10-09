using System.Collections;
using System.Text.Json;
using Vent.Registry;

namespace Vent.ToJson.Readers
{
    public class Utf8JsonDictionaryReader<TKey, TValue> : AbstractUtf8JsonReader<Dictionary<TKey, TValue>>
    {
        public override object ReadValue(ref Utf8JsonReader reader,
            JsonReaderContext context,
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            return Utf8JsonDictionaryReaderExtensions.ReadDictionary(ref reader, context, typeof(Dictionary<TKey, TValue>), entitySerialization);
        }
    }
   
    public static class Utf8JsonDictionaryReaderExtensions
    {
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

                    if (value is ForwardEntityReference reference)
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
           

            throw new NotImplementedException($"Cannot convert dictionary key from {keyType.Name}.");
        }
    }
}

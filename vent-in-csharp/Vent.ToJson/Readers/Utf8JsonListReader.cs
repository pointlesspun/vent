/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using System.Collections;
using System.Text.Json;
using Vent.Registry;

namespace Vent.ToJson.Readers
{
    /// <summary>
    /// Reads a json list with elements of T.  
    /// </summary>
    /// <typeparam name="T"></typeparam>
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
        /// <summary>
        /// Uses the given reader and context to read a list element from the current position
        /// of the json reader. The current json reader position is expected to be an array
        /// which will be converted to a list.
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="context"></param>
        /// <param name="listElementType"></param>
        /// <param name="entitySerialization"></param>
        /// <returns></returns>
        /// <exception cref="JsonException"></exception>
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
                        reader.ReadValue(listElementType, context, entitySerialization));

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

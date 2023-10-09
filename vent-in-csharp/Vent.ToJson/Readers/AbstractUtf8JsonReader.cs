using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Vent.Registry;
using Vent.Util;

namespace Vent.ToJson.Readers
{
    public abstract class AbstractUtf8JsonReader<T>
    {
        public T ReadFromJson(
            string jsonText,
            JsonReaderContext context = null,
            EntitySerialization entitySerialization = EntitySerialization.AsReference
        )
        {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(jsonText));

            // create a new context if none was provided
            context ??= new JsonReaderContext(new EntityRegistry(), ClassLookup.CreateDefault());

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

            return (T) ReadValue(ref reader, context, entitySerialization);
        }

        public abstract object ReadValue(ref Utf8JsonReader reader,
            JsonReaderContext context,
            EntitySerialization entitySerialization = EntitySerialization.AsReference);
    }
}

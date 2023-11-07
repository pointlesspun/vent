/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using System.Reflection;
using System.Text;
using System.Text.Json;
using Vent.Registry;
using Vent.ToJson.ClassResolver;
using Vent.Util;

namespace Vent.ToJson.Readers
{
    /// <summary>
    /// Base class for a reader taking care of some of the boilerplate
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class AbstractUtf8JsonReader<T>
    {
        /// <summary>
        /// Read type T from json text. 
        /// </summary>
        /// <param name="jsonText">A string or null.</param>
        /// <param name="context">Optional parse context, if none is given this will create a default context.</param>
        /// <param name="entitySerialization"></param>
        /// <returns>T or default(T) depending on the data provided</returns>
        /// <exception cref="JsonException"></exception>
        public T ReadFromJson(
            string jsonText,
            JsonReaderContext context = null,
            EntitySerialization entitySerialization = EntitySerialization.AsReference
        )
        {
            if (string.IsNullOrEmpty(jsonText))
            {
                return default;
            }

            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(jsonText));

            // do all the boilerplate stuff

            // create a new context if none was provided ...
            context ??= new JsonReaderContext(new EntityRegistry(), ClassLookup.CreateDefault(Assembly.GetCallingAssembly()));

            // start reading if nothing was read yet
            if (reader.TokenType == JsonTokenType.None)
            {
                reader.Read();
            }

            // this should not happen, but we all know how much value "should" has...
            if (reader.TokenType == JsonTokenType.Null)
            {
                if (typeof(T).IsNullableType())
                {
                    return default;
                }

                throw new JsonException($"Encounted a json null token but the corresponding type {typeof(T)} is not nullable.");
            }

            // ... actual parsing
            return (T) ReadValue(ref reader, context, entitySerialization);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="context"></param>
        /// <param name="entitySerialization"></param>
        /// <returns></returns>
        public abstract object ReadValue(ref Utf8JsonReader reader,
            JsonReaderContext context,
            EntitySerialization entitySerialization = EntitySerialization.AsReference);
    }
}

/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using System.Text.Json;
using Vent.ToJson.Readers;

namespace Vent.ToJson
{
    public interface ICustomJsonSerializable
    {
        void Write(Utf8JsonWriter writer);

        void Read(ref Utf8JsonReader reader, JsonReaderContext context);
    }
}

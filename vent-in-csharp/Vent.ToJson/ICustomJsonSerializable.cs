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

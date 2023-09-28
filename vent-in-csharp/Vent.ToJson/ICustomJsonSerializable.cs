using System.Text.Json;

namespace Vent.ToJson
{
    public interface ICustomJsonSerializable
    {
        void Write(Utf8JsonWriter writer);

        void Read(ref Utf8JsonReader reader, JsonReaderContext context);
    }
}

/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using System.Text.Json;

namespace Vent.ToJson
{
    /// <summary>
    /// Interface for entities which require custom de/serialization. In the write method
    /// the implementation should write all relevant properties (including the Id). In the
    /// read method, the implementation should read the properties in the same order as
    /// the write.
    /// </summary>
    public interface ICustomJsonSerializable
    {
        void Write(Utf8JsonWriter writer);

        void Read(ref Utf8JsonReader reader, JsonReaderContext context);
    }
}

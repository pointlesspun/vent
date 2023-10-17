/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using System.Text.Json;

using Vent.ToJson.Readers;
using Vent.ToJson.Writers;

namespace Vent.ToJson.Test.TestEntities
{
    /// <summary>
    /// Fun test class with a large name. 
    /// </summary>
    public class CustomMultiPropertySerializableTestEntity : MultiPropertyTestEntity, ICustomJsonSerializable
    {
        public void Read(ref Utf8JsonReader reader, JsonReaderContext _)
        {
            Id = reader.ReadPrimitiveProperty<int>(nameof(Id));
            StringValue = reader.ReadPrimitiveProperty<string>(nameof(StringValue));
            IntValue = reader.ReadPrimitiveProperty<int>(nameof(IntValue));
        }

        // we only write some selected properties
        public void Write(Utf8JsonWriter writer)
        {
            writer.WriteProperty(nameof(Id), Id);
            writer.WriteProperty(nameof(StringValue), StringValue);
            writer.WriteProperty(nameof(IntValue), IntValue);
        }
    }
}

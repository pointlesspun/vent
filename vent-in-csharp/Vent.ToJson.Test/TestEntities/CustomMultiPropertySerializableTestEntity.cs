using System.Text.Json;

namespace Vent.ToJson.Test.TestEntities
{
    /// <summary>
    /// Fun test class with a large name. 
    /// </summary>
    public class CustomMultiPropertySerializableTestEntity : MultiPropertyTestEntity, ICustomJsonSerializable
    {
        public void Read(ref Utf8JsonReader reader,
            List<JsonReaderContext> contextStack,
            List<ForwardReference> objectReferences)
        {
            StringValue = reader.ReadPrimitiveProperty<string>(nameof(StringValue));
            IntValue = reader.ReadPrimitiveProperty<int>(nameof(IntValue));
        }

        // we only write some selected properties
        public void Write(Utf8JsonWriter writer)
        {
            writer.WriteProperty(nameof(StringValue), StringValue);
            writer.WriteProperty(nameof(IntValue), IntValue);
        }
    }
}

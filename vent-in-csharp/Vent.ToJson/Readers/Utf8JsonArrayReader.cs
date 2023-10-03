using System.Text.Json;

namespace Vent.ToJson.Readers
{
    public class Utf8JsonArrayReader<T> : AbstractUtf8JsonReader<T[]>
    {
        public override object ReadValue(ref Utf8JsonReader reader,
            JsonReaderContext context,
            Type type,
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            return Utf8JsonArrayReaderExtensions.ReadArray(ref reader, context, type, entitySerialization); 
        }
    }

    public static class Utf8JsonArrayReaderExtensions
    {
        public static Array ReadArray(this ref Utf8JsonReader reader,
            JsonReaderContext context,
            Type valueType,
            EntitySerialization entitySerialization = EntitySerialization.AsReference)
        {
            var elementType = valueType.GetElementType();
            
            // use the Utf8JsonListReader extensions, we don't know the length of the array
            // ahead of reading it so we're using a temp list
            var list = reader.ReadList(context, elementType, entitySerialization);
            var array = Array.CreateInstance(elementType, list.Count);

            list.CopyTo(array, 0);
            return array;
        }
    }
}

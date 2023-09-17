using Newtonsoft.Json;
using System.Collections;
using System.Reflection;

namespace Vent.ToJson
{
    public class StoreConverter : JsonConverter<EntityStore>
    {
        public override EntityStore? ReadJson(JsonReader reader, Type objectType, EntityStore existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, EntityStore store, JsonSerializer serializer)
        {
            WriteStoreConfig(writer, store);
            WriteEntityTypes(writer, store);
            WriteEntityValues(writer, store);
        }

        private void WriteStoreConfig(JsonWriter writer, EntityStore store)
        {
            writer.WritePropertyName("config");
            writer.WriteStartObject();

            writer.WritePropertyName(nameof(store.Version));
            writer.WriteValue(store.Version);

            writer.WritePropertyName(nameof(store.NextEntityId));
            writer.WriteValue(store.NextEntityId);

            writer.WritePropertyName(nameof(store.MaxEntitySlots));
            writer.WriteValue(store.MaxEntitySlots);

            writer.WritePropertyName(nameof(store.MaxMutations));
            writer.WriteValue(store.MaxMutations);

            writer.WritePropertyName(nameof(store.DeleteOutOfScopeVersions));
            writer.WriteValue(store.DeleteOutOfScopeVersions);


            writer.WriteEndObject();
        }

        private void WriteEntityTypes(JsonWriter writer, EntityStore store)
        {
            writer.WritePropertyName("entityTypes");
            writer.WriteStartObject();

            foreach (KeyValuePair<int, IEntity> kvp in store)
            {
                writer.WritePropertyName(kvp.Key.ToString());
                writer.WriteValue(kvp.Value?.GetType().FullName);
            }

            writer.WriteEndObject();
        }

        private void WriteEntityValues(JsonWriter writer, EntityStore store) 
        {
            writer.WritePropertyName("entityValues");
            writer.WriteStartObject();

            foreach (KeyValuePair<int, IEntity> kvp in store)
            {
                if (kvp.Value != null)
                {
                    writer.WritePropertyName(kvp.Key.ToString());
                    WriteObject(writer, kvp.Value);
                }
            }

            writer.WriteEndObject();
        }

        private void WriteObject(JsonWriter writer, object obj) 
        {
            writer.WriteStartObject();

            foreach (PropertyInfo property in obj.GetType().GetProperties())
            {
                if (property.CanWrite && property.CanRead)
                {
                    var value = property.GetValue(obj);

                    writer.WritePropertyName(property.Name);
                    WriteValue(writer, property.PropertyType, value);
                }
            }

            writer.WriteEndObject();
        }

        private void WriteValue(JsonWriter writer, Type propertyType, object value) 
        {
            if (value == null)
            {
                writer.WriteNull();
            }
            else if (EntityReflection.IsPrimitiveOrString(propertyType))
            {
                writer.WriteValue(value);
            }
            else if (EntityReflection.IsEntity(propertyType))
            {
                writer.WriteValue(((IEntity)value).Id);
            }
            else if (propertyType.IsArray)
            {
                WriteArray(writer, (Array)value);
            }
            else if (typeof(IEnumerable).IsAssignableFrom(propertyType))
            {
                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    WriteList(writer, (IList) value);
                }
                else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    WriteDictionary(writer, (IDictionary) value);
                }
            }
            else
            {
                WriteObject(writer, value);
            }
        }

        private void WriteArray(JsonWriter writer, Array array)
        {
            var type = array.GetType();
            var elementType = type.GetElementType();
            
            writer.WriteStartArray();

            for (int i = 0; i < array.Length; i++)
            {
                WriteValue(writer, elementType, array.GetValue(i));
            }

            writer.WriteEndArray();
        }

        private void WriteList(JsonWriter writer, IList list)
        {
            var type = list.GetType();
            var elementType = type.GetGenericArguments()[0];

            writer.WriteStartArray();

            foreach (object element in list)
            {
                WriteValue(writer, elementType, element);
            }

            writer.WriteEndArray();
        }

        private void WriteDictionary(JsonWriter writer, IDictionary dictionary)
        {
            var type = dictionary.GetType();
            var keyType = type.GetGenericArguments()[0];
            var valueType = type.GetGenericArguments()[1];

            writer.WriteStartObject();

            foreach (DictionaryEntry entry in dictionary)
            {
                writer.WritePropertyName(entry.Key.ToString());
                WriteValue(writer, valueType, entry.Value);
            }

            writer.WriteEndObject();
        }
    }
}


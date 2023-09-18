using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Reflection;

namespace Vent.ToJson
{
    public class StoreConverter : JsonConverter<EntityStore>
    {
        private const string EntityInstancesTag = "entityInstances";
        private const string StoreConfigTag = "config";
        private const string EntityPropertiesTag = "entityValues";
        private Dictionary<string, Type> _classLookup = new Dictionary<string, Type>();

        public StoreConverter RegisterEntityClasses(params Assembly[] assemblies)
        {
            var entityType = typeof(IEntity);

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes().Where(p => 
                    p.IsClass && !p.IsAbstract
                    && entityType.IsAssignableFrom(p)))
                {
                    _classLookup[type.FullName] = type;
                }
            }

            return this;
        }

        public override EntityStore ReadJson(
            JsonReader reader,
            Type objectType,
            EntityStore existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            return ReadJson(reader, hasExistingValue && existingValue != null ? existingValue : new EntityStore(), serializer);
        }

        public override void WriteJson(JsonWriter writer, EntityStore store, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            WriteStoreConfig(writer, store);
            WriteEntityTypes(writer, store);
            WriteEntityProperties(writer, store);

            writer.WriteEndObject();
        }

        private EntityStore ReadJson(JsonReader reader, EntityStore store, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.None && reader.TokenType !=  JsonToken.Null)
            { 
                var storeJson = JObject.Load(reader);
                
                ReadConfig(storeJson[StoreConfigTag], store, serializer);
                ReadEntityInstances(storeJson[EntityInstancesTag], store, serializer);
                ReadEntityProperties(storeJson[EntityPropertiesTag], store, serializer);

                // todo restore transient properties
                store.RestoreTransientProperties();


                return store;
            }
    
            throw new JsonException($"Trying to read EntityStore config but the current token is null.");
        }

        private static void ReadConfig(JToken config, EntityStore store, JsonSerializer serializer)
        {
            var version = (string) config[nameof(store.Version)];

            if (version == null || store.Version != version)
            {
                throw new JsonException($"Version of the json data ({version}) doesn't match the given store {store.Version}.");
            }

            store.RestoreNextEntityId((int) config[nameof(store.NextEntityId)]);

            store.MaxEntitySlots = (int)config[nameof(store.MaxEntitySlots)];
            store.MaxMutations = (int)config[nameof(store.MaxMutations)];
            store.DeleteOutOfScopeVersions = (bool)config[nameof(store.DeleteOutOfScopeVersions)];
        }

        private void ReadEntityInstances(JToken instances, EntityStore store, JsonSerializer serializer)
        {
            foreach ( var kvp in ((JObject)instances).Properties() ) 
            {
                var key = int.Parse(kvp.Name);

                if (kvp.Value != null && kvp.Value.Type != JTokenType.None && kvp.Value.Type != JTokenType.Null)
                {
                    var className = (string)kvp.Value;
                    
                    if (className != null  && _classLookup.TryGetValue(className, out var type))
                    {
                        store.RestoreEntity((IEntity) Activator.CreateInstance(type), key);
                    }
                    else 
                    {
                        throw new JsonException($"Cannot instantiate entity of class {className}, it was not found in the classRegistry");
                    }
                }
                else
                {
                    store.RestoreEntity(null, key);
                }
            }
        }

        private void ReadEntityProperties(JToken properties, EntityStore store, JsonSerializer serializer)
        {
            foreach (var kvp in ((JObject)properties).Properties())
            {
                var key = int.Parse(kvp.Name);
                var entity = store[key];

                if (kvp.Value != null && kvp.Value.Type != JTokenType.None && kvp.Value.Type != JTokenType.Null)
                {
                    foreach (var entityPropertyValue in ((JObject)kvp.Value))
                    {
                        ReadProperty((KeyValuePair<string, JToken>) entityPropertyValue, store, entity);
                    }
                }
            }
        }

        private void ReadProperty(KeyValuePair<string, JToken> property, EntityStore store, IEntity entity)
        {
            var propertyInfo = entity.GetType().GetProperty(property.Key);

            ReadValue(property.Value, store, entity, propertyInfo);
    
        }

        private void ReadValue(JToken value, EntityStore store, IEntity entity, PropertyInfo info)
        {
            var type = info.PropertyType;

            if (EntityReflection.IsPrimitiveOrString(type))
            {
                var result = value.ToObject(type);
                info.SetValue(entity, result);
            }
            else if (EntityReflection.IsEntity(type))
            {
                var id = int.Parse(value.ToString());
                info.SetValue(entity, store[id]);
            }
            else if (type.IsArray)
            {
                var arrayValue = value.ToObject(type);
                info.SetValue(entity, arrayValue);
            }
            else if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var elementType = type.GetGenericArguments()[0];
                    if (typeof(IEntity).IsAssignableFrom(elementType))
                    {
                        var listType = typeof(List<>).MakeGenericType(elementType);
                        var listValue = (IList)Activator.CreateInstance(listType);
                        var array = value as JArray;

                        foreach (var arrayValue in array)
                        {
                            var key = arrayValue.ToObject<int>();
                            listValue.Add(store[key]);
                        }

                        info.SetValue(entity, listValue);
                    }
                    else
                    {
                        var listValue = value.ToObject(type);
                        info.SetValue(entity, listValue);
                    }
                }
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    var arrayValue = value.ToObject(type);
                    info.SetValue(entity, arrayValue);
                }
            }
        }

        private void WriteStoreConfig(JsonWriter writer, EntityStore store)
        {
            writer.WritePropertyName(StoreConfigTag);
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
            writer.WritePropertyName(EntityInstancesTag);
            writer.WriteStartObject();

            foreach (KeyValuePair<int, IEntity> kvp in store)
            {
                writer.WritePropertyName(kvp.Key.ToString());
                writer.WriteValue(kvp.Value?.GetType().FullName);
            }

            writer.WriteEndObject();
        }

        private void WriteEntityProperties(JsonWriter writer, EntityStore store) 
        {
            writer.WritePropertyName(EntityPropertiesTag);
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


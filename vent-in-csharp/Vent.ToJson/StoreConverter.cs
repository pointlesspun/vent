
using System.Collections;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Vent.ToJson
{
    public class StoreConverter : JsonConverter<EntityHistory>
    {
        private const string EntityInstancesTag = "entities";
        private const string StoreConfigTag = "storeProperties";
        private const string EntityTypeTag = "__entityType";

        private readonly Dictionary<string, Type> _classLookup = new();

        public StoreConverter()
        {
        }

        public StoreConverter(params Assembly[] assemblies)
        {
            RegisterEntityClasses(assemblies);
        }

        

        public StoreConverter RegisterEntityClasses() => 
            RegisterEntityClasses(AppDomain.CurrentDomain.GetAssemblies());

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

        public override EntityHistory ReadJson(
            JsonReader reader,
            Type objectType,
            EntityHistory existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            return ReadJson(reader, hasExistingValue && existingValue != null ? existingValue : new EntityHistory(new EntityRegistry()), serializer);
        }

        public override void WriteJson(JsonWriter writer, EntityHistory store, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            WriteStoreProperties(writer, store);
            WriteEntityInstances(writer, store);          

            writer.WriteEndObject();
        }

        private EntityHistory ReadJson(JsonReader reader, EntityHistory store, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.None && reader.TokenType !=  JsonToken.Null)
            { 
                var storeJson = JObject.Load(reader);
                
                ReadConfig(storeJson[StoreConfigTag], store);
                ReadEntityInstances(storeJson[EntityInstancesTag], store);
                ReadEntityProperties(storeJson[EntityInstancesTag], store);

                store.RestoreTransientProperties();

                return store;
            }
    
            throw new JsonException($"Trying to read EntityStore config but the current token is null.");
        }

        private static void ReadConfig(JToken config, EntityHistory store)
        {
            var version = (string) config[nameof(store.Version)];

            if (version == null || store.Version != version)
            {
                throw new JsonException($"Version of the json data ({version}) doesn't match the given store {store.Version}.");
            }

            store.RestoreSettings((int) config[nameof(store.NextEntityId)], 
                                    (int)config[nameof(store.CurrentMutation)],
                                    (int)config[nameof(store.OpenGroupCount)]);

            // restore configuration settings
            store.MaxEntitySlots = (int)config[nameof(store.MaxEntitySlots)];
            store.MaxMutations = (int)config[nameof(store.MaxMutations)];
            store.DeleteOutOfScopeVersions = (bool)config[nameof(store.DeleteOutOfScopeVersions)];
        }

        private void ReadEntityInstances(JToken instances, EntityHistory store)
        {
            foreach ( var kvp in ((JObject)instances).Properties() ) 
            {
                var key = int.Parse(kvp.Name);

                if (kvp.Value != null && kvp.Value.Type != JTokenType.None && kvp.Value.Type != JTokenType.Null)
                {
                    var className = (string)kvp.Value[EntityTypeTag];
                    
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

        private void ReadEntityProperties(JToken properties, EntityHistory store)
        {
            foreach (var kvp in ((JObject)properties).Properties())
            {
                var key = int.Parse(kvp.Name);
                var entity = store[key];

                if (kvp.Value != null && kvp.Value.Type != JTokenType.None && kvp.Value.Type != JTokenType.Null)
                {
                    foreach (var entityPropertyValue in ((JObject)kvp.Value))
                    {
                        if (entityPropertyValue.Key != EntityTypeTag)
                        {
                            ReadProperty((KeyValuePair<string, JToken>)entityPropertyValue, store, entity);
                        }
                    }
                }
            }
        }

        private void ReadProperty(KeyValuePair<string, JToken> property, EntityHistory store, IEntity entity)
        {
            var propertyInfo = entity.GetType().GetProperty(property.Key);

            ReadValue(property.Value, store, entity, propertyInfo);
        }

        private void ReadValue(JToken value, EntityHistory store, IEntity entity, PropertyInfo info)
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

        private static void WriteStoreProperties(JsonWriter writer, EntityHistory store)
        {
            writer.WriteObjectValues(StoreConfigTag, store,
                    nameof(store.Version),
                    nameof(store.NextEntityId),
                    nameof(store.MaxEntitySlots),
                    nameof(store.MaxMutations),
                    nameof(store.DeleteOutOfScopeVersions),
                    nameof(store.CurrentMutation),
                    nameof(store.OpenGroupCount));
        }

        private static void WriteEntityInstances(JsonWriter writer, EntityHistory store)
        {
            writer.WritePropertyName(EntityInstancesTag);
            writer.WriteStartObject();

            foreach (KeyValuePair<int, IEntity> kvp in store)
            {
                writer.WritePropertyName(kvp.Key.ToString());

                if (kvp.Value != null)
                {
                    writer.WriteStartObject();

                    writer.WritePropertyName(EntityTypeTag);
                    writer.WriteValue(kvp.Value.GetType().FullName);

                    WriteObjectProperties(writer, kvp.Value);

                    writer.WriteEndObject();
                }
                else
                {
                    writer.WriteNull();
                }
            }

            writer.WriteEndObject();
        }

        private static void WriteObjectProperties(JsonWriter writer, object obj)
        {
            foreach (PropertyInfo property in obj.GetType().GetProperties())
            {
                if (property.CanWrite && property.CanRead)
                {
                    var value = property.GetValue(obj);

                    writer.WritePropertyName(property.Name);
                    writer.WriteVentValue(value);
                }
            }
        }

        
    }
}


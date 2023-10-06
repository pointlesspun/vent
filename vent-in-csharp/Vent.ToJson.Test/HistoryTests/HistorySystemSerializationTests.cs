using Vent.ToJson.Readers;

using static Vent.ToJson.Utf8JsonWriterExtensions;

namespace Vent.ToJson.Test.HistoryTests
{
    /// <summary>
    /// Mimics the HistorySystem test but then with added serialization
    /// </summary>
    [TestClass]
    public class HistorySystemSerializationTests
    {
        private static Utf8JsonEntityReader _reader = new Utf8JsonEntityReader();

        // mirrors HistorySystemTest.RegisterDegisterPropertyEntityTest
        [TestMethod]
        public void RegisterDegisterPropertyEntityTest()
        {
            var registry = new EntityRegistry();
            var history = new HistorySystem(registry);

            var ent = registry.Add(new PropertyEntity<string>("foo"));

            var registryClone0 = CloneViaJson(registry);

            // count = ent + history data
            Assert.IsTrue(registryClone0.EntitiesInScope == 2);
            Assert.IsTrue(registryClone0[ent.Id].Equals(ent));
            
            var history0 = CreateHistorySystemFromRegistry(registryClone0);
            var entId = ent.Id;

            history.DeregisterById(entId);
            history0.DeregisterById(entId);

            // count = history data
            Assert.IsTrue(registry.EntitiesInScope == 1);
            Assert.IsTrue(CloneViaJson(registry).EntitiesInScope == 1);
            Assert.IsTrue(CloneViaJson(registryClone0).EntitiesInScope == 1);
        }

        private static T CloneViaJson<T>(T entity) where T : IEntity
        {
            var entityString = WriteObjectToJsonString(entity, EntitySerialization.AsValue);
            return (T)_reader.ReadFromJson(entityString, entitySerialization: EntitySerialization.AsValue);
        }

        private static HistorySystem CreateHistorySystemFromRegistry(EntityRegistry registry)
        {
            return new HistorySystem(registry, (EntityHistory)registry.First(e => e.Value is EntityHistory).Value);
        }
    }
}
